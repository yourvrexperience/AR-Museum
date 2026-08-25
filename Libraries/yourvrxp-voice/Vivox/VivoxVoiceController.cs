using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using yourvrexperience.Utils;
using UnityEngine;
#if ENABLE_VIVOX
using VivoxUnity;
#endif

namespace yourvrexperience.Voice
{
	public class VivoxVoiceController : MonoBehaviour
	{
		public const string EventVivoxVoiceControllerStarted = "EventVivoxVoiceControllerStarted";
		public const string EventVivoxVoiceControllerInitialized = "EventVivoxVoiceControllerInitialized";
		public const string EventVivoxVoiceControllerLoggedIn = "EventVivoxVoiceControllerLoggedIn";
		public const string EventVivoxVoiceControllerLoggedOut = "EventVivoxVoiceControllerLoggedOut";
		public const string EventVivoxVoiceControllerNewParticipant = "EventVivoxVoiceControllerNewParticipant";

		[SerializeField]
		private string _server = "https://GETFROMPORTAL.www.vivox.com/api2";
		[SerializeField]
		private string _domain = "GET VALUE FROM VIVOX DEVELOPER PORTAL";
		[SerializeField]
		private string _tokenIssuer = "GET VALUE FROM VIVOX DEVELOPER PORTAL";
		[SerializeField]
		private string _tokenKey = "GET VALUE FROM VIVOX DEVELOPER PORTAL";

#if ENABLE_VIVOX
		public enum ChangedProperty
		{
			None,
			Speaking,
			Typing,
			Muted
		}

		public enum ChatCapability
		{
			TextOnly,
			AudioOnly,
			TextAndAudio
		};

		public delegate void ParticipantValueChangedHandler(string username, ChannelId channel, bool value);
		public event ParticipantValueChangedHandler OnSpeechDetectedEvent;
		public delegate void ParticipantValueUpdatedHandler(string username, ChannelId channel, double value);
		public event ParticipantValueUpdatedHandler OnAudioEnergyChangedEvent;


		public delegate void ParticipantStatusChangedHandler(string username, ChannelId channel, IParticipant participant);
		public event ParticipantStatusChangedHandler OnParticipantAddedEvent;
		public event ParticipantStatusChangedHandler OnParticipantRemovedEvent;

		public delegate void ChannelTextMessageChangedHandler(string sender, IChannelTextMessage channelTextMessage);
		public event ChannelTextMessageChangedHandler OnTextMessageLogReceivedEvent;

		public delegate void LoginStatusChangedHandler();
		public event LoginStatusChangedHandler OnUserLoggedInEvent;
		public event LoginStatusChangedHandler OnUserLoggedOutEvent;

		private Uri _serverUri
		{
			get => new Uri(_server);

			set
			{
				_server = value.ToString();
			}
		}
		private TimeSpan _tokenExpiration = TimeSpan.FromSeconds(90);

		private Client _client = new Client();
		private AccountId _accountId;

		private static object _lock = new object();
		private static VivoxVoiceController _instance;

		public static VivoxVoiceController Instance
		{
			get
			{
				lock (_lock)
				{
					if (_instance == null)
					{
						_instance = (VivoxVoiceController)FindObjectOfType(typeof(VivoxVoiceController));
					}
					return _instance;
				}
			}
		}


		public LoginState LoginState { get; private set; }
		public ILoginSession LoginSession;
		public VivoxUnity.IReadOnlyDictionary<ChannelId, IChannelSession> ActiveChannels => LoginSession?.ChannelSessions;
		public IAudioDevices AudioInputDevices => _client.AudioInputDevices;
		public IAudioDevices AudioOutputDevices => _client.AudioOutputDevices;

		public IChannelSession TransmittingSession
		{
			get
			{
				if (_client == null)
					throw new NullReferenceException("client");
				return _client.GetLoginSession(_accountId).ChannelSessions.FirstOrDefault(x => x.IsTransmitting);
			}
			set
			{
				if (value != null)
				{
					_client.GetLoginSession(_accountId).SetTransmissionMode(TransmissionMode.Single, value.Channel);
				}
			}
		}

		void Start()
        {
			SystemEventController.Instance.DispatchSystemEvent(EventVivoxVoiceControllerStarted);
        }

		public void Initialize()
		{
			_client.Uninitialize();
			_client.Initialize();

			SystemEventController.Instance.DelaySystemEvent(EventVivoxVoiceControllerInitialized, 0.1F);
		}

		public void Destroy()
        {
			if (_instance != null)
            {
				GameObject.Destroy(_instance);
				_instance = null;

				OnApplicationQuit();
			}
        }

		private void OnApplicationQuit()
		{
			Client.Cleanup();
			if (_client != null)
			{
				VivoxLog("Uninitializing client.");
				_client.Uninitialize();
				_client = null;
			}
		}

		public void Login(string displayName = null)
		{
			string uniqueId = Guid.NewGuid().ToString();

			_accountId = new AccountId(_tokenIssuer, uniqueId, _domain, displayName);
			LoginSession = _client.GetLoginSession(_accountId);
			LoginSession.PropertyChanged += OnLoginSessionPropertyChanged;
			LoginSession.BeginLogin(_serverUri, LoginSession.GetLoginToken(_tokenKey, _tokenExpiration), SubscriptionMode.Accept, null, null, null, ar =>
			{
				try
				{
					LoginSession.EndLogin(ar);
				}
				catch (Exception e)
				{
					VivoxLogError(nameof(e));
					LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
					return;
				}
			});
		}

		public void Logout()
		{
			if (LoginSession != null && LoginState != LoginState.LoggedOut && LoginState != LoginState.LoggingOut)
			{
				OnUserLoggedOutEvent?.Invoke();
				LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
				LoginSession.Logout();
			}
		}

		public void JoinChannel(string channelName, ChannelType channelType, ChatCapability chatCapability,
			bool switchTransmission = true, Channel3DProperties properties = null)
		{
			if (LoginState == LoginState.LoggedIn)
			{

				ChannelId channelId = new ChannelId(_tokenIssuer, channelName, _domain, channelType, properties);
				IChannelSession channelSession = LoginSession.GetChannelSession(channelId);
				channelSession.PropertyChanged += OnChannelPropertyChanged;
				channelSession.Participants.AfterKeyAdded += OnParticipantAdded;
				channelSession.Participants.BeforeKeyRemoved += OnParticipantRemoved;
				channelSession.Participants.AfterValueUpdated += OnParticipantValueUpdated;
				channelSession.MessageLog.AfterItemAdded += OnMessageLogRecieved;
				channelSession.BeginConnect(chatCapability != ChatCapability.TextOnly, chatCapability != ChatCapability.AudioOnly, switchTransmission, channelSession.GetConnectToken(_tokenKey, _tokenExpiration), ar =>
				{
					try
					{
						channelSession.EndConnect(ar);
					}
					catch (Exception e)
					{
						VivoxLogError($"Could not connect to voice channel: {e.Message}");
						return;
					}
				});
			}
			else
			{
				VivoxLogError("Cannot join a channel when not logged in.");
			}
		}

		public void SendTextMessage(string messageToSend, ChannelId channel, string applicationStanzaNamespace = null, string applicationStanzaBody = null)
		{
			if (ChannelId.IsNullOrEmpty(channel))
			{
				throw new ArgumentException("Must provide a valid ChannelId");
			}
			if (string.IsNullOrEmpty(messageToSend))
			{
				throw new ArgumentException("Must provide a message to send");
			}
			var channelSession = LoginSession.GetChannelSession(channel);
			channelSession.BeginSendText(null, messageToSend, applicationStanzaNamespace, applicationStanzaBody, ar =>
			{
				try
				{
					channelSession.EndSendText(ar);
				}
				catch (Exception e)
				{
					VivoxLog($"SendTextMessage failed with exception {e.Message}");
				}
			});
		}

		public void DisconnectAllChannels()
		{
			if (ActiveChannels?.Count > 0)
			{
				foreach (var channelSession in ActiveChannels)
				{
					channelSession?.Disconnect();
				}
			}
		}

		private void OnMessageLogRecieved(object sender, QueueItemAddedEventArgs<IChannelTextMessage> textMessage)
		{
			ValidateArgs(new object[] { sender, textMessage });

			IChannelTextMessage channelTextMessage = textMessage.Value;
			VivoxLog(channelTextMessage.Message);
			OnTextMessageLogReceivedEvent?.Invoke(channelTextMessage.Sender.DisplayName, channelTextMessage);
		}

		private void OnLoginSessionPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			if (propertyChangedEventArgs.PropertyName != "State")
			{
				return;
			}
			var loginSession = (ILoginSession)sender;
			LoginState = loginSession.State;
			VivoxLog("Detecting login session change");
			switch (LoginState)
			{
				case LoginState.LoggingIn:
					{
						VivoxLog("Logging in");
						break;
					}
				case LoginState.LoggedIn:
					{
						VivoxLog("Connected to voice server and logged in.");
						OnUserLoggedInEvent?.Invoke();
						SystemEventController.Instance.DispatchSystemEvent(EventVivoxVoiceControllerLoggedIn);
						break;
					}
				case LoginState.LoggingOut:
					{
						VivoxLog("Logging out");
						break;
					}
				case LoginState.LoggedOut:
					{
						VivoxLog("Logged out");
						LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
						SystemEventController.Instance.DispatchSystemEvent(EventVivoxVoiceControllerLoggedOut);
						break;
					}
				default:
					break;
			}
		}

		private void OnParticipantAdded(object sender, KeyEventArg<string> keyEventArg)
		{
			ValidateArgs(new object[] { sender, keyEventArg });

			var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
			var participant = source[keyEventArg.Key];
			var username = participant.Account.Name;
			var channel = participant.ParentChannelSession.Key;
			var channelSession = participant.ParentChannelSession;

			OnParticipantAddedEvent?.Invoke(username, channel, participant);
			
			SystemEventController.Instance.DispatchSystemEvent(EventVivoxVoiceControllerNewParticipant, username, channel.Name, channelSession.IsTransmitting);
		}

		private void OnParticipantRemoved(object sender, KeyEventArg<string> keyEventArg)
		{
			ValidateArgs(new object[] { sender, keyEventArg });

			var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
			var participant = source[keyEventArg.Key];
			var username = participant.Account.Name;
			var channel = participant.ParentChannelSession.Key;
			var channelSession = participant.ParentChannelSession;

			if (participant.IsSelf)
			{
				VivoxLog($"Unsubscribing from: {channelSession.Key.Name}");
				
				channelSession.PropertyChanged -= OnChannelPropertyChanged;
				channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
				channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;
				channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;
				channelSession.MessageLog.AfterItemAdded -= OnMessageLogRecieved;

				var user = _client.GetLoginSession(_accountId);
				user.DeleteChannelSession(channelSession.Channel);
			}

			OnParticipantRemovedEvent?.Invoke(username, channel, participant);
		}

		private static void ValidateArgs(object[] objs)
		{
			foreach (var obj in objs)
			{
				if (obj == null)
					throw new ArgumentNullException(obj.GetType().ToString(), "Specify a non-null/non-empty argument.");
			}
		}

		private void OnParticipantValueUpdated(object sender, ValueEventArg<string, IParticipant> valueEventArg)
		{
			ValidateArgs(new object[] { sender, valueEventArg });

			var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
			var participant = source[valueEventArg.Key];

			string username = valueEventArg.Value.Account.Name;
			ChannelId channel = valueEventArg.Value.ParentChannelSession.Key;
			string property = valueEventArg.PropertyName;

			switch (property)
			{
				case "SpeechDetected":
					{
						VivoxLog($"OnSpeechDetectedEvent: {username} in {channel}.");
						OnSpeechDetectedEvent?.Invoke(username, channel, valueEventArg.Value.SpeechDetected);
						break;
					}
				case "AudioEnergy":
					{
						OnAudioEnergyChangedEvent?.Invoke(username, channel, valueEventArg.Value.AudioEnergy);
						break;
					}
				default:
					break;
			}
		}

		private void OnChannelPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			ValidateArgs(new object[] { sender, propertyChangedEventArgs });

			var channelSession = (IChannelSession)sender;

			if (propertyChangedEventArgs.PropertyName == "AudioState" && channelSession.AudioState == ConnectionState.Disconnected)
			{
				VivoxLog($"Audio disconnected from: {channelSession.Key.Name}");

				foreach (var participant in channelSession.Participants)
				{
					OnSpeechDetectedEvent?.Invoke(participant.Account.Name, channelSession.Channel, false);
				}
			}

			if ((propertyChangedEventArgs.PropertyName == "AudioState" || propertyChangedEventArgs.PropertyName == "TextState") &&
				channelSession.AudioState == ConnectionState.Disconnected &&
				channelSession.TextState == ConnectionState.Disconnected)
			{
				VivoxLog($"Unsubscribing from: {channelSession.Key.Name}");
				channelSession.PropertyChanged -= OnChannelPropertyChanged;
				channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
				channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;
				channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;
				channelSession.MessageLog.AfterItemAdded -= OnMessageLogRecieved;

				var user = _client.GetLoginSession(_accountId);
				user.DeleteChannelSession(channelSession.Channel);

			}
		}

		private void VivoxLog(string msg)
		{
			Debug.Log("<color=green>VivoxVoice: </color>: " + msg);
		}

		private void VivoxLogError(string msg)
		{
			Debug.LogError("<color=green>VivoxVoice: </color>: " + msg);
		}
#endif		
	}
}
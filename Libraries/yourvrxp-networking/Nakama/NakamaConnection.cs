using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if ENABLE_NAKAMA
using Nakama;
using Nakama.TinyJson;
#endif
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.Networking
{
	[Serializable]
#if ENABLE_NAKAMA	
	[CreateAssetMenu]
#endif	
	public class NakamaConnection : ScriptableObject
	{
		public string Scheme = "http";
		public string Host = "localhost";
		public int Port = 7350;
		public string ServerKey = "defaultkey";
#if ENABLE_NAKAMA			
		private const string SessionPrefName = "nakama.session";
		private const string DeviceIdentifierPrefName = "nakama.deviceUniqueIdentifier";

		public IClient Client;
		public ISession Session;
		public ISocket Socket;

		private IChannel m_channel;

		private string m_currentMatchmakingTicket;
		private string m_currentMatchId;

		private bool m_connectedToMainChat = false;

		public IChannel Channel
		{
			get { return m_channel; }
		}
		public bool ConnectedToMainChat
        {
			get { return m_connectedToMainChat; }
        }

		public async Task Connect()
		{
			Client = new Nakama.Client(Scheme, Host, Port, ServerKey, UnityWebRequestAdapter.Instance);

			// Attempt to restore an existing user session.
			if (NakamaController.DebugMessages) Debug.LogError("NakamaController::Connect::SessionPrefName[" + SessionPrefName + "]");

			var authToken = PlayerPrefs.GetString(SessionPrefName);
			if (!string.IsNullOrEmpty(authToken))
			{
				var session = Nakama.Session.Restore(authToken);
				if (!session.IsExpired)
				{
					Session = session;
				}
			}

			// If we weren't able to restore an existing session, authenticate to create a new user session.
			if (Session == null)
			{
				string deviceId;

#if UNITY_EDITOR
				deviceId = yourvrexperience.Utils.Utilities.RandomCodeGeneration("user_" + yourvrexperience.Utils.Utilities.GetTimestamp());
#else
				if (PlayerPrefs.HasKey(DeviceIdentifierPrefName))
				{
					deviceId = PlayerPrefs.GetString(DeviceIdentifierPrefName);
				}
				else
				{
					deviceId = SystemInfo.deviceUniqueIdentifier;
					if (deviceId == SystemInfo.unsupportedIdentifier)
					{
						deviceId = System.Guid.NewGuid().ToString();
					}

					PlayerPrefs.SetString(DeviceIdentifierPrefName, deviceId);
				}
#endif

				Session = await Client.AuthenticateDeviceAsync(deviceId);

				if (NakamaController.DebugMessages) Debug.LogError("NakamaController::Connect::deviceId[" + deviceId + "]");
				PlayerPrefs.SetString(SessionPrefName, Session.AuthToken);
			}

			// Open a new Socket for realtime communication.
			Socket = Client.NewSocket();
			await Socket.ConnectAsync(Session, true);
		}

		public async Task Disconnect(IMatch match)
		{
			await Socket.LeaveMatchAsync(match);

			await Socket.CloseAsync();

			Socket = null;
		}

		public async Task FindMatch(string roomName, int minPlayers, int maxPlayers)
		{
			var matchmakingProperties = new Dictionary<string, string>
			{
				{ "roomname", roomName }
			};

			// Add this client to the matchmaking pool and get a ticket.
			// Debug.LogError("+++++++++++++++++++FindMatch::roomName["+roomName+"]::minPlayers["+minPlayers+"]::maxPlayers["+maxPlayers+"]");
			var matchmakerTicket = await Socket.AddMatchmakerAsync("+properties.roomname:"+ roomName, minPlayers, maxPlayers, matchmakingProperties);
			m_currentMatchmakingTicket = matchmakerTicket.Ticket;
		}

		public async Task JoinMainChat()
		{
			var roomname = "YourVRExperience_Lobby";
			var persistence = true;
			var hidden = false;
			m_channel = await Socket.JoinChatAsync(roomname, ChannelType.Room, persistence, hidden);
			Debug.LogFormat("Now connected to channel id: '{0}'", m_channel.Id);
			m_connectedToMainChat = true;
		}

		public async Task LeaveMainChat()
		{
			if (m_connectedToMainChat)
			{
				m_connectedToMainChat = false;
				await Socket.LeaveChatAsync(m_channel);
				m_channel = null;
			}
		}

		public async Task SendMainChatMessage(string _title, string _description)
		{
			if (m_connectedToMainChat)
            {
				string serializedContent = new Dictionary<string, string> { { _title, _description } }.ToJson();
				if (m_channel != null)
				{
					var sendAck = await Socket.WriteChatMessageAsync(m_channel.Id, serializedContent);
				}
			}
		}

		public async Task CancelMatchmaking()
		{
			await Socket.RemoveMatchmakerAsync(m_currentMatchmakingTicket);
		}
#endif		
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Networking;
using yourvrexperience.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.Networking
{
	public class BasicConnection : MonoBehaviour
	{
		public const string EventBasicConnectionMessageText = "EventBasicConnectionMessageText";

		public Text SDKState;
		public Text TitleServer;
		public Text InformationState;
		public InputField InputData;
		public Button StartHost;
		public Button ConnectToHost;
		public Button Disconnect;
		public Button SendEvent;
		public Button ClearInfo;

		private bool _isHost;
		private bool _isConnected;
		private bool _hasStartedSession = false;
		
		protected virtual void Start()
		{
			NetworkController.Instance.Initialize();

			StartHost.onClick.AddListener(OnStartHost);
			ConnectToHost.onClick.AddListener(OnConnectToHost);
			Disconnect.onClick.AddListener(OnDisconnect);
			if (SendEvent != null) SendEvent.onClick.AddListener(OnSendEvent);
			if (ClearInfo != null) ClearInfo.onClick.AddListener(OnClearInfo);
			if (InputData != null)
			{
				NetworkController.Instance.ServerAddress = "localhost";
				InputData.text = NetworkController.Instance.ServerAddress;
			} 

			NetworkController.Instance.NetworkEvent += OnNetworkEvent;
			SystemEventController.Instance.Event += OnSystemEvent;
			ChangeState(false);
			ReadyToConnect();
		}

		private void ReadyToConnect()
		{
			string messageReady = "Ready to connect";
#if ENABLE_PHOTON
            messageReady += "[PHOTON]";
#elif ENABLE_MIRROR
            messageReady += "[MIRROR]";
#elif ENABLE_NAKAMA
            messageReady += "[NAKAMA]";
#elif ENABLE_NETCODE
            messageReady += "[NETCODE]";
#endif
			InformationState.text = messageReady;
		}


		protected virtual void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
		}

		protected virtual void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(NetworkController.EventNetworkControllerDebugSDKConnection))
			{
				string message = (string)parameters[0];
				if (SDKState != null) SDKState.text = message;
			}
		}

		protected virtual void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			if (nameEvent.Equals(NetworkController.EventNetworkControllerListRoomsUpdated))
			{
				if (!_hasStartedSession)
				{
					_hasStartedSession = true;
					if (_isHost)
					{
						NetworkController.Instance.CreateRoom("BasicConnection", 2);
						InformationState.text += "\n Creating room...";
					}
					else
					{
						NetworkController.Instance.JoinRoom("BasicConnection");
						InformationState.text += "\n Joining room...";
					}
				}
			}
			if (nameEvent.Equals(NetworkController.EventNetworkControllerConnectionWithRoom))
			{
				ChangeState(true);
				InformationState.text += "\n Connected with netID["+(int)parameters[0]+"]!";
				NetworkController.Instance.DelayNetworkEvent(NetworkController.EventNetworkControllerClientLevelReady,  0.2f, -1, -1, !NetworkController.Instance.IsServer);
			}
			if (nameEvent.Equals(NetworkController.EventNetworkControllerNewPlayerJoinedRoom))
			{
				InformationState.text += "\n New user["+(int)parameters[0]+"] joined...";
			}
			if (nameEvent.Equals(NetworkController.EventNetworkControllerPlayerDisconnected))
			{
				int netIDDisconnected = -1;
				if (parameters != null)
				{
					if (parameters.Length > 0)
					{
						netIDDisconnected = (int)parameters[0];
					}
				}
				if (netIDDisconnected != -1)
				{
					InformationState.text += "\n A user["+netIDDisconnected+"] has disconnected...";
				}
				else
				{
					InformationState.text += "\n A user has disconnected...";
				}
			}
			if (nameEvent.Equals(EventBasicConnectionMessageText))
			{
				InformationState.text += "\n User["+originNetworkID+"] > " + (string)parameters[0];
			}
		}

		private void OnStartHost()
		{
			_isHost = true;
			if (InputData != null)
			{
				NetworkController.Instance.ServerAddress = InputData.text;
			}		
			NetworkController.Instance.Connect();
			InformationState.text = "Now starting host...";
		}

		private void OnConnectToHost()
		{
			_isHost = false;
			if (InputData != null)
			{
				NetworkController.Instance.ServerAddress = InputData.text;
			}		
			NetworkController.Instance.Connect();
			InformationState.text = "Now connecting to host...";
		}

		private void OnDisconnect()
		{
			_isHost = false;
			Disconnect.gameObject.SetActive(false);
			NetworkController.Instance.Disconnect();
			InformationState.text += "\n Disconnecting...";
			Invoke("DisconnectedSuccessfully", 1);
		}

		private void OnSendEvent()
		{
			string dataToSend = InputData.text;
			InputData.text = "";
			NetworkController.Instance.DispatchNetworkEvent(EventBasicConnectionMessageText, NetworkController.Instance.UniqueNetworkID, -1, dataToSend);
		}

		private void OnClearInfo()
		{
			InformationState.text = "";
		}

		private void DisconnectedSuccessfully()
		{
			ChangeState(false);
			ReadyToConnect();
		}

		private void ChangeState(bool isConnected)
		{
			_isConnected = isConnected;
			if (_isConnected)
			{
				StartHost.gameObject.SetActive(false);
				ConnectToHost.gameObject.SetActive(false);
				Disconnect.gameObject.SetActive(true);
				if (SendEvent != null) SendEvent.gameObject.SetActive(true);
				if (ClearInfo != null) ClearInfo.gameObject.SetActive(true);
				if (TitleServer != null) TitleServer.text = "Message:";		
				if (InputData != null) InputData.text = "";	
			}
			else
			{
				StartHost.gameObject.SetActive(true);
				ConnectToHost.gameObject.SetActive(true);
				Disconnect.gameObject.SetActive(false);
				if (SendEvent != null) SendEvent.gameObject.SetActive(false);
				if (ClearInfo != null) ClearInfo.gameObject.SetActive(false);
				if (TitleServer != null) TitleServer.text = "Server Address:";
				_hasStartedSession = false;
			}
		}
	}
}
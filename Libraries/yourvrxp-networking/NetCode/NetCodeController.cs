using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;
#if ENABLE_NETCODE
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
#endif
using UnityEngine.SceneManagement;

namespace yourvrexperience.Networking
{
#if ENABLE_NETCODE
	[RequireComponent(typeof(UnityTransport))]
#endif	
    public class NetCodeController : MonoBehaviour
    {
#if ENABLE_NETCODE
		public const string EventNetcodeControllerNetworkAvatarInited = "EventNetcodeControllerNetworkAvatarInited";
        public const string EventNetcodeControllerLocalConnection = "EventNetcodeControllerLocalConnection";
        public const string EventNetcodeControllerNewClientConnection = "EventNetcodeControllerNewClientConnection";

        private static NetCodeController _instance;

        public static NetCodeController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType<NetCodeController>();
                }
                return _instance;
            }
        }

		public NetCodeConnection PrefabConnection;

        private bool _isServer = false;
        private bool _isConnected = false;
		private bool _isInited = false;
		private bool _isInRoom = false;
        private NetCodeConnection _netcodeConnection;

		private int _instanceCounter = 0;

		private int _networkID = -1;

        public int UniqueNetworkID
        {
            get { return _networkID; }
        }

        public NetCodeConnection Connection
        {
            get { return _netcodeConnection; }
        }

        public bool IsConnected
        {
            get { return _isConnected; }
        }

		public bool IsInRoom
		{
			get { return _isInRoom; }
		}

        public bool IsServer
        {
            get { return _isServer; }
        }

		public int InstanceCounter
		{
			get { return _instanceCounter; }
			set { 
				if (IsServer)
				{
					_instanceCounter = value; 
				}				
			}
		}
		public string ServerAddress
        {
            get { return this.GetComponent<UnityTransport>().ConnectionData.Address; }
            set { 
				if (value.Equals("localhost") || (value.Length == 0))
				{
					this.GetComponent<UnityTransport>().ConnectionData.Address = "127.0.0.1"; 
				}
				else
				{
					this.GetComponent<UnityTransport>().ConnectionData.Address = value; 
				}				
			}
        }

        public void Initialize()
        {
			_isInited = true;
        }

        public void Connect()
        {
			_isInited = true;
			SystemEventController.Instance.Event += OnSystemEvent;
			NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
			NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;

			NetworkController.Instance.DelayEvent(NetworkController.EventNetworkControllerListRoomsUpdated, 0.2f, new List<RoomData>());
        }

		void OnDestroy()
		{
			Disconnect();
			Destroy();
		}

		public void Disconnect()
		{
			if (_isInited)
			{
				_isInited = false;
				_isServer = false;
				_isConnected = false;
				_instanceCounter = 0;
				_networkID = -1;

				NetworkBehaviour[] networkGOs = GameObject.FindObjectsOfType<NetworkBehaviour>();
				for (int i = 0; i < networkGOs.Length; i++)
				{
					if (networkGOs[i] != null)
					{
						GameObject.Destroy(networkGOs[i].gameObject);
					}
				}

				if (NetworkManager.Singleton != null)
				{
					NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
					NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;

					NetworkManager.Singleton.Shutdown();
				}

				if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			}
		}

		public void Destroy()
		{
			if (Instance)
			{
				_instance = null;
				GameObject.Destroy(this.gameObject);

				if (_netcodeConnection != null)
				{
					GameObject.Destroy(_netcodeConnection.gameObject);
				}
			}
		}

		public void ServerRpcNetworkObject(string uniqueNetworkName, string prefab, Vector3 position, int owner, params object[] parameters)
        {
			string initialData = "";
            string initialTypes = "";
            NetworkUtils.Serialize(parameters, ref initialData, ref initialTypes);
            _netcodeConnection.NetworkObjectServerRpc(uniqueNetworkName, prefab, position, owner, initialData, initialTypes);
        }

        public void DispatchNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, params object[] parameters)
        {
            string types = "";
            string output = "";
            NetworkUtils.Serialize(parameters, ref output, ref types);
            _netcodeConnection.MessageFromClientsToServerServerRpc(nameEvent, originNetworkID, targetNetworkID, output, types);
        }

		public void LoadNewScene(string nextScene, string previousScene)
		{
			if (IsServer) NetworkManager.Singleton.SceneManager.LoadScene(nextScene, LoadSceneMode.Additive);
			if ((previousScene != null) && (previousScene.Length > 0)) SceneManager.UnloadSceneAsync(previousScene);
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(EventNetcodeControllerLocalConnection))
            {
                _netcodeConnection = (NetCodeConnection)parameters[0];
				_isInRoom = true;
                NetworkController.Instance.DispatchEvent(NetworkController.EventNetworkControllerConnectionWithRoom, (int)parameters[1]);
            }
            if (nameEvent.Equals(EventNetcodeControllerNewClientConnection))
            {
				NetworkController.Instance.DispatchEvent(NetworkController.EventNetworkControllerNewPlayerJoinedRoom, (int)parameters[0]);
            }
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))
			{
				if (Instance)
				{
					DontDestroyOnLoad(Instance.gameObject);
				}
			}
        }

		public void CreateRoom(string nameRoom, int totalNumberOfPlayers)
        {
			NetworkManager.Singleton.StartHost();
        }

		private void HandleClientDisconnected(ulong obj)
		{
		}

		private void HandleClientConnected(ulong netID)
		{
			if (_networkID == -1)
			{
				_networkID = (int)netID;
				_isConnected = true;
				_isServer = NetworkManager.Singleton.IsHost;
			}			
		}

		public void JoinRoom(string room)
        {
			NetworkManager.Singleton.StartClient();
        }
#endif		
	}
}

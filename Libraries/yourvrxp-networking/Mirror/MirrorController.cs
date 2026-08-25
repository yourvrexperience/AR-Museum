#if ENABLE_MIRROR
using Mirror;
using Mirror.Discovery;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;
#if ENABLE_DISSONANCE_MIRROR
using Dissonance;
using Dissonance.Integrations.MirrorIgnorance;
#endif

namespace yourvrexperience.Networking
{
#if ENABLE_MIRROR
	public struct CreateMirrorConnection : NetworkMessage
	{
		public string name;
		public bool isVRPlayer;
	}
#endif

    public class MirrorController : 
#if ENABLE_MIRROR	
	NetworkManager
#else
	MonoBehaviour	
#endif	
    {
#if ENABLE_MIRROR		
		public const bool DebugMessages = true;

		public const string EventMirrorNetworkAvatarInited = "EventMirrorNetworkAvatarInited";
        public const string EventMirrorNetworkLocalConnection = "EventMirrorNetworkLocalConnection";
        public const string EventMirrorNetworkNewClientConnection = "EventMirrorNetworkNewClientConnection";

        private static MirrorController _instance;

        public static MirrorController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType<MirrorController>();
                }
                return _instance;
            }
        }

        private NetworkDiscovery _networkDiscovery;
        private bool _discovering = false;
        private bool _isServer = false;
        private bool _isConnected = false;
		private bool _isInRoom = false;
		private bool _isInited = false;
        private MirrorConnection _mirrorConnection;

		private int _instanceCounter = 0;

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

        public int UniqueNetworkID
        {
            get
            {
                if (_mirrorConnection != null)
                {
                    return (int)_mirrorConnection.netId;
                }
                else
                {
                    return -1;
                }
            }
        }

        public MirrorConnection Connection
        {
            get { return _mirrorConnection; }
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
		public string ServerAddress
        {
            get { return networkAddress; }
            set { networkAddress = value; }
        }

#if ENABLE_DISSONANCE_MIRROR
		public override void Awake()
		{
			base.Awake();
			if (playerPrefab.GetComponent<VoiceBroadcastTrigger>() == null) playerPrefab.AddComponent<VoiceBroadcastTrigger>();
			if (playerPrefab.GetComponent<MirrorIgnorancePlayer>() == null) playerPrefab.AddComponent<MirrorIgnorancePlayer>();
			playerPrefab.GetComponent<VoiceBroadcastTrigger>().RoomName = "Global";
		}
#endif		

        public void Initialize()
        {
			_isInited = true;

			SystemEventController.Instance.Event += OnSystemEvent;
        }

        public void Connect()
        {
			_isInited = true;
			NetworkController.Instance.DelayEvent(NetworkController.EventNetworkControllerListRoomsUpdated, 0.2f, new List<RoomData>());
        }

		public void Disconnect()
		{
			if (_isInited)
			{
				_isInited = false;
                _discovering = false;
                _isServer = false;
                _isConnected = false;
				_isInRoom = false;

				NetworkBehaviour[] networkGOs = GameObject.FindObjectsOfType<NetworkBehaviour>();
				for (int i = 0; i < networkGOs.Length; i++)
				{
					if (networkGOs[i] != null)
					{
						GameObject.Destroy(networkGOs[i].gameObject);
					}
				}

				if (this.mode == NetworkManagerMode.Host)
				{
					StopHost();
				}
				else
				{
					StopClient();
				}

				if (_networkDiscovery != null)
				{
					_networkDiscovery.StopDiscovery();
				}
			}			
		}

		public void Destroy()
		{
			if (Instance)
			{
				if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;

				_instance = null;
				_networkDiscovery = null;
				GameObject.Destroy(this.gameObject);
				if (DebugMessages) Debug.LogError("%%%%%%%%%%%%%%%%%%%%%%%%%%% SUCCESS DETRUCTION MIRROR CONTROLLER");
			}
		}

        public override void OnDestroy()
        {
            base.OnDestroy();

			Disconnect();
			Destroy();
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(MirrorController.EventMirrorNetworkLocalConnection))
            {
				_isInRoom = true;
                _mirrorConnection = (MirrorConnection)parameters[0];
				if (NetworkController.Instance.IsMultipleScene)
				{
					DontDestroyOnLoad(_mirrorConnection.gameObject);
				}
				NetworkController.Instance.DispatchEvent(NetworkController.EventNetworkControllerConnectionWithRoom, (int)parameters[1]);
            }
            if (nameEvent.Equals(MirrorController.EventMirrorNetworkNewClientConnection))
            {
				NetworkController.Instance.DispatchEvent(NetworkController.EventNetworkControllerNewPlayerJoinedRoom, (int)parameters[0]);
            }
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))
			{
				if (_mirrorConnection != null)
				{
					DontDestroyOnLoad(_mirrorConnection.gameObject);
				}				
			}
        }

        public void OnDiscoveredServer(ServerResponse info)
        {
            if (_discovering)
            {
                _discovering = false;
                _isServer = false;
				_isConnected = true;
				_networkDiscovery.StopDiscovery();
				
                StartClient(info.uri);
                if (DebugMessages) Debug.LogError("%%%%%%%%%% MirrorDiscoveryController::STARTED AS A CLIENT (MIRROR) CONNECTED TO SERVER[" + info.EndPoint.Address.ToString() + "].");
            }
        }

        private void CancelDiscovery()
        {
            if (_discovering)
            {
                _discovering = false;
                _networkDiscovery.StopDiscovery();
                _isServer = true;
				_isConnected = true;

                StartHost();
                _networkDiscovery.AdvertiseServer();
                if (DebugMessages) Debug.LogError("%%%%%%%%%% MirrorDiscoveryController::STARTED AS A SERVER (MIRROR).");
            }
        }

        public void CmdNetworkObject(string uniqueNetworkName, string prefab, Vector3 position, int owner, params object[] parameters)
        {
            string initialData = "";
            string initialTypes = "";
			if (_mirrorConnection != null)
			{
				NetworkUtils.Serialize(parameters, ref initialData, ref initialTypes);
				_mirrorConnection.CmdNetworkObject(uniqueNetworkName, prefab, position, owner, initialData, initialTypes);
			}
        }

        public void TakeNetworkAuthority(NetworkIdentity target)
        {
			if (_mirrorConnection != null)
			{
            	_mirrorConnection.CmdAssignNetworkAuthority(target, _mirrorConnection.GetComponent<NetworkIdentity>());
			}
        }

        public void DispatchNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, params object[] parameters)
        {
            string types = "";
            string output = "";
			if (_mirrorConnection != null)
			{
				NetworkUtils.Serialize(parameters, ref output, ref types);
				_mirrorConnection.CmdMessageFromClientsToServer(nameEvent, originNetworkID, targetNetworkID, output, types);
			}            
        }

		private bool StartDiscovery(string nameRoom)
		{
			if (_networkDiscovery == null)
            {
                _networkDiscovery = GetComponent<NetworkDiscovery>();
				if ((nameRoom != null) && (nameRoom.Length > 0))
				{
					MirrorCustomDiscovery customDiscover = GetComponent<MirrorCustomDiscovery>();
					if (customDiscover != null)
					{
						int finalPort = yourvrexperience.Utils.Utilities.StringToHash(nameRoom, 40000);
						customDiscover.SetUpDiscoveryPort(finalPort);
					}					
				}				
				if (_networkDiscovery != null)
				{
					_networkDiscovery.OnServerFound.AddListener(OnDiscoveredServer);
				}
            }

			if (_networkDiscovery != null)
			{
				_discovering = true;
				_isConnected = false;
				_networkDiscovery.StartDiscovery();

				Invoke("CancelDiscovery", 3);

				if (DebugMessages) Debug.LogError("%%%%%%%%%% MirrorDiscoveryController::START SEARCHING FOR A SERVER...");
				return true;
			}
			else
			{
				return false;
			}
		}

		public void CreateRoom(string nameRoom, int totalNumberOfPlayers)
        {
			if (!StartDiscovery(nameRoom))
			{
				_discovering = false;
                _isServer = true;
				_isConnected = true;

				StartHost();
			}
        }

        public void JoinRoom(string room)
        {
			if (!StartDiscovery(room))
			{                
				_discovering = false;
                _isServer = false;
				_isConnected = true;

				StartClient();
			}
        }

		public override void OnStartServer()
		{
			base.OnStartServer();

			NetworkServer.RegisterHandler<CreateMirrorConnection>(OnCreateMirrorConnection);
		}
		
		public override void OnClientConnect()
		{
			NetworkConnection conn = NetworkClient.connection;
			base.OnClientConnect();

			CreateMirrorConnection createPlayerMessage = new CreateMirrorConnection
			{
				name = "Player",
				isVRPlayer = false
			};
			conn.Send(createPlayerMessage);
		}

		private void OnCreateMirrorConnection(NetworkConnectionToClient conn, CreateMirrorConnection player)
		{
			GameObject mirroConnection = Instantiate(playerPrefab);
			NetworkServer.AddPlayerForConnection(conn, mirroConnection);
		}
#endif
	}
}

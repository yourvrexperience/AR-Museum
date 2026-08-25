using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.Collections;
using UnityEngine.SceneManagement;
using yourvrexperience.Utils;

namespace yourvrexperience.Networking
{
    public class NetworkController : MonoBehaviour, IDontDestroy
    {
		public const string EventNetworkControllerConnectionEstablishment = "EventNetworkControllerConnectionEstablishment";
		public const string EventNetworkControllerListRoomsUpdated = "EventNetworkControllerListRoomsUpdated";		
		public const string EventNetworkControllerListRoomsConfirmedUpdated = "EventNetworkControllerListRoomsConfirmedUpdated";		
		public const string EventNetworkControllerConnectionWithRoom = "EventNetworkControllerConnectionWithRoom";
		public const string EventNetworkControllerConfirmationConnectionWithRoom = "EventNetworkControllerConfirmationConnectionWithRoom";
		public const string EventNetworkControllerDestroyCommunications = "EventNetworkControllerDestroyCommunications";
		public const string EventNetworkControllerPlayerHasBeenDestroyed = "EventNetworkControllerPlayerHasBeenDestroyed";
		public const string EventNetworkControllerNewPlayerJoinedRoom = "EventNetworkControllerNewPlayerJoinedRoom";
		public const string EventNetworkControllerPlayerDisconnected = "EventNetworkControllerPlayerDisconnected";
		public const string EventNetworkControllerServerConnectionError = "EventNetworkControllerServerConnectionError";
		public const string EventNetworkControllerDebugSDKConnection = "EventNetworkControllerDebugSDKConnection";
		public const string EventNetworkControllerDisconnected = "EventNetworkControllerDisconnected";
		public const string EventNetworkControllerClientLevelReady = "EventNetworkControllerClientLevelReady";
		public const string EventNetworkControllerRequestTotalConnectedPlayers = "EventNetworkControllerRequestTotalConnectedPlayers";
		public const string EventNetworkControllerReportTotalConnectedPlayers = "EventNetworkControllerReportTotalConnectedPlayers";
		public const string EventNetworkControllerRequestInitialIndexPosition = "EventNetworkControllerRequestInitialIndexPosition";
		public const string EventNetworkControllerResponseInitialIndexPosition = "EventNetworkControllerResponseInitialIndexPosition";
		public const string EventNetworkControllerResetInitialIndexPosition = "EventNetworkControllerResetInitialIndexPosition";
		public const string EventNetworkControllerReportCurrentState = "EventNetworkControllerReportCurrentState";
		
		public const string TokenSeparatorEvents = "<event>";

		public const string DisconnectScene = "DisconnectScene";

        public const bool DEBUG = true;

        public delegate void MultiplayerNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, params object[] parameters);

        public event MultiplayerNetworkEvent NetworkEvent;

        public void DispatchEvent(string nameEvent, int originNetworkID, int targetNetworkID, params object[] parameters)
        {
            if (NetworkEvent != null) NetworkEvent(nameEvent, originNetworkID, targetNetworkID, parameters);
        }
        public void DispatchEvent(string nameEvent, params object[] parameters)
        {
            if (NetworkEvent != null) NetworkEvent(nameEvent, -1, -1, parameters);
        }

		public void DelayEvent(string nameEvent, float delay, params object[] parameters)
		{
			if (NetworkEvent == null) return;

			StartCoroutine(DelayedEventExecution(nameEvent, delay, parameters));
		}

		IEnumerator DelayedEventExecution(string nameEvent, float delay, params object[] parameters)
		{
			yield return new WaitForSeconds(delay);

			if (NetworkEvent != null) NetworkEvent(nameEvent, -1, -1, parameters);
		}

        private static NetworkController _instance;
        public static NetworkController Instance
        {
            get
            {
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(NetworkController)) as NetworkController;
				}
                return _instance;
            }
        }

		[SerializeField] private GameObject mirrorController;
		[SerializeField] private GameObject photonController;
		[SerializeField] private GameObject netCodeController;
		[SerializeField] private GameObject nakamaController;
		[SerializeField] private GameObject socketsController;

		[SerializeField] private GameObject[] networkPrefabs;


        private int _networkInstanceCounter = 0;
		private bool _hasBeenInited = false;
        private bool _isMultiplayer = false;
		private bool _hasEstablishedConnection = false;
		private bool _isMultipleScene = false;
		private List<TimedEventData> _listEvents = new List<TimedEventData>();
		private List<RoomData> _roomsLobby;
		private string _nameRoom;
		private Dictionary<string, Dictionary<string, string>> _networkObjectInitData = new Dictionary<string, Dictionary<string, string>>();
		private List<int> _connections = new List<int>();
		private int _indexPositionInRoom;

		public int NetworkInstanceCounter
		{
			get { return _networkInstanceCounter; }
			set { _networkInstanceCounter = value;}
		}

		public List<RoomData> RoomsLobby
		{
			get { return _roomsLobby; }
		}
		public List<int> Connections
		{
			get { return _connections; }
		}
		public string NameRoom
		{
			get { return _nameRoom; }
		}
		public bool IsMultipleScene
		{
			get { return _isMultipleScene; }
		}
        public string ServerAddress
        {
            get
            {
#if ENABLE_PHOTON
                	return PhotonController.Instance.ServerAddress;
#elif ENABLE_MIRROR
                	return MirrorController.Instance.ServerAddress;
#elif ENABLE_NAKAMA
                	return NakamaController.Instance.ServerAddress;				
#elif ENABLE_NETCODE
                	return NetCodeController.Instance.ServerAddress;	
#elif ENABLE_SOCKETS
                	return SocketsController.Instance.ServerAddress;	
#else
					return null;					
#endif
            }

            set
            {
				if ((value != null) && (value.Length > 0))
				{
#if ENABLE_PHOTON
                	PhotonController.Instance.ServerAddress = value;
#elif ENABLE_MIRROR
                	MirrorController.Instance.ServerAddress = value;
#elif ENABLE_NAKAMA
                	NakamaController.Instance.ServerAddress = value;				
#elif ENABLE_NETCODE
                	NetCodeController.Instance.ServerAddress = value;		
#elif ENABLE_SOCKETS
                	SocketsController.Instance.ServerAddress = value;	
#endif
				}
            }
        }
        public int UniqueNetworkID
        {
            get
            {
				if (!_hasEstablishedConnection)
				{
					return -1;
				}
				else
				{
#if ENABLE_PHOTON
                	return PhotonController.Instance.UniqueNetworkID;
#elif ENABLE_MIRROR
                	return MirrorController.Instance.UniqueNetworkID;
#elif ENABLE_NAKAMA
                	return NakamaController.Instance.UniqueNetworkID;				
#elif ENABLE_NETCODE
                	return NetCodeController.Instance.UniqueNetworkID;		
#elif ENABLE_SOCKETS
                	return SocketsController.Instance.UniqueNetworkID;
#else
            		return -1;
#endif
				}
            }
        }
        public bool IsServer
        {
            get
            {
				if (!_hasEstablishedConnection)
				{
					return false;
				}
				else
				{
#if ENABLE_PHOTON
                	if (PhotonController.Instance!=null) return PhotonController.Instance.IsServer;
#elif ENABLE_MIRROR
                	if (MirrorController.Instance!=null) return MirrorController.Instance.IsServer;
#elif ENABLE_NAKAMA
                	if (NakamaController.Instance!=null) return NakamaController.Instance.IsServer;				
#elif ENABLE_NETCODE
                	if (NetCodeController.Instance!=null) return NetCodeController.Instance.IsServer;		
#elif ENABLE_SOCKETS
                	if (SocketsController.Instance!=null) return SocketsController.Instance.IsServer;
#endif
            		return false;
				}
            }
        }
        public bool IsConnected
        {
            get
            {
                if (!_isMultiplayer)
                {
                    return false;
                }
                else
                {
					if (!_hasEstablishedConnection)
					{
						return false;
					}
					else
					{
#if ENABLE_PHOTON
                		return PhotonController.Instance.IsConnected;
#elif ENABLE_MIRROR
                		return MirrorController.Instance.IsConnected;
#elif ENABLE_NAKAMA
                		return NakamaController.Instance.IsConnected;				
#elif ENABLE_NETCODE
                		return NetCodeController.Instance.IsConnected;				
#elif ENABLE_SOCKETS
                		return SocketsController.Instance.IsConnected;
#else
            			return false;
#endif
					}
                }
            }
        }
        public bool IsInLobby
        {
            get
            {
                if (!_isMultiplayer)
                {
                    return false;
                }
                else
                {
					if (!_hasEstablishedConnection)
					{
						return false;
					}
					else
					{
#if ENABLE_PHOTON
                		return PhotonController.Instance.IsInLobby;
#elif ENABLE_MIRROR
                		return true;
#elif ENABLE_NAKAMA
                		return NakamaController.Instance.IsInLobby;				
#elif ENABLE_NETCODE
                		return true;	
#elif ENABLE_SOCKETS
                		return SocketsController.Instance.IsInLobby;
#else
            			return false;
#endif
					}
                }
            }
        }

        public bool IsInRoom
        {
            get
            {
                if (!_isMultiplayer)
                {
                    return false;
                }
                else
                {
					if (!_hasEstablishedConnection)
					{
						return false;
					}
					else
					{
#if ENABLE_PHOTON
                		return PhotonController.Instance.IsInRoom;
#elif ENABLE_MIRROR
                		return MirrorController.Instance.IsInRoom;
#elif ENABLE_NAKAMA
                		return NakamaController.Instance.IsInRoom;				
#elif ENABLE_NETCODE
                		return NetCodeController.Instance.IsInRoom;				
#elif ENABLE_SOCKETS
                		return SocketsController.Instance.IsInRoom;
#else
            			return false;
#endif
					}
                }
            }
        }

        public bool IsMultiplayer
        {
            set { _isMultiplayer = value; }
        }

		public bool CanDestroyOnLoad 
		{
			get { return false; }
		}

		void Awake()
		{
#if ENABLE_PHOTON
			if ((GameObject.FindObjectOfType<PhotonController>() == null) && (photonController != null))
			{
				Instantiate(photonController);
			}
#elif ENABLE_MIRROR
			if ((GameObject.FindObjectOfType<MirrorController>() == null) && (mirrorController != null))
			{
				Instantiate(mirrorController);
			}
#elif ENABLE_NAKAMA
			if ((GameObject.FindObjectOfType<NakamaController>() == null) && (nakamaController != null))
			{
				Instantiate(nakamaController);
			}
#elif ENABLE_NETCODE
			if ((GameObject.FindObjectOfType<NetCodeController>() == null) && (netCodeController != null))
			{
				Instantiate(netCodeController);
			}
#elif ENABLE_SOCKETS
			if ((GameObject.FindObjectOfType<SocketsController>() == null) && (socketsController != null))
			{
				Instantiate(socketsController);
			}
#endif
		}

		void OnDestroy()
		{
			Disconnect();
			Destroy();
		}

        public void Initialize()
        {
			System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
			customCulture.NumberFormat.NumberDecimalSeparator = ".";
			System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

			_hasBeenInited = true;
			SystemEventController.Instance.Event += OnSystemEvent;
			NetworkEvent += OnNetworkEvent;
#if ENABLE_PHOTON
            PhotonController.Instance.Initialize();
#elif ENABLE_MIRROR
            MirrorController.Instance.Initialize();
#elif ENABLE_NAKAMA
			NakamaController.AllowInstance = true;
			NakamaController.Instance.Initialize();
#elif ENABLE_NETCODE
            NetCodeController.Instance.Initialize();
#elif ENABLE_SOCKETS
			SocketsController.Instance.Initialize();
#endif
        }

		public void Connect()
        {
			IsMultiplayer = true;
#if ENABLE_PHOTON
            PhotonController.Instance.Connect();
#elif ENABLE_MIRROR
            MirrorController.Instance.Connect();
#elif ENABLE_NAKAMA
			NakamaController.Instance.Connect();
#elif ENABLE_NETCODE
            NetCodeController.Instance.Connect();
#elif ENABLE_SOCKETS
			SocketsController.Instance.Connect();
#endif
        }

		public void Disconnect()
		{
			if (_isMultiplayer)
			{
				_isMultiplayer = false;
				_hasEstablishedConnection = false;				
				_connections.Clear();
				_indexPositionInRoom = 0;

#if ENABLE_PHOTON
				if (PhotonController.Instance != null) PhotonController.Instance.Disconnect();
#elif ENABLE_MIRROR
				if (MirrorController.Instance != null) MirrorController.Instance.Disconnect();
#elif ENABLE_NAKAMA
				if (NakamaController.Instance != null) NakamaController.Instance.Disconnect();
#elif ENABLE_NETCODE
				if (NetCodeController.Instance != null) NetCodeController.Instance.Disconnect();
#elif ENABLE_SOCKETS
				if (SocketsController.Instance != null) SocketsController.Instance.Disconnect();
#endif			

				DispatchEvent(EventNetworkControllerDisconnected);
			}
		}

		public void Destroy()
		{
			if (_hasBeenInited)
			{
				_hasBeenInited = false;
				if (SystemEventController.Instance != null)	SystemEventController.Instance.Event -= OnSystemEvent;
				NetworkEvent -= OnNetworkEvent;

#if ENABLE_PHOTON
				if (PhotonController.Instance != null) PhotonController.Instance.Destroy();
#elif ENABLE_MIRROR
				if (MirrorController.Instance != null) MirrorController.Instance.Destroy();
#elif ENABLE_NAKAMA
				if (NakamaController.Instance != null) NakamaController.Instance.Destroy();
#elif ENABLE_NETCODE
				if (NetCodeController.Instance != null) NetCodeController.Instance.Destroy();
#elif ENABLE_SOCKETS
				if (SocketsController.Instance != null) SocketsController.Instance.Destroy();
#endif			

				if (Instance)
				{
					_instance = null;
					GameObject.Destroy(this.gameObject);
				}
			}
		}

		public void CreateRoom(string nameRoom, int totalPlayers)
		{
			_nameRoom = nameRoom;
#if ENABLE_PHOTON
            PhotonController.Instance.CreateRoom(nameRoom, totalPlayers);
#elif ENABLE_MIRROR
            MirrorController.Instance.CreateRoom(nameRoom, totalPlayers);
#elif ENABLE_NAKAMA
			NakamaController.Instance.CreateRoom(nameRoom, totalPlayers);
#elif ENABLE_NETCODE
            NetCodeController.Instance.CreateRoom(nameRoom, totalPlayers);
#elif ENABLE_SOCKETS
			SocketsController.Instance.CreateRoom(nameRoom, totalPlayers);
#endif			
		}

		public void JoinRoom(string nameRoom)
		{
			_nameRoom = nameRoom;
#if ENABLE_PHOTON
            PhotonController.Instance.JoinRoom(nameRoom);
#elif ENABLE_MIRROR
            MirrorController.Instance.JoinRoom(nameRoom);
#elif ENABLE_NAKAMA
			NakamaController.Instance.JoinRoom(nameRoom);
#elif ENABLE_NETCODE
            NetCodeController.Instance.JoinRoom(nameRoom);
#elif ENABLE_SOCKETS
			SocketsController.Instance.JoinRoom(nameRoom);
#endif			
		}


        public string CreateNetworkPrefab(bool refresh, string namePrefab, GameObject prefab, string pathToPrefab, Vector3 position, Quaternion rotation, byte data, params object[] parameters)
        {
			if (!_hasEstablishedConnection) return null;

			_networkInstanceCounter++;
            string uniqueNetworkName = namePrefab + "::instance[" + _networkInstanceCounter + "]::owner["+UniqueNetworkID+"]";
#if ENABLE_PHOTON
            PhotonController.Instance.CreateNetworkPrefab(_isMultipleScene, uniqueNetworkName, prefab, pathToPrefab, position, rotation, data, parameters);
#elif ENABLE_MIRROR
            MirrorController.Instance.CmdNetworkObject(uniqueNetworkName, pathToPrefab, position, UniqueNetworkID, parameters);
#elif ENABLE_NAKAMA
			int indexPrefab = NakamaController.Instance.GetIndexPrefab(prefab);
			NakamaController.Instance.CreateNetworkPrefab(UniqueNetworkID, indexPrefab, uniqueNetworkName, position, rotation);
#elif ENABLE_NETCODE
            NetCodeController.Instance.ServerRpcNetworkObject(uniqueNetworkName, pathToPrefab, position, UniqueNetworkID, parameters);
#elif ENABLE_SOCKETS
			int indexPrefab = SocketsController.Instance.GetIndexPrefab(prefab);
			SocketsController.Instance.CreateNetworkPrefab(UniqueNetworkID, indexPrefab, uniqueNetworkName, position, rotation);
#endif

			if (refresh)
			{
#if ENABLE_NAKAMA || ENABLE_SOCKETS
				DelayNetworkEvent(EventNetworkControllerClientLevelReady, 0.5f, -1, -1, IsServer);
#else				
				DelayNetworkEvent(EventNetworkControllerClientLevelReady, 0.2f, -1, -1, IsServer);
#endif				
			}

            return uniqueNetworkName;
        }

		public void LoadNewScene(string nextScene, string previousScene)
		{
			_isMultipleScene = true;
			SystemEventController.Instance.DispatchSystemEvent(SystemEventController.EventSystemEventControllerDontDestroyOnLoad);
			
#if ENABLE_PHOTON
			PhotonController.Instance.LoadNewScene(nextScene, previousScene);
#elif ENABLE_NETCODE
			NetCodeController.Instance.LoadNewScene(nextScene, previousScene);
#else
			if ((previousScene != null) && (previousScene.Length > 0)) SceneManager.UnloadSceneAsync(previousScene);
			SceneManager.LoadScene(nextScene, LoadSceneMode.Additive);
#endif
		}

		public void UnLoadScene(string previousScene)
		{
			if (previousScene.Length > 0) SceneManager.UnloadSceneAsync(previousScene);
		}

		public bool ExistNameRoom(string nameRoom)
        {
			foreach(RoomData room in _roomsLobby)
            {
				if (room.NameRoom.Equals(nameRoom)) return true;
            }
			return false;
        }

        public void DispatchNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, params object[] parameters)
        {
			if (!_hasEstablishedConnection) return;

#if ENABLE_PHOTON
            if (PhotonController.Instance != null) PhotonController.Instance.DispatchNetworkEvent(nameEvent, originNetworkID, targetNetworkID, parameters);
#elif ENABLE_MIRROR
            if (MirrorController.Instance != null) MirrorController.Instance.DispatchNetworkEvent(nameEvent, originNetworkID, targetNetworkID, parameters);
#elif ENABLE_NAKAMA
			if (NakamaController.Instance != null) NakamaController.Instance.DispatchNetworkEvent(nameEvent, originNetworkID, targetNetworkID, parameters);
#elif ENABLE_NETCODE
            if (NetCodeController.Instance != null) NetCodeController.Instance.DispatchNetworkEvent(nameEvent, originNetworkID, targetNetworkID, parameters);
#elif ENABLE_SOCKETS
			if (SocketsController.Instance != null) SocketsController.Instance.DispatchNetworkEvent(nameEvent, originNetworkID, targetNetworkID, parameters);
#endif
        }


		public void DelayNetworkEvent(string nameEvent, float time, int originNetworkID, int targetNetworkID, params object[] parameters)
		{
            if (_instance == null) return;

            _listEvents.Add(new TimedEventData(nameEvent, originNetworkID, targetNetworkID, time, parameters));
		}

		public void ClearNetworkEvents(params string[] events)
		{
			if ((events == null) || (events.Length == 0))
			{
				for (int k = 0; k < _listEvents.Count; k++)
				{
					_listEvents[k].Destroy();
				}
				_listEvents.Clear();
			}
			else
			{
				for (int i = 0; i < events.Length; i++)
				{
					string nameEvent = events[i];
					List<TimedEventData> eventToRemove = _listEvents.Where(x => x.NameEvent == nameEvent).ToList();
					for (int j = 0; j < eventToRemove.Count; j++)
					{
						eventToRemove[j].Destroy();
					}
					_listEvents.RemoveAll(x => x.NameEvent == nameEvent);
				}
			}
		}

#if UNITY_EDITOR
		private int _uniqueIDPrevious = -1;
		private bool _isServerPrevious = false;
		void OnGUI()
		{
			string connectionType = "NONE";
#if ENABLE_PHOTON
            connectionType = "PHOTON";
#elif ENABLE_MIRROR
            connectionType = "MIRROR";
#elif ENABLE_NAKAMA
			connectionType = "NAKAMA";
#elif ENABLE_NETCODE
            connectionType = "NETCODE";
#elif ENABLE_SOCKETS
            connectionType = "SOCKETS";
#endif

			GUILayout.BeginVertical();
			string debugDisplayConnectionState = "";
			if (UniqueNetworkID == -1)
			{
				debugDisplayConnectionState = "--NOT CONNECTED--";
			}
			else
			{
				debugDisplayConnectionState = "++["+connectionType+"]::UID["+UniqueNetworkID+"]::Server[" + IsServer + "]";
			}
			GUILayout.Box(new GUIContent(debugDisplayConnectionState));
			if (_uniqueIDPrevious != UniqueNetworkID)
			{
				SystemEventController.Instance.DispatchSystemEvent(EventNetworkControllerDebugSDKConnection, debugDisplayConnectionState);
			}
			_uniqueIDPrevious = UniqueNetworkID;
			GUILayout.EndVertical();
		}
#endif		

		private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			if (nameEvent.Equals(EventNetworkControllerConnectionWithRoom))
			{
				int uidConnection = (int)parameters[0];
				if (!_connections.Contains(uidConnection))
				{
					_connections.Add(uidConnection);
					NetworkController.Instance.DispatchEvent(EventNetworkControllerConfirmationConnectionWithRoom, uidConnection);
					yourvrexperience.Utils.Utilities.DebugLogColor("++LOCAL++ ADDED ID=" + uidConnection + " AND THE SIZE =" + _connections.Count, Color.red);
				}
			}
			if (nameEvent.Equals(EventNetworkControllerNewPlayerJoinedRoom))
			{
				int uidConnection = (int)parameters[0];
				if (!_connections.Contains(uidConnection))
				{
					_connections.Add(uidConnection);
					NetworkController.Instance.DispatchEvent(EventNetworkControllerConfirmationConnectionWithRoom, uidConnection);
					if (IsServer)
					{
						NetworkController.Instance.DispatchNetworkEvent(EventNetworkControllerReportCurrentState, -1, -1, IsMultipleScene);
					}					
					yourvrexperience.Utils.Utilities.DebugLogColor("++REMOTE++ ADDED ID=" + uidConnection + " AND THE SIZE =" + _connections.Count, Color.red);
				}
			}			
			if (nameEvent.Equals(EventNetworkControllerReportCurrentState))
			{
				if (!IsServer)
				{
					_isMultipleScene = (bool)parameters[0];
				}
			}
			if (nameEvent.Equals(EventNetworkControllerPlayerDisconnected))
			{
				int uidConnection = (int)parameters[0];
				if (_connections.Remove(uidConnection))
				{
					yourvrexperience.Utils.Utilities.DebugLogColor("REMOVED ID=" + uidConnection + " _connections AFTER THAT =" + _connections.Count, Color.red);
				}
			}
			if (nameEvent.Equals(EventNetworkControllerResetInitialIndexPosition))
			{
				_indexPositionInRoom = 0;
			}
			if (nameEvent.Equals(EventNetworkControllerRequestInitialIndexPosition))
			{
				if (IsServer)
				{
					int viewID = (int)parameters[0];
					NetworkController.Instance.DispatchNetworkEvent(EventNetworkControllerResponseInitialIndexPosition, -1, -1, viewID, _indexPositionInRoom);
					_indexPositionInRoom++;
				}
			}
			if (nameEvent.Equals(EventNetworkControllerListRoomsUpdated))
			{
				_roomsLobby = (List<RoomData>)parameters[0];
				if (!_hasEstablishedConnection)
				{
					_hasEstablishedConnection = true;
					DispatchEvent(EventNetworkControllerConnectionEstablishment);
				}
				DispatchEvent(EventNetworkControllerListRoomsConfirmedUpdated);
			}
			if (nameEvent.Equals(EventNetworkControllerRequestTotalConnectedPlayers))
			{
				if (IsServer)
				{
					if (parameters.Length > 0)
					{
						int viewID = (int)parameters[0];
						NetworkController.Instance.DispatchNetworkEvent(EventNetworkControllerReportTotalConnectedPlayers, -1, -1, _connections.Count, viewID);
					}
					else
					{
						NetworkController.Instance.DispatchNetworkEvent(EventNetworkControllerReportTotalConnectedPlayers, -1, -1, _connections.Count);
					}
				}
			}
			if (nameEvent.Equals(NetworkPrefab.EventNetworkPrefabHasStarted))
			{
				GameObject referenceGO = (GameObject)parameters[0];
				bool isInLevel = (bool)parameters[1];				
				if (IsServer)
				{
					string nameNetworkPrefab = (string)parameters[2];
					string pathNetworkPrefab = (string)parameters[3];		
					if (isInLevel)
					{
						for (int i = 0; i < networkPrefabs.Length; i++)
						{
							if (networkPrefabs[i].name == nameNetworkPrefab)
							{
								string networkUID = CreateNetworkPrefab(false, nameNetworkPrefab, networkPrefabs[i].gameObject, pathNetworkPrefab, new Vector3(0, 0, 0), Quaternion.identity, 0);
								INetworkInitialData[] dataProviders = referenceGO.GetComponents<INetworkInitialData>();
								if (!_networkObjectInitData.ContainsKey(networkUID))
								{
									Dictionary<string, string> dataSerialized = new Dictionary<string, string>();
									foreach (INetworkInitialData dataProvider in dataProviders)
									{
										dataSerialized.Add(dataProvider.ProviderName, dataProvider.GetInitialData());
									}
									_networkObjectInitData.Add(networkUID, dataSerialized);
								}
							}
						}
					}
					else
					{
						if (_networkObjectInitData.ContainsKey(referenceGO.name))
						{
							Dictionary<string, string> allSerializedData = null;
							INetworkInitialData[] dataProviders = referenceGO.GetComponents<INetworkInitialData>();
							if (_networkObjectInitData.TryGetValue(referenceGO.name, out allSerializedData))
							{
								foreach (INetworkInitialData dataProvider in dataProviders)
								{
									string dataProviderSerialized = "";
									if (allSerializedData.TryGetValue(dataProvider.ProviderName, out dataProviderSerialized))
									{
										dataProvider.ApplyInitialData(dataProviderSerialized, true);
									}
								}
							}
						}
					}
				}
				if (isInLevel)
				{
					GameObject.Destroy(referenceGO);
				}
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerReleaseAllResources))
            {
                Disconnect();
				Destroy();
            }		
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))
			{
				if (Instance)
				{
					DontDestroyOnLoad(Instance.gameObject);
				}
			}
        }

  		void Update()
        {
            if (_instance == null) return;

            // DELAYED EVENTS
            for (int i = 0; i < _listEvents.Count; i++)
            {
                TimedEventData eventData = _listEvents[i];
                if (eventData.Time == -1000)
                {
                    eventData.Destroy();
                    _listEvents.RemoveAt(i);
                    break;
                }
                else
                {
                    eventData.Time -= Time.deltaTime;
                    if (eventData.Time <= 0)
                    {
                        if (eventData != null)
                        {
                            DispatchNetworkEvent(eventData.NameEvent, eventData.Origin, eventData.Target, eventData.Parameters);
                            eventData.Destroy();
                        }
                        _listEvents.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using yourvrexperience.Utils;
using UnityEngine;

namespace yourvrexperience.Networking
{
	public class SocketsController : MonoBehaviour
	{
		public const bool DEBUG = false;

		public const int MESSAGE_EVENT = 0;
		public const int MESSAGE_TRANSFORM = 1;
		public const int MESSAGE_DATA = 2;

		public const string EventSocketsControllerCreateNetworkInstance = "EventSocketsControllerCreateNetworkInstance";

		// EVENTS WITH THE JAVA SERVER
		public const string EVENT_CLIENT_TCP_ESTABLISH_NETWORK_ID = "EVENT_CLIENT_TCP_ESTABLISH_NETWORK_ID";
		public const string EVENT_CLIENT_TCP_LIST_OF_GAME_ROOMS = "EVENT_CLIENT_TCP_LIST_OF_GAME_ROOMS";
		public const string EVENT_CLIENT_TCP_ROOM_ID = "EVENT_CLIENT_TCP_ROOM_ID";
        public const string EVENT_CLIENT_TCP_CLOSE_CURRENT_ROOM = "EVENT_CLIENT_TCP_CLOSE_CURRENT_ROOM";
        public const string EVENT_CLIENT_TCP_CONNECTED_ROOM = "EVENT_CLIENT_TCP_CONNECTED_ROOM";
		public const string EVENT_CLIENT_TCP_REPONSE_ALIVE = "EVENT_CLIENT_TCP_REPONSE_ALIVE";
		public const string EVENT_STREAMSERVER_REPORT_CLOSED_STREAM = "EVENT_STREAMSERVER_REPORT_CLOSED_STREAM";
		public const string EVENT_SYSTEM_PLAYER_HAS_BEEN_DESTROYED = "EVENT_SYSTEM_PLAYER_HAS_BEEN_DESTROYED";

		public const string EVENT_CLIENT_TCP_PLAYER_UID = "EVENT_CLIENT_TCP_PLAYER_UID";

		public const string TOKEN_SEPARATOR_EVENTS = "<tokevt>";
		public const string TOKEN_SEPARATOR_PARTY = "<tokprt>";
		public const string TOKEN_SEPARATOR_PLAYERS_IDS = "<tokply>";

		private static SocketsController _instance;

		public static SocketsController Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(SocketsController)) as SocketsController;
				}
				return _instance;
			}
		}

		[SerializeField] private string serverHost = "localhost";
		[SerializeField] private int serverPort = 8892;
		[SerializeField] private int bufferReceive = 65536;
		[SerializeField] private int timeoutReceive = 0;
		[SerializeField] private int bufferSend = 65536;
		[SerializeField] private int timeoutSend = 0;

		public GameObject[] NetworkPrefabs;

#if ENABLE_SOCKETS
		internal bool _socketConnected = false;

		private TcpClient _mySocket;
		private int _uniqueNetworkID = -1;
		private int _idNetworkServer = -1;

		private string _uidPlayer = "null";
		private string _serverIPAddress = "";

		private NetworkStream _theStream;
		private StreamWriter _theWriter;
		private StreamReader _theReader;
		private BinaryWriter _binWriter;
		private BinaryReader _binReader;
		private string _host;
		private Int32 _port;

		private float _timeoutForPing = 0;

		private string _nameRoom = "";
		private List<string> _events = new List<string>();
		private List<byte[]> _transforms = new List<byte[]>();
		private List<byte[]> _datas = new List<byte[]>();
		private List<RoomData> _roomsLobby = new List<RoomData>();

		private List<PlayerConnectionData> _playersConnections = new List<PlayerConnectionData>();

		private bool _requestToConsumWhenReady = false;
		private bool _hasBeenDestroyed = false;

		private List<SocketIdentity> _networkTransforms = new List<SocketIdentity>();

        public int UniqueNetworkID
        {
            get { return _uniqueNetworkID; }
        }
		public bool IsConnected
        {
			get { return _socketConnected; }
        }
		public bool IsServer
		{
			get { return _uniqueNetworkID == _idNetworkServer; }
		}
		public bool IsInLobby
		{
			get { return _socketConnected; }
		}
		public bool IsInRoom
		{
			get { return _uniqueNetworkID != -1; }
		}
		public List<RoomData> RoomsLobby
		{
			get { return _roomsLobby; }
		}
 		public string ServerAddress
        {
            get { return _serverIPAddress; }
            set { _serverIPAddress = value; }
        }

        public delegate void SocketTransformEvent(int netid, int uid, int indexPrefab, Vector3 position, Quaternion rotation, Vector3 scale);

        public event SocketTransformEvent SocketEvent;

        public void DispatchTransformEvent(int owner, int uid, int indexPrefab, Vector3 position, Quaternion rotation, Vector3 scale)
        {
			CheckExistingTransform(owner, uid, indexPrefab, position, rotation, scale);
            if (SocketEvent != null) SocketEvent(owner, uid, indexPrefab, position, rotation, scale);
        }

		public void Initialize()
		{
			_socketConnected = false;
		}

        public void Connect()
		{
			if (_socketConnected)
			{
				NetworkController.Instance.DispatchEvent(NetworkController.EventNetworkControllerListRoomsUpdated, _roomsLobby);
				return;
			}

			_uniqueNetworkID = -1;
			_idNetworkServer = -1;
			_nameRoom = "";					
			_hasBeenDestroyed = false;
			_events.Clear();
			_transforms.Clear();
			_datas.Clear();

			try
			{
				_host = serverHost;
				_port = serverPort;
                if (_serverIPAddress.Length > 0)
                {
                    _host = _serverIPAddress;
                    _mySocket = new TcpClient(_serverIPAddress, _port);
                }
                else
                {
                    _mySocket = new TcpClient(_host, _port);
                }				
                _mySocket.SendBufferSize = bufferSend;
                _mySocket.ReceiveBufferSize = bufferReceive;
                _mySocket.ReceiveTimeout = timeoutReceive;
                _mySocket.SendTimeout = timeoutSend;
                _theStream = _mySocket.GetStream();
				_theWriter = new StreamWriter(_theStream);
				_theReader = new StreamReader(_theStream);
				_binWriter = new BinaryWriter(_theStream);
				_binReader = new BinaryReader(_theStream);

				_socketConnected = true;

				SystemEventController.Instance.Event += OnSystemEvent;
				NetworkController.Instance.NetworkEvent += OnNetworkEvent;
			}
			catch (Exception e)
			{
				Debug.LogError("ClientTCPEventsController::Init::CONNECTION ERROR WITH SERVER[" + _host + ":" + _port + "]::Socket error: " + e);
			}
		}

		void OnDestroy()
		{
			Disconnect();
			Destroy();
		}

		public void Disconnect()
		{
			if (_hasBeenDestroyed) return;
			_hasBeenDestroyed = true;

			foreach (SocketIdentity socketGO in _networkTransforms)
			{
				GameObject.Destroy(socketGO.gameObject);
			}
			_networkTransforms.Clear();

			_events.Add(EVENT_STREAMSERVER_REPORT_CLOSED_STREAM);				

			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
        }

		public void Destroy()
		{
			if (Instance)
			{
				_instance = null;
				GameObject.Destroy(this.gameObject);
			}
		}

		private void DisconnectionConfirmation()
		{
			if (_instance != null)
			{
				_instance = null;
				CloseSocket(true);

				if (DEBUG) Debug.LogError("ClientTCPEventsController::Destroy::SOCKEDT CONNECTION HAS BEEN SUCCESSFULLY DESTROYED!!!!!!!!!!!!!!!!!!!!!!!!");
			}
		}

		private void CloseSocket(bool sendMessageDisconnect)
		{
			if (!_socketConnected)
			{
				return;
			}

			try { if (_theWriter != null) _theWriter.Close(); } catch (Exception errw) { }
			try { if (_theReader != null) _theReader.Close(); } catch (Exception errR) { }
			try { if (_mySocket != null) _mySocket.Close(); } catch (Exception errS) { }

			_socketConnected = false;
		}

		public int GetIndexPrefab(GameObject prefab)
		{
			int indexPrefab = -1;
			for (int i = 0; i < NetworkPrefabs.Length; i++)
			{
				if (NetworkPrefabs[i].name.Equals(prefab.name))
				{
					indexPrefab = i;
					break;
				}
			}
			return indexPrefab;
		}

		private void CheckExistingTransform(int owner, int uid, int indexPrefab, Vector3 position, Quaternion rotation, Vector3 scale)
		{
			bool exists = false;
			for (int i = 0; i < _networkTransforms.Count; i++)
			{
				if (_networkTransforms[i].NetID == uid)
				{
					exists = true;
				}
			}
			if (!exists)
			{
				if ((indexPrefab != -1) && (uid != -1) && (owner != -1))
				{
					CreateNetworkPrefabInServer(owner, uid, indexPrefab, position, rotation);
				}
			}
		}

        public void CreateNetworkPrefab(int ownerNetID, int indexPrefab, string uniqueUIDNameObject, Vector3 position, Quaternion rotation)
        {
			DispatchNetworkEvent(EventSocketsControllerCreateNetworkInstance, -1, -1, ownerNetID, indexPrefab, uniqueUIDNameObject, position, rotation);
        }

        private GameObject CreateNetworkPrefabInServer(int ownerNetID, int uniqueNetworkID, int indexPrefab, Vector3 position, Quaternion rotation, string nameObject = "")
        {
			if (indexPrefab < 0) return null;

			GameObject prefab = NetworkPrefabs[indexPrefab];
            GameObject newNetworkGO = Instantiate(prefab, position, rotation);
			if (nameObject.Length > 0)
			{
				newNetworkGO.GetComponent<NetworkObjectID>().NameObject = nameObject;
			}
			if (NetworkController.Instance.IsMultipleScene)
			{
				DontDestroyOnLoad(newNetworkGO);
			}
			yourvrexperience.Utils.Utilities.FixObject(newNetworkGO);
			newNetworkGO.GetComponent<SocketIdentity>().Set(ownerNetID, uniqueNetworkID, indexPrefab);
	      	return newNetworkGO;
        }

        public void DispatchNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, params object[] parameters)
        {
			if (_uniqueNetworkID != -1)
            {
				string types = "";
				string data = "";
				NetworkUtils.Serialize(parameters, ref data, ref types);
				_events.Add(Pack(nameEvent, originNetworkID, targetNetworkID, data, types));
			}
        }

		private bool WriteSocket(string _message)
		{
			if (!_socketConnected)
			{
				return false;
			}

			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			bw.Write((byte)MESSAGE_EVENT);
			byte[] bytesMessage = Encoding.UTF8.GetBytes(_message);
			bw.Write(bytesMessage.Length);
			bw.Write(bytesMessage);

			byte[] bytesEvent = ms.ToArray();
			_binWriter.Write(bytesEvent, 0, bytesEvent.Length);
			_binWriter.Flush();

			return true;
		}

		private bool WriteSocket(byte[] _message)
		{
			if (!_socketConnected)
			{
				return false;
			}

			// PROCESS TRANSFORMS
			try
			{
				_binWriter.Write(_message, 0, _message.Length);
				_binWriter.Flush();
			}
			catch (Exception err)
			{
			}

			return true;
		}

		private bool ReadSocket()
		{
			if (!_socketConnected)
			{
				return false;
			}
			if (_theStream.DataAvailable)
			{
				int firstByte = (int)_binReader.ReadByte();
				int sizeData = (int)_binReader.ReadInt32();
				switch (firstByte)
				{
					case MESSAGE_EVENT:
						byte[] eventData = _binReader.ReadBytes(sizeData);
						string message = System.Text.Encoding.UTF8.GetString(eventData);
						UnPackEventAndDispatch(message);
						break;

					case MESSAGE_TRANSFORM:
						ReadTransformAndDispatch();
						break;

					case MESSAGE_DATA:
						ReadDataAndDispatch(sizeData);
						break;
				}
			}
			if (_events.Count > 0)
			{
				for (int i = 0; i < _events.Count; i++)
				{
					WriteSocket(_events[i]);
				}
				_events.Clear();
			}
			if (_transforms.Count > 0)
			{
				for (int i = 0; i < _transforms.Count; i++)
				{
					WriteSocket(_transforms[i]);
				}
				_transforms.Clear();
			}
			for (int i = 0; i < _datas.Count; i++)
			{
				WriteSocket(_datas[i]);
			}
			_datas.Clear();
			if (_hasBeenDestroyed)
			{
				DisconnectionConfirmation();
			}
			return true;
		}

		private string Pack(string nameEvent, int networkOriginID, int networkTargetID, params object[] parameters)
		{
			string output = nameEvent + TOKEN_SEPARATOR_EVENTS;
			output += _uniqueNetworkID.ToString() + TOKEN_SEPARATOR_EVENTS;
			output += networkOriginID.ToString() + TOKEN_SEPARATOR_EVENTS;
			output += networkTargetID.ToString() + TOKEN_SEPARATOR_EVENTS;
			for (int i = 0; i < parameters.Length; i++)
			{
				if (i < parameters.Length - 1)
				{
					output += (string)parameters[i] + TOKEN_SEPARATOR_EVENTS;
				}
				else
				{
					output += parameters[i];
				}
			}
			return output;
		}

		public void SendTransform(int netID, int uID, int indexPrefab, Vector3 position, Quaternion rotation, Vector3 scale)
		{
			int counter = 0;
			int totalSizePacket = 4 + 4 + 4 + (8 * 10);
			byte[] message = new byte[1 + 4 + totalSizePacket];
			message[0] = (byte)MESSAGE_TRANSFORM;
			counter++;
			Array.Copy(BitConverter.GetBytes(totalSizePacket), 0, message, counter, 4);
			counter += 4;
			Array.Copy(BitConverter.GetBytes(netID), 0, message, counter, 4);
			counter += 4;
			Array.Copy(BitConverter.GetBytes(uID), 0, message, counter, 4);
			counter += 4;
			Array.Copy(BitConverter.GetBytes(indexPrefab), 0, message, counter, 4);
			counter += 4;
			// POSITION
			Array.Copy(BitConverter.GetBytes((double)position.x), 0, message, counter, 8);
			counter += 8;
			Array.Copy(BitConverter.GetBytes((double)position.y), 0, message, counter, 8);
			counter += 8;
			Array.Copy(BitConverter.GetBytes((double)position.z), 0, message, counter, 8);
			counter += 8;
			// ROTATION
			Array.Copy(BitConverter.GetBytes((double)rotation.x), 0, message, counter, 8);
			counter += 8;
			Array.Copy(BitConverter.GetBytes((double)rotation.y), 0, message, counter, 8);
			counter += 8;
			Array.Copy(BitConverter.GetBytes((double)rotation.z), 0, message, counter, 8);
			counter += 8;
			Array.Copy(BitConverter.GetBytes((double)rotation.w), 0, message, counter, 8);
			counter += 8;
			// SCALE
			Array.Copy(BitConverter.GetBytes((double)scale.x), 0, message, counter, 8);
			counter += 8;
			Array.Copy(BitConverter.GetBytes((double)scale.y), 0, message, counter, 8);
			counter += 8;
			Array.Copy(BitConverter.GetBytes((double)scale.z), 0, message, counter, 8);
			counter += 8;

			_transforms.Add(message);
		}

		private void ReadTransformAndDispatch()
		{
			int netID = _binReader.ReadInt32();
			int uID = _binReader.ReadInt32();
			int indexPrefab = _binReader.ReadInt32();

			// POSITION
			float posX = (float)_binReader.ReadDouble();
			float posY = (float)_binReader.ReadDouble();
			float posZ = (float)_binReader.ReadDouble();
			// ROTATION
			float rotX = (float)_binReader.ReadDouble();
			float rotY = (float)_binReader.ReadDouble();
			float rotZ = (float)_binReader.ReadDouble();
			float rotW = (float)_binReader.ReadDouble();
			// SCALE
			float scaleX = (float)_binReader.ReadDouble();
			float scaleY = (float)_binReader.ReadDouble();
			float scaleZ = (float)_binReader.ReadDouble();

			DispatchTransformEvent(netID, uID, indexPrefab, new Vector3(posX, posY, posZ), new Quaternion(rotX,rotY, rotZ, rotW), new Vector3(scaleX, scaleY, scaleZ));
		}

		public void SendBinaryData(int netID, byte[] data)
		{
			int counter = 0;
			int totalSizePacket = 4 + data.Length;
			byte[] message = new byte[1 + 4 + totalSizePacket];
			message[0] = (byte)MESSAGE_DATA;
			counter++;
			Array.Copy(BitConverter.GetBytes(totalSizePacket), 0, message, counter, 4);
			counter += 4;
			Array.Copy(BitConverter.GetBytes(netID), 0, message, counter, 4);
			counter += 4;
			Array.Copy(data, 0, message, counter, data.Length);

			_datas.Add(message);
		}

		private void ReadDataAndDispatch(int sizeData)
		{
			int netID = _binReader.ReadInt32();

			byte[] binaryData = _binReader.ReadBytes((sizeData - 4));

			// GET NAME EVENT
			int counter = 0;
			int sizeNameEvent = BitConverter.ToInt32(binaryData, counter);
			counter += 4;
			byte[] binaryNameEvent = new byte[sizeNameEvent];
			Array.Copy(binaryData, counter, binaryNameEvent, 0, sizeNameEvent);
			counter += sizeNameEvent;
			string nameEvent = Encoding.ASCII.GetString(binaryNameEvent);

			// GET DATA CONTENT
			int sizeContentEvent = BitConverter.ToInt32(binaryData, counter);
			counter += 4;
			byte[] binaryContentEvent = new byte[sizeContentEvent];
			Array.Copy(binaryData, counter, binaryContentEvent, 0, sizeContentEvent);
		}

		public static string GetPlayersString(int _playerNumber)
		{
			string players = "";
			for (int i = 0; i < _playerNumber; i++)
			{
				if (players.Length > 0)
				{
					players += TOKEN_SEPARATOR_PLAYERS_IDS;
				}
				players += "PLAYER_LOBBY_" + i;
			}

			return players;
		}

		public void CreateRoom(string nameRoom, int playerNumber)
		{
			_nameRoom = nameRoom;
			_events.Add(Pack(EVENT_CLIENT_TCP_ROOM_ID, -1, -1, _nameRoom.ToString(), playerNumber.ToString()));
		}

		public void JoinRoom(string nameRoom)
		{
			_nameRoom = nameRoom;
			_events.Add(Pack(EVENT_CLIENT_TCP_ROOM_ID, -1, -1, _nameRoom.ToString(), "-1"));
		}

		private bool RemoveConnection(int idConnection)
		{
			for (int i = 0; i < _playersConnections.Count; i++)
			{
				if (_playersConnections[i].Id == idConnection)
				{
					_playersConnections[i].Destroy();
					_playersConnections.RemoveAt(i);
					return true;
				}
			}
			return false;
		}

		public PlayerConnectionData GetConnection(int idConnection)
		{
			for (int i = 0; i < _playersConnections.Count; i++)
			{
				if (_playersConnections[i].Id == idConnection)
				{
					return _playersConnections[i];
				}
			}
			return null;
		}

		public bool ClientNewConnection(int idConnection)
		{
			PlayerConnectionData newPlayerConnection = new PlayerConnectionData(idConnection, null);
			if (!_playersConnections.Contains(newPlayerConnection))
			{
				_playersConnections.Add(newPlayerConnection);
				// DISPATCH NEW CLIENT EVENT
				return true;
			}
			else
			{
				return false;
			}
		}

		public void ClientDisconnected(int idConnection)
		{
			if (RemoveConnection(idConnection))
			{
				// NetworkEventController.Instance.DispatchLocalEvent(NetworkEventController.EVENT_PLAYERCONNECTIONDATA_USER_DISCONNECTED, _idConnection);
			}
		}

		public void UnPackEventAndDispatch(string package)
		{
			if (DEBUG) Debug.LogError("****UnPackEventAndDispatch::_package=" + package);

			// PROCESS ALL THE OTHER EVENTS
			string[] parameters = package.Split(TOKEN_SEPARATOR_EVENTS, StringSplitOptions.None);
			string nameEvent = parameters[0];
			int uniqueNetworkID = -1;
			int originNetworkID = -1;
			int targetNetworkID = -1;
			if (parameters.Length > 3)
			{
				uniqueNetworkID = int.Parse(parameters[1]);
				originNetworkID = int.Parse(parameters[2]);
				targetNetworkID = int.Parse(parameters[3]);
			}

			// PROCESS PACKAGE
			if (_uniqueNetworkID != -1)
			{
				if (nameEvent.Equals(EVENT_STREAMSERVER_REPORT_CLOSED_STREAM))
				{
					Disconnect();
				}
				else
				if (nameEvent.Equals(EVENT_SYSTEM_PLAYER_HAS_BEEN_DESTROYED))
				{
					int uidDisconnectedClient = int.Parse(parameters[4]);
					if (DEBUG) Debug.LogError("EVENT_SYSTEM_PLAYER_HAS_BEEN_DESTROYED::HAS BEEN DISCONNECTED uidDisconnectedClient[" + uidDisconnectedClient + "]++++++++++");
					NetworkController.Instance.DispatchEvent(NetworkController.EventNetworkControllerPlayerDisconnected, uidDisconnectedClient);
				}
				else
				{
					List<object> extraParameters = new List<object>();
					if (parameters.Length == 6)
					{
						NetworkUtils.Deserialize(extraParameters, parameters[4], parameters[5]);
					}
					if (DEBUG) Debug.LogError("NETWORK EVENT RECEIVED::nameEvent[" + nameEvent + "]++++++++++");
					NetworkController.Instance.DispatchEvent(nameEvent, originNetworkID, targetNetworkID, extraParameters.ToArray());
				}
			}
			else
			{
				// RETRIEVE THE UNIQUE NETWORK IDENTIFICATOR
				if (nameEvent.Equals(EVENT_CLIENT_TCP_ESTABLISH_NETWORK_ID))
				{
					_uidPlayer = parameters[4];
					if (DEBUG) Debug.LogError("EVENT_CLIENT_TCP_ESTABLISH_NETWORK_ID::HAS BEEN ASSIGNED _uidPlayer[" + _uidPlayer + "]++++++++++");
					_events.Add(Pack(EVENT_CLIENT_TCP_PLAYER_UID, -1, -1, _uidPlayer));
				}
				else
				{
					if (nameEvent.Equals(EVENT_CLIENT_TCP_LIST_OF_GAME_ROOMS))
					{
						if (DEBUG) Debug.LogError("EVENT_CLIENT_TCP_LIST_OF_GAME_ROOMS::LIST OF ROOMS received!!!!!!!!!!!!!!!!!");
						string[] roomsInvited = parameters[4].Split(TOKEN_SEPARATOR_PARTY, StringSplitOptions.None);
						_roomsLobby.Clear();
						for (int i = 0; i < roomsInvited.Length; i++)
						{
							string[] dataParty = roomsInvited[i].Split(TOKEN_SEPARATOR_PLAYERS_IDS, StringSplitOptions.None);
							if (dataParty.Length > 1)
							{
								RoomData roomData = new RoomData();
								roomData.NameRoom = dataParty[0];
								roomData.TotalPlayers = int.Parse(dataParty[1]);
								_roomsLobby.Add(roomData);
							}
						}
						if (DEBUG) Debug.LogError("EVENT_CLIENT_TCP_LIST_OF_GAME_ROOMS::LIST OF ROOMS AVAILABLE[" + _roomsLobby.Count + "]");
						NetworkController.Instance.DispatchEvent(NetworkController.EventNetworkControllerListRoomsUpdated, _roomsLobby);
					}
					else
					{
						if (nameEvent.Equals(EVENT_CLIENT_TCP_CONNECTED_ROOM))
						{
							_uniqueNetworkID = int.Parse(parameters[4]);
							_idNetworkServer = int.Parse(parameters[5]);
							int totalNumberPlayers = int.Parse(parameters[6]);
							if (DEBUG) Debug.LogError("EVENT_CLIENT_TCP_CONNECTED_ROOM::ASSIGNED LOCAL CLIENT NUMBER[" + _uniqueNetworkID + "] IN THE ROOM[" + _nameRoom + "] WHERE THE SERVER IS[" + _idNetworkServer + "]++++++++++");
							NetworkController.Instance.DispatchEvent(NetworkController.EventNetworkControllerConnectionWithRoom, _uniqueNetworkID);
							NetworkController.Instance.DispatchNetworkEvent(NetworkController.EventNetworkControllerNewPlayerJoinedRoom, -1, -1, _uniqueNetworkID);
						}
					}
				}
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(SocketTransform.EventSocketTransformNew))
			{
				bool found = false;
				SocketIdentity newIdentity = (SocketIdentity)parameters[0];
				for (int i = 0; i < _networkTransforms.Count; i++)
				{
					if (_networkTransforms[i].NetID == newIdentity.NetID)
					{
						found = true;
					}
				}
				if (!found)
				{
					_networkTransforms.Add(newIdentity);
				}				
			}
			if (nameEvent.Equals(NetworkObjectID.EventNetworkObjectIDReportDestroyed))
			{
				GameObject networkObjectDestroyed = (GameObject)parameters[0];
				SocketIdentity identityToDestroy = networkObjectDestroyed.GetComponent<SocketIdentity>();
				if (identityToDestroy != null)
				{
					for (int i = 0; i < _networkTransforms.Count; i++)
					{
						if (_networkTransforms[i].NetID == identityToDestroy.NetID)
						{
							_networkTransforms.RemoveAt(i);
						}
					}
				}
			}
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))
			{
				if (Instance)
				{
					DontDestroyOnLoad(Instance.gameObject);
				}
			}
		}


		private void OnNetworkEvent(string nameEvent, int networkOriginID, int networkTargetID, params object[] parameters)
		{
			if (nameEvent.Equals(NetworkController.EventNetworkControllerDestroyCommunications))
			{
				Disconnect();
			}
			if (nameEvent.Equals(EventSocketsControllerCreateNetworkInstance))
			{
				int owner = (int)parameters[0];
				int indexPrefab = (int)parameters[1];
				string nameObject = (string)parameters[2];
				Vector3 position = (Vector3)parameters[3];
				Quaternion rotation = (Quaternion)parameters[4];
				if (IsServer)
				{
					CreateNetworkPrefabInServer(owner, NetworkController.Instance.NetworkInstanceCounter++, indexPrefab, position, rotation, nameObject);
				}
			}
			if (nameEvent.Equals(NetworkController.EventNetworkControllerNewPlayerJoinedRoom))
			{
				int networkIDPlayer = (int)parameters[0];
				if (NetworkController.Instance.IsServer)
				{
					ClientNewConnection(networkIDPlayer);
				}
			}
			if (nameEvent.Equals(NetworkController.EventNetworkControllerPlayerDisconnected))
			{
				int networkIDPlayer = (int)parameters[0];
				if (NetworkController.Instance.IsServer)
				{
					RemoveConnection(networkIDPlayer);
				}
			}
			if (nameEvent.Equals(NetworkController.EventNetworkControllerPlayerDisconnected))
			{
				int networkIDPlayer = (int)parameters[0];
				ClientDisconnected(networkIDPlayer);
			}
		}

		public void Update()
		{
			ReadSocket();

			// PING
			_timeoutForPing += Time.deltaTime;
			if (_timeoutForPing > 2)
			{
				_timeoutForPing = 0;
				_events.Add(EVENT_CLIENT_TCP_REPONSE_ALIVE);				
			}
		}
#endif		
	}
}
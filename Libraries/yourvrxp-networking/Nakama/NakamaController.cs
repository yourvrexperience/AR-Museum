#if ENABLE_NAKAMA
using Nakama;
using Nakama.TinyJson;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.Networking
{
	public class OpCodes
	{
		public const long AssignUID = 1;
		public const long Message = 2;
		public const long Transform = 3;
	}

	public class NakamaController : MonoBehaviour
	{
		public const bool DebugMessages = false;

		public const string EventNakamaControllerGameStarted = "EventNakamaControllerGameStarted";
		public const string EventNakamaControllerTimeOutSendUIDs = "EventNakamaControllerTimeOutSendUIDs";
		public const string EventNakamaControllerSendInitialRooms = "EventNakamaControllerSendInitialRooms";
		public const string EventNakamaControllerCreateNetworkInstance = "EventNakamaControllerCreateNetworkInstance";

		public const string RoomsChatMessage = "rooms";
		public const string RemoveRoomsMessage = "remroom";
		public const string LeaveChatMessage = "leavechat";
		public const char RoomsSeparator = ';';
		public const char ParamSeparator = ',';
		public const float TimeUpdateTransforms = 0.2f;

		public static bool AllowInstance = true;
		private static NakamaController _instance;

		public static NakamaController Instance
		{
			get
			{
				if (AllowInstance)
				{
					if (_instance == null)
					{
						_instance = GameObject.FindObjectOfType(typeof(NakamaController)) as NakamaController;
					}
				}
				return _instance;
			}
		}

		public NakamaConnection NakamaConnection;

		public GameObject[] NetworkPrefabs;
#if ENABLE_NAKAMA

		private List<NakamaPlayer>  _players = new List<NakamaPlayer>();
		private IUserPresence _localUser;
		private IMatch _currentMatch;
		private bool _isInLobby = false;
		private bool _hasBeenInitialized = false;

		private string _roomName = "";
		private int _uid = -1;
		private bool _isGameCreator = false;
		private bool _isInRoom = false;
		private int _totalPlayers = -1;

		private string _roomsBuffer = "";
		private List<RoomData> _roomsLobby = new List<RoomData>();

		private List<ItemMultiObjectEntry> _events = new List<ItemMultiObjectEntry>();

		private UnityMainThreadDispatcher _mainThread;

		private List<NakamaIdentity> _networkTransforms = new List<NakamaIdentity>();

		public bool IsConnected
        {
			get { return _isInLobby; }
        }
		public int UniqueNetworkID
        {
			get { return _uid; }
        }
		public bool IsServer
		{
			get { return _isGameCreator; }
		}
		public bool IsInLobby
		{
			get { return _isInLobby; }
		}
		public bool IsInRoom
		{
			get { return _isInRoom; }
		}
		public List<RoomData> RoomsLobby
		{
			get { return _roomsLobby; }
		}

 		public string ServerAddress
        {
            get { return NakamaConnection.Host; }
            set { 
				if ((value == null) || (value.Length == 0))
				{
					NakamaConnection.Host = "localhost"; 
				}
				else
				{
					NakamaConnection.Host = value; 
				}
			}
        }

		public string RoomsBuffer
        {
			get { return _roomsBuffer; }
			set
            {
				_roomsBuffer = value;
				_roomsLobby = new List<RoomData>();
				if (RoomsBuffer.IndexOf(RoomsSeparator) != -1)
                {
					string[] currentRooms = RoomsBuffer.Split(RoomsSeparator);
					if (currentRooms.Length > 0)
                    {
						for (int i = 0; i < currentRooms.Length; i++)
						{
							string[] entryRoom = currentRooms[i].Split(ParamSeparator);
							if (entryRoom.Length == 3)
                            {
								string nameRoom = entryRoom[0];
								string totalPlayers = entryRoom[1];
								string extraData = entryRoom[2];
								extraData = ((extraData.Length == 0)?"extraData": extraData);
								RoomData item = new RoomData() { NameRoom = nameRoom, ExtraData = extraData, TotalPlayers =  int.Parse(totalPlayers) };
								_roomsLobby.Add(item);
							}
						}
						if (DebugMessages)
                        {
							Debug.LogError("+++++++++++++TOTAL ROOMS[" + _roomsLobby.Count + "]");
						}
					}
				}
			}
        }


        public delegate void NakamaTransformEvent(int netid, int uid, int indexPrefab, Vector3 position, Quaternion rotation, Vector3 scale);

        public event NakamaTransformEvent NakamaEvent;

        public void DispatchTransformEvent(int owner, int uid, int indexPrefab, Vector3 position, Quaternion rotation, Vector3 scale)
        {
			CheckExistingTransform(owner, uid, indexPrefab, position, rotation, scale);
            if (NakamaEvent != null) NakamaEvent(owner, uid, indexPrefab, position, rotation, scale);
        }

		public void Initialize()
		{
		}

		public async void Connect()
		{
			if (_hasBeenInitialized) return;
			_hasBeenInitialized = true;

			if (DebugMessages) Debug.LogError("NakamaController::Initialitzation");

			_mainThread = UnityMainThreadDispatcher.Instance();

			NetworkController.Instance.NetworkEvent += OnNetworkEvent;
			SystemEventController.Instance.Event += OnSystemEvent;

			// Connect to the Nakama server.
			await NakamaConnection.Connect();

			NakamaConnection.Socket.ReceivedMatchmakerMatched += ActionReceivedMatchmakerMatched;
			NakamaConnection.Socket.ReceivedMatchPresence += ActionReceivedMatchPresence;
			NakamaConnection.Socket.ReceivedMatchState += ActionReceivedMatchState;

			NakamaConnection.Socket.ReceivedChannelPresence += ActionReceivedChannelPresence;
			NakamaConnection.Socket.ReceivedChannelMessage += ActionReceivedChannelMessage;

			await NakamaConnection.JoinMainChat();
		}

		private void ActionReceivedMatchmakerMatched(IMatchmakerMatched m) { _mainThread.Enqueue(() => OnReceivedMatchmakerMatched(m)); }
		private void ActionReceivedMatchPresence(IMatchPresenceEvent m) { _mainThread.Enqueue(() => OnReceivedMatchPresence(m)); }
		private void ActionReceivedMatchState(IMatchState m) { _mainThread.Enqueue(() => OnReceivedMatchState(m)); }
		private void ActionReceivedChannelPresence(IChannelPresenceEvent m) { _mainThread.Enqueue(() => OnReceivedChannelPresence(m)); }
		private void ActionReceivedChannelMessage(IApiChannelMessage m) { _mainThread.Enqueue(() => OnReceivedChannelMessage(m)); }

		public async void Disconnect()
        {
			if (_hasBeenInitialized)
			{
				_hasBeenInitialized = false;
				AllowInstance = false;

				_players.Clear();
				_localUser = null;
				_currentMatch = null;
				_isInLobby = false;

				_roomName = "";
				_uid = -1;
				_isGameCreator = false;
				_isInRoom = false;
				_totalPlayers = -1;

				RoomsBuffer = "";
				_events.Clear();

				NakamaConnection.Socket.ReceivedMatchmakerMatched -= ActionReceivedMatchmakerMatched;
				NakamaConnection.Socket.ReceivedMatchPresence -= ActionReceivedMatchPresence;
				NakamaConnection.Socket.ReceivedMatchState -= ActionReceivedMatchState;

				NakamaConnection.Socket.ReceivedChannelPresence -= ActionReceivedChannelPresence;
				NakamaConnection.Socket.ReceivedChannelMessage -= ActionReceivedChannelMessage;

				if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
				if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;

				_mainThread.Clear();

				foreach (NakamaIdentity nakamaGO in _networkTransforms)
				{
					GameObject.Destroy(nakamaGO.gameObject);
				}
				_networkTransforms.Clear();

				QuitMatch();
				if (DebugMessages) Debug.LogError("//////////////////////////////////////// DESTROYED SUCCESSFULLY NAKAMA CONTROLLER");
			}
		}

		public void Destroy()
		{
			if (Instance)
			{
				_instance = null;
				GameObject.Destroy(this.gameObject);
			}
		}

		void OnDestroy()
		{
			Disconnect();
			Destroy();
		}

		private async void OnReceivedChannelPresence(IChannelPresenceEvent m)
		{
			if (m.Leaves.ToArray().Length == 0)
			{
				_isInLobby = true;
				if (DebugMessages) Debug.LogError("RECEIVED CHANNEL PRESENCE::ChannelId=" + m.ChannelId + "::SENDING ROOMS=" + RoomsBuffer);
				if (NakamaConnection.ConnectedToMainChat)
                {
					await NakamaConnection.SendMainChatMessage(RoomsChatMessage, RoomsBuffer);
				}
				else
                {
					SystemEventController.Instance.DelaySystemEvent(EventNakamaControllerSendInitialRooms, 1);
                }
			}
		}

		private async void OnReceivedChannelMessage(IApiChannelMessage m)
		{
			Dictionary<string, object> message = (Dictionary<string, object>)Json.Deserialize(m.Content);
			foreach (KeyValuePair<string, object> item in message)
			{
				if (item.Key == RoomsChatMessage)
				{
					string buf = (string)item.Value;
					if (buf.Length > 0)
                    {
						RoomsBuffer = (string)item.Value;
					}
					NetworkController.Instance.DispatchEvent(NetworkController.EventNetworkControllerListRoomsUpdated, _roomsLobby);
					if (DebugMessages) Debug.LogError("NakamaController::ROOMS UPDATED=" + RoomsBuffer);
				}
				else if (item.Key == RemoveRoomsMessage)
				{
					string roomToDelete = (string)item.Value;
					string[] currentRooms = RoomsBuffer.Split(RoomsSeparator);
					string finalRooms = "";
					bool hasBeenDeleted = false;
					for (int i = 0; i < currentRooms.Length; i++)
					{
						if (currentRooms[i].IndexOf(roomToDelete) != -1)
						{
							if (DebugMessages) Debug.LogError("NakamaController::DELETED ROOM=" + roomToDelete);
							hasBeenDeleted = true;
						}
						else
						{
							finalRooms = currentRooms[i] + ((finalRooms.Length > 0) ? RoomsSeparator +"" : "") + finalRooms;
						}                    
					}
					if (hasBeenDeleted)
					{
						RoomsBuffer = finalRooms;
						if (DebugMessages) Debug.LogError("NakamaController::ROOMS AFTER DELETE=" + finalRooms);
						if (_localUser != null)
						{
							if (NakamaConnection.Channel != null)
							{
								await NakamaConnection.LeaveMainChat();
							}
						}
					}
				}
			}
		}

		private async void OnReceivedMatchmakerMatched(IMatchmakerMatched matched)
		{
			_localUser = matched.Self.Presence;

			// Join the match.
			var match = await NakamaConnection.Socket.JoinMatchAsync(matched);

			// Spawn a player instance for each connected user.
			foreach (var user in match.Presences)
			{
				RegisterPlayer(match.Id, user);
			}

			RegisterPlayer(match.Id, match.Self);

			_currentMatch = match;

			await NakamaConnection.SendMainChatMessage(RemoveRoomsMessage, _roomName);
		}

		private void OnReceivedMatchPresence(IMatchPresenceEvent matchPresenceEvent)
		{
			if (DebugMessages) Debug.LogError("NakamaController::IMatchPresenceEvent::JOINS["+ matchPresenceEvent.Joins.ToList().Count + "]");

			// For each new user that joins, spawn a player for them.
			foreach (var user in matchPresenceEvent.Joins)
			{
				RegisterPlayer(matchPresenceEvent.MatchId, user);
			}

			// For each player that leaves, despawn their player.
			foreach (var user in matchPresenceEvent.Leaves)
			{
				for (int i = 0; i <  _players.Count; i++)
				{
					NakamaPlayer player =  _players[i];
					if (player.UserPresence.SessionId == user.SessionId)
                    {
						_players.RemoveAt(i);
						i--;
					}
				}
			}
		}

		private NakamaPlayer GetNakamaPlayer(string userID)
		{
			for (int i = 0; i <  _players.Count; i++)
			{
				NakamaPlayer player =  _players[i];
				if (player.UserPresence.UserId == userID)
				{
					return player;
				}
			}
			return null;
		}

		private RoomData GetRoomByName(string roomName)
        {
			for (int i = 0; i < _roomsLobby.Count; i++)
            {
				if (_roomsLobby[i].NameRoom.Equals(roomName))
                {
					return _roomsLobby[i];
				}
            }
			return new RoomData();
		}

		public int GetRoomIDByName(string roomName)
		{
			for (int i = 0; i < _roomsLobby.Count; i++)
			{
				if (_roomsLobby[i].NameRoom.Equals(roomName))
				{
					return i;
				}
			}
			return -1;
		}

		public string GetExtraDataForRoom(int idRoom)
        {
			return _roomsLobby[idRoom].ExtraData;
		}

		private void OnReceivedMatchState(IMatchState matchState)
		{
			OnProcessReceivedMatchState(matchState.UserPresence.SessionId, matchState.OpCode, System.Text.Encoding.UTF8.GetString(matchState.State));
		}

		private void OnProcessReceivedMatchState(string userSessionId, long opCode, string matchStateData)
		{
			Dictionary<string, string> state = matchStateData.Length > 0 ? matchStateData.FromJson<Dictionary<string, string>>() : null;

			switch (opCode)
			{
				case OpCodes.AssignUID:
					SystemEventController.Instance.ClearSystemEvents(EventNakamaControllerTimeOutSendUIDs);
					foreach (KeyValuePair<string, string> playerUID in state)
                    {
						NakamaPlayer nakamaPlayer = GetNakamaPlayer(playerUID.Key);
						if (nakamaPlayer != null)
                        {
							nakamaPlayer.UID = int.Parse(playerUID.Value);
							if (nakamaPlayer.Equals(_localUser))
                            {
								_uid = nakamaPlayer.UID;
								if (DebugMessages) Debug.LogError("+++++++++++++++++++ASSIGNED UID["+ _uid + "]");
							}
						}
					}
					SystemEventController.Instance.DelaySystemEvent(NakamaController.EventNakamaControllerGameStarted, 0.1f, _isGameCreator, _uid);
					break;

				case OpCodes.Message:
					string eventName = "";
					state.TryGetValue(MatchDataJson.EventNameKey, out eventName);
					string origin = "";
					state.TryGetValue(MatchDataJson.OriginKey, out origin);
					string target = "";
					state.TryGetValue(MatchDataJson.TargetKey, out target);
					string data = "";
					state.TryGetValue(MatchDataJson.DataKey, out data);

					if (eventName.Length > 0)
                    {
						string[] paramData = data.Split(new string[] { NetworkController.TokenSeparatorEvents }, StringSplitOptions.None);
						List<object> parameters = new List<object>();
						if (paramData.Length == 2)
						{
							NetworkUtils.Deserialize(parameters, paramData[0], paramData[1]);
						}						
						NetworkController.Instance.DispatchEvent(eventName, int.Parse(origin), int.Parse(target), parameters.ToArray());
					}
					break;

				case OpCodes.Transform:
					string owner = "";
					state.TryGetValue(MatchDataJson.OwnerKey, out owner);
					string uid = "";
					state.TryGetValue(MatchDataJson.UidKey, out uid);
					string indexPrefab = "";
					state.TryGetValue(MatchDataJson.IndexKey, out indexPrefab);
					string position = "";
					state.TryGetValue(MatchDataJson.PositionKey, out position);
					string rotation = "";
					state.TryGetValue(MatchDataJson.RotationKey, out rotation);
					string scale = "";
					state.TryGetValue(MatchDataJson.ScaleKey, out scale);

					DispatchTransformEvent(int.Parse(owner), int.Parse(uid), int.Parse(indexPrefab), yourvrexperience.Utils.Utilities.DeserializeVector3(position), yourvrexperience.Utils.Utilities.DeserializeQuaternion(rotation), yourvrexperience.Utils.Utilities.DeserializeVector3(scale));
					break;

				default:
					break;
			}
		}

		private async void RegisterPlayer(string matchId, IUserPresence user)
		{
			if (DebugMessages) Debug.LogError("+++++++++++++++++++++++++++++++++++++++++++++REGISTERPLAYER::_user.UserId=" + user.UserId);

			NakamaPlayer newPlayer = new NakamaPlayer(user.UserId, matchId, user);

			bool found = false;
			foreach (NakamaPlayer player in  _players)
            {
				if (player.Equals(newPlayer))
				{
					found = true;
				}
			}

			if (!found)
            {
				 _players.Add(newPlayer);
			}

			if (_isGameCreator)
            {
				if ( _players.Count == _totalPlayers)
                {
					if (DebugMessages) Debug.LogError("RegisterPlayer::SENDING UIDS");
					await SendUIDsPlayers();
				}
			}
		}

		private async Task SendUIDsPlayers()
        {
			List<string> uids = new List<string>();
			foreach (NakamaPlayer player in  _players)
			{
				uids.Add(player.ID);
			}
			SystemEventController.Instance.DelaySystemEvent(EventNakamaControllerTimeOutSendUIDs, 2);
			await SendMatchStateAsync(OpCodes.AssignUID, MatchDataJson.AssignUIDS(uids.ToArray()), true);
		}

		public async Task QuitMatch()
		{
			if (_currentMatch != null)
            {
				await NakamaConnection.Disconnect(_currentMatch);

				_currentMatch = null;
				_localUser = null;

				 _players.Clear();
			}
		}

		public async Task SendMatchStateAsync(long opCode, string state, bool sendLocal)
		{
			if (_currentMatch != null)
            {
				await NakamaConnection.Socket.SendMatchStateAsync(_currentMatch.Id, opCode, state);
				if (sendLocal)
                {
					OnProcessReceivedMatchState(_currentMatch.Self.SessionId, opCode, state);
				}				
			}			
		}

		public void SendMatchState(long opCode, string state, bool sendLocal)
		{
			if (_currentMatch != null)
            {
				NakamaConnection.Socket.SendMatchStateAsync(_currentMatch.Id, opCode, state);
				if (sendLocal)
                {
					OnProcessReceivedMatchState(_currentMatch.Self.SessionId, opCode, state);
				}					
			}				
		}

		public async Task FindMatch(string roomName, int totalPlayers, string extraData = "")
		{
			_roomName = roomName;
			string newRooms = RoomsBuffer;
			if (RoomsBuffer.IndexOf(_roomName) == -1)
			{
				_isGameCreator = true;
				_totalPlayers = totalPlayers;
				newRooms = _roomName + ParamSeparator + totalPlayers + ParamSeparator + extraData;
				newRooms += RoomsSeparator + RoomsBuffer;
				if (DebugMessages) Debug.LogError("NakamaController::FindMatch::--NEW CREATE GAME--::_roomName[" + _roomName + "]::_totalPlayers["+ _totalPlayers + "]");
			}
            else
            {
				RoomData roomFound = GetRoomByName(roomName);
				_totalPlayers = roomFound.TotalPlayers;
				if (DebugMessages) Debug.LogError("NakamaController::FindMatch::==GAME FOUND==::_roomName[" + _roomName + "]::_totalPlayers["+ _totalPlayers + "]");
			}
			await NakamaConnection.FindMatch(_roomName, _totalPlayers, _totalPlayers);

			// Update the lobby list of rooms
			await NakamaConnection.SendMainChatMessage(RoomsChatMessage, newRooms);
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
					CreateNetworkPrefabInServer(owner, uid, indexPrefab, position, Quaternion.Euler(scale.x, scale.y, scale.z));
				}				
			}
		}

		private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			if (nameEvent.Equals(EventNakamaControllerCreateNetworkInstance))
			{
				int owner = (int)parameters[0];
				int indexPrefab = (int)parameters[1];
				string nameObject = (string)parameters[2];
				Vector3 position = (Vector3)parameters[3];
				Quaternion rotation = (Quaternion)parameters[4];
				if (_isGameCreator)
				{
					CreateNetworkPrefabInServer(owner, NetworkController.Instance.NetworkInstanceCounter++, indexPrefab, position, rotation, nameObject);
				}
			}
			if (nameEvent.Equals(NetworkController.EventNetworkControllerDestroyCommunications))
			{
				Disconnect();
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(NakamaTransform.EventNakamaTransformNew))
			{
				bool found = false;
				NakamaIdentity newIdentity = (NakamaIdentity)parameters[0];
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
				NakamaIdentity identityToDestroy = networkObjectDestroyed.GetComponent<NakamaIdentity>();
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
			if (nameEvent.Equals(EventNakamaControllerSendInitialRooms))
            {
				if (NakamaConnection.ConnectedToMainChat)
				{
					NakamaConnection.SendMainChatMessage(RoomsChatMessage, RoomsBuffer);
				}
				else
				{
					SystemEventController.Instance.DelaySystemEvent(EventNakamaControllerSendInitialRooms, 1);
				}
			}
			if (nameEvent.Equals(EventNakamaControllerGameStarted))
			{
				bool isServer = _isGameCreator;
				if (parameters.Length > 0)
				{
					isServer = (bool)parameters[0];
				}
				int clientNetID = -1;
				if (parameters.Length > 1)
				{
					clientNetID = (int)parameters[1];
				}
				_isInRoom = true;
				NetworkController.Instance.DispatchEvent(NetworkController.EventNetworkControllerConnectionWithRoom, clientNetID);
			}
			if (nameEvent.Equals(EventNakamaControllerTimeOutSendUIDs))
            {
				if (DebugMessages) Debug.LogError("++++++++++++++++++++++++++++++++TIMEOUT FOR UIDS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
				SendUIDsPlayers();
			}
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))
			{
				if (Instance)
				{
					DontDestroyOnLoad(Instance.gameObject);
				}
			}
		}

        public ItemMultiObjectEntry DispatchNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, params object[] parameters)
        {
			ItemMultiObjectEntry output = null;
			if (_uid != -1)
            {
				string types = "";
				string data = "";
				NetworkUtils.Serialize(parameters, ref data, ref types);
				output = new ItemMultiObjectEntry(OpCodes.Message, MatchDataJson.Message(nameEvent, originNetworkID, targetNetworkID, data, types));
				_events.Add(output);
			}
			return output;
        }

		public ItemMultiObjectEntry SendTransform(int netID, int uID, int indexPrefab, Vector3 position, Quaternion rotation, Vector3 scale)
        {
			ItemMultiObjectEntry output = null;
			if (_uid != -1)
            {
				output = new ItemMultiObjectEntry(OpCodes.Transform, MatchDataJson.Transform(netID, uID, indexPrefab, position, rotation, scale));
				_events.Add(output);
			}			
			return output;
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

        public void CreateNetworkPrefab(int ownerNetID, int indexPrefab, string uniqueUIDNameObject, Vector3 position, Quaternion rotation)
        {
			DispatchNetworkEvent(EventNakamaControllerCreateNetworkInstance, -1, -1, ownerNetID, indexPrefab, uniqueUIDNameObject, position, rotation);
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
			newNetworkGO.GetComponent<NakamaIdentity>().Set(ownerNetID, uniqueNetworkID, indexPrefab);
	      	return newNetworkGO;
        }

		private async void SendNakamaMessage(long code, string data)
		{
			if (code == OpCodes.Transform)
			{
				await SendMatchStateAsync(code, data, false);
			}
			else
			{
				await SendMatchStateAsync(code, data, true);
			}			
		}

		public void CreateRoom(string nameRoom, int totalPlayers)
		{
			FindMatch(nameRoom, totalPlayers);
		}

		public void JoinRoom(string nameRoom)
		{
			FindMatch(nameRoom, -1);
		}

		private async void Update()
        {
			if (_uid != -1)
            {
				while (_events.Count > 0)
				{
					ItemMultiObjectEntry newMessage = _events[0];
					_events.RemoveAt(0);
					SendNakamaMessage((long)newMessage.Objects[0], (string)newMessage.Objects[1]);
				}				
			}
		}
#endif		
	}
}

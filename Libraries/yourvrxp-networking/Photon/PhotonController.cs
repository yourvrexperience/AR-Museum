#if ENABLE_PHOTON
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;
using UnityEngine.SceneManagement;

namespace yourvrexperience.Networking
{
    public class PhotonController :
#if ENABLE_PHOTON
    MonoBehaviourPunCallbacks
#else
    MonoBehaviour
#endif
    {
#if ENABLE_PHOTON
        public const bool DebugMessages = false;

		public const string EventPhotonControllerListRoomsUpdated = "EventPhotonControllerListRoomsUpdated";

        public const string MY_ROOM_NAME = "MyRoomName";
        public const byte PhotonEventCode = 99;

        private static PhotonController _instance;

        public static PhotonController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(PhotonController)) as PhotonController;
                }
                return _instance;
            }
        }

        private int _uniqueNetworkID = -1;
		private bool _hasStartedConnection = false;
        private bool _isConnected = false;
		private bool _isInited = false;
		private bool _isInLobby = false;
		private bool _isInRoom = false;
        private List<RoomData> _roomsLobby = new List<RoomData>();
        private int _totalNumberOfPlayers = -1;
        private RaiseEventOptions _raiseEventOptions = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.All,
            CachingOption = EventCaching.DoNotCache
        };
        private SendOptions _sendOptions = new SendOptions { Reliability = true };

        public int UniqueNetworkID
        {
            get { return _uniqueNetworkID; }
        }
        public bool IsServer
        {
            get { return PhotonNetwork.IsMasterClient; }
        }
        public bool IsConnected
        {
            get { return _uniqueNetworkID != -1; }
        }
		public string ServerAddress
        {
            get {  return PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime;  }
            set {       }
        }
		public bool IsInLobby
		{
			get { return _isInLobby; }
		}
		public bool IsInRoom
		{
			get { return _isInRoom; }
		}

        public void Initialize()
        {
			_isInited = true;
        }

        public void Connect()
        {
			if (!_hasStartedConnection)
			{
				_isInited = true;
				_hasStartedConnection = true;
				PhotonNetwork.LocalPlayer.NickName = yourvrexperience.Utils.Utilities.RandomCodeGeneration(4) + "_" + UnityEngine.Random.Range(100, 999).ToString();
				PhotonNetwork.ConnectUsingSettings();

				PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;

				SystemEventController.Instance.Event += OnSystemEvent;
			}
			else
			{
				NetworkController.Instance.DispatchEvent(NetworkController.EventNetworkControllerListRoomsUpdated, _roomsLobby);	
			}
        }

		public void Disconnect()
		{
			if (_isInited)
			{
				_hasStartedConnection = false;
				_isInited = false;
				_isConnected = false;
				_totalNumberOfPlayers = -1;
				_uniqueNetworkID = -1;
				_roomsLobby.Clear();
				PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
				PhotonNetwork.Disconnect();

				if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
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

        private void OnPhotonEvent(EventData eventData)
        {
            if (eventData.Code == PhotonEventCode)
            {
                object[] data = (object[])eventData.CustomData;
                string eventMessage = (string)data[0];
                int originNetworkID = (int)data[1];
                int targetNetworkID = (int)data[2];
                object[] paramsEvent = null;
                if (data.Length > 3)
                {
                    paramsEvent = new object[data.Length - 3];
                    for (int i = 3; i < data.Length; i++)
                    {
                        paramsEvent[i - 3] = (object)data[i];
                    }
                }
                NetworkController.Instance.DispatchEvent(eventMessage, originNetworkID, targetNetworkID, paramsEvent);
            }
        }

        public void DispatchNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, params object[] parameters)
        {
            object[] data = new object[3 + parameters.Length];
            data[0] = nameEvent;
            data[1] = originNetworkID;
            data[2] = targetNetworkID;
            if (parameters.Length > 0)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    data[3 + i] = parameters[i];
                }
            }
            PhotonNetwork.RaiseEvent(PhotonEventCode, data, _raiseEventOptions, _sendOptions);
        }

        public override void OnConnectedToMaster()
        {
            if (DebugMessages) yourvrexperience.Utils.Utilities.DebugLogColor("PhotonController::OnLeftLobby", Color.red);
            _isConnected = true;
            GetListRooms();
        }

        public void GetListRooms()
        {
            if (!PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
                if (DebugMessages) yourvrexperience.Utils.Utilities.DebugLogColor("PhotonController::GetListRooms:REQUEST TO JOIN THE LOBBY", Color.red);
            }
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
			_isInLobby = true;
            if (DebugMessages) yourvrexperience.Utils.Utilities.DebugLogColor("PhotonController::OnRoomListUpdate:roomList.Count[" + roomList.Count + "]", Color.red);
            _roomsLobby.Clear();
            for (int i = 0; i < roomList.Count; i++)
            {
                RoomInfo info = roomList[i];
                if (!info.IsOpen || !info.IsVisible || info.RemovedFromList) continue;
                _roomsLobby.Add(new RoomData() { NameRoom = info.Name, ExtraData = "extraData", TotalPlayers =  info.MaxPlayers });
            }
			NetworkController.Instance.DispatchEvent(NetworkController.EventNetworkControllerListRoomsUpdated, _roomsLobby);

            if (DebugMessages) yourvrexperience.Utils.Utilities.DebugLogColor("PhotonController::OnRoomListUpdate::REPORTING LIST OF ROOMS[" + _roomsLobby.Count + "]", Color.red);
        }

        public void CreateRoom(string nameRoom, int totalNumberOfPlayers)
        {
            if (_totalNumberOfPlayers == -1)
            {
                _totalNumberOfPlayers = totalNumberOfPlayers;
                RoomOptions options = new RoomOptions { MaxPlayers = (byte)_totalNumberOfPlayers, PlayerTtl = 10000 };
                PhotonNetwork.CreateRoom(nameRoom, options, null);
                if (DebugMessages) yourvrexperience.Utils.Utilities.DebugLogColor("PhotonController::CreateRoom::CREATING THE ROOM...", Color.red);
            }
        }

        public void JoinRoom(string room)
        {
            if (_totalNumberOfPlayers == -1)
            {
                _totalNumberOfPlayers = -999999;
                if (PhotonNetwork.InLobby)
                {
                    PhotonNetwork.LeaveLobby();
                }
                PhotonNetwork.JoinRoom(room);
                if (DebugMessages) yourvrexperience.Utils.Utilities.DebugLogColor("PhotonController::JoinRoom::JOINING THE ROOM....", Color.red);
            }
        }

        public override void OnLeftLobby()
        {
            if (DebugMessages) yourvrexperience.Utils.Utilities.DebugLogColor("PhotonController::OnLeftLobby", Color.red);
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            if (DebugMessages) yourvrexperience.Utils.Utilities.DebugLogColor("PhotonController::OnCreateRoomFailed", Color.red);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            if (DebugMessages) yourvrexperience.Utils.Utilities.DebugLogColor("PhotonController::OnJoinRoomFailed", Color.red);
        }

        public override void OnJoinedRoom()
        {
            if (_uniqueNetworkID == -1)
            {
                _uniqueNetworkID = PhotonNetwork.LocalPlayer.ActorNumber;
				_isInRoom = true;
				_isInLobby = false;
                NetworkController.Instance.DispatchEvent(NetworkController.EventNetworkControllerConnectionWithRoom, _uniqueNetworkID);
                if (DebugMessages) yourvrexperience.Utils.Utilities.DebugLogColor("PhotonController::OnJoinedRoom::UniqueNetworkID[" + UniqueNetworkID + "]::MasterClient[" + PhotonNetwork.IsMasterClient + "]", Color.red);
            }
        }

        public override void OnLeftRoom()
        {
            if (DebugMessages) yourvrexperience.Utils.Utilities.DebugLogColor("PhotonController::OnLeftRoom", Color.red);
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            int otherNetworkID = newPlayer.ActorNumber;
			NetworkController.Instance.DispatchEvent(NetworkController.EventNetworkControllerNewPlayerJoinedRoom, otherNetworkID);
            if (DebugMessages) yourvrexperience.Utils.Utilities.DebugLogColor("PhotonController::OnPlayerEnteredRoom::otherNetworkID[" + otherNetworkID + "]", Color.red);
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
			NetworkController.Instance.DispatchEvent(NetworkController.EventNetworkControllerPlayerDisconnected, otherPlayer.ActorNumber);
            if (DebugMessages) yourvrexperience.Utils.Utilities.DebugLogColor("PhotonController::OnPlayerLeftRoom", Color.red);
        }

        public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
        {
            if (DebugMessages) yourvrexperience.Utils.Utilities.DebugLogColor("PhotonController::OnMasterClientSwitched", Color.red);
        }

        public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (DebugMessages) yourvrexperience.Utils.Utilities.DebugLogColor("PhotonController::OnPlayerPropertiesUpdate", Color.red);
        }

        public GameObject CreateNetworkPrefab(bool _dontDestroyOnLoad, string uniqueNetworkName, GameObject prefab, string pathToPrefab, Vector3 position, Quaternion rotation, byte data, params object[] parameters)
        {
            GameObject networkInstance;
            if ((parameters != null) && (parameters.Length > 0))
            {
                networkInstance = PhotonNetwork.Instantiate(pathToPrefab, position, rotation, data, parameters);
            }
            else
            {
                networkInstance = PhotonNetwork.Instantiate(pathToPrefab, position, rotation, data);
            }
            networkInstance.name = uniqueNetworkName;
			networkInstance.GetComponent<NetworkObjectID>().NameObject = uniqueNetworkName;		
            NetworkController.Instance.DelayNetworkEvent(NetworkObjectID.EventNetworkObjectIDIdentity, 0.2f, -1, -1, networkInstance.GetComponent<NetworkObjectID>().GetOwnerID(), networkInstance.GetComponent<NetworkObjectID>().GetViewID(), networkInstance.GetComponent<NetworkObjectID>().GetIndexPrefab(), networkInstance.GetComponent<NetworkObjectID>().NameObject, NetworkController.Instance.IsMultipleScene, this.transform.position, this.transform.rotation);	
            yourvrexperience.Utils.Utilities.FixObject(networkInstance);
			if (_dontDestroyOnLoad)
			{
				DontDestroyOnLoad(networkInstance);
			}
            return networkInstance;
        }

		public void TransferOwnership(PhotonView target)
		{
			target.TransferOwnership(UniqueNetworkID);
		}

		public void LoadNewScene(string nextScene, string previousScene)
		{			
			PhotonNetwork.LoadLevel(nextScene);
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))
			{
				if (Instance)
				{
					DontDestroyOnLoad(Instance.gameObject);
				}
			}
        }
#endif
    }
}
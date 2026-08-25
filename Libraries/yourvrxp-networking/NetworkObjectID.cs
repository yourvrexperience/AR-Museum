#if ENABLE_PHOTON
using Photon.Pun;
#endif
#if ENABLE_MIRROR
using Mirror;
#endif
#if ENABLE_NETCODE
using Unity.Netcode;
using Unity.Multiplayer.Samples.yourvrexperience.Utils.Utilities.ClientAuthority;
#endif
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using yourvrexperience.Utils;
#if ENABLE_AVATAR_OCULUS
using yourvrexperience.VR;
#endif

namespace yourvrexperience.Networking
{
    public class NetworkObjectID :
#if ENABLE_PHOTON
    MonoBehaviour
#elif ENABLE_MIRROR
    NetworkBehaviour
#elif ENABLE_NAKAMA
    MonoBehaviour
#elif ENABLE_NETCODE
    NetworkBehaviour
#else
    MonoBehaviour
#endif
    {
		public const string EventNetworkObjectIDRequestStart = "EventNetworkObjectIDRequestStart";
		public const string EventNetworkObjectIDStarted = "EventNetworkObjectIDStarted";
		public const string EventNetworkObjectIDIdentity = "EventNetworkObjectIDIdentity";
		public const string EventNetworkObjectIDRequestData = "EventNetworkObjectIDRequestData";
		public const string EventNetworkObjectIDResponseData = "EventNetworkObjectIDResponseData";
		public const string EventNetworkObjectIDTransferOwnership = "EventNetworkObjectIDTransferOwnership";
		public const string EventNetworkObjectIDTransferCompletedOwnership = "EventNetworkObjectIDTransferCompletedOwnership";
		public const string EventNetworkObjectIDReportDestroyed = "EventNetworkObjectIDReportDestroyed";
		public const string EventNetworkObjectIDDestroy = "EventNetworkObjectIDDestroy";
		public const string EventNetworkObjectIDUpdateScale = "EventNetworkObjectIDUpdateScale";
		public const string EventNetworkObjectIDRefreshTransform = "EventNetworkObjectIDRefreshTransform";
		public const string EventNetworkObjectIDOwnershipOfServer = "EventNetworkObjectIDOwnershipOfServer";

		public delegate void InitedNetworkObjectEvent(string initializationData);

        public event InitedNetworkObjectEvent InitedEvent;

        public void DispatchInitedEvent(string initializationData)
        {
            if (InitedEvent != null) InitedEvent(initializationData);
        }

		public bool LinkedToCurrentLevel = false;

		private bool _hasBeenInited = false;		

		private string _initialInitializationData;
		private float _requestedTime = -1;

		private string _nameObject = null;
		private string _nameConfirmationObject = null;

		public string InitialInstantiationData
		{
			get { return _initialInitializationData; }
			set {
				_initialInitializationData = value;
				_hasBeenInited = true;
			}
		}

		public bool HasBeenInited
		{
			get { return _hasBeenInited; }
		}
		public string NameObject
		{
			get { return _nameObject; }
			set { 
				_nameObject = value; 
				this.gameObject.name = _nameObject;
			}
		}
		public string NameConfirmationObject
		{
			get { return _nameConfirmationObject; }
			set { _nameConfirmationObject = value; }
		}

#if ENABLE_AVATAR_OCULUS
		private OculusMetaAvatarEntity _avatarEntity;
		public OculusMetaAvatarEntity AvatarEntity
		{
			get {
				if (_avatarEntity == null)
				{
					_avatarEntity = this.GetComponentInChildren<OculusMetaAvatarEntity>();
				}
				return _avatarEntity;
			}
		}
#endif			

#if ENABLE_PHOTON
        private PhotonView m_photonView;
        public PhotonView PhotonView
        {
            get
            {
                if (m_photonView == null)
                {
                    if (this != null)
                    {
                        m_photonView = GetComponent<PhotonView>();
                    }
                }
                return m_photonView;
            }
        }
        private PhotonTransformView m_photonTransformView;
        public PhotonTransformView PhotonTransformView
        {
            get
            {
                if (m_photonTransformView == null)
                {
                    if (this != null)
                    {
                        m_photonTransformView = GetComponent<PhotonTransformView>();
                    }
                }
                return m_photonTransformView;
            }
        }
#elif ENABLE_MIRROR
        [SyncVar]
        public int Owner;

        [SyncVar]
        public int InstanceID;

        public object[] InstantiationData;

        private NetworkIdentity _mirrorView;

        public NetworkIdentity MirrorView
        {
            get
            {
                if (_mirrorView == null)
                {
                    if (this != null)
                    {
                        _mirrorView = GetComponent<NetworkIdentity>();
                    }
                }
                return _mirrorView;
            }
        }

#elif ENABLE_NAKAMA		
		private NakamaIdentity _nakamaView;

        public NakamaIdentity NakamaView
        {
            get
            {
                if (_nakamaView == null)
                {
                    if (this != null)
                    {
                        _nakamaView = GetComponent<NakamaIdentity>();
                    }
                }
                return _nakamaView;
            }
        }
#elif ENABLE_NETCODE
		public NetworkVariable<int> InstanceID = new NetworkVariable<int>();
		public NetworkVariable<int> OwnerID = new NetworkVariable<int>();

		private NetworkObject _netCodeView;

        public NetworkObject NetCodeView
        {
            get
            {
                if (_netCodeView == null)
                {
                    if (this != null)
                    {
                        _netCodeView = GetComponent<NetworkObject>();
                    }
                }
                return _netCodeView;
            }
        }
#elif ENABLE_SOCKETS	
		private SocketIdentity _socketView;

        public SocketIdentity SocketView
        {
            get
            {
                if (_socketView == null)
                {
                    if (this != null)
                    {
                        _socketView = GetComponent<SocketIdentity>();
                    }
                }
                return _socketView;
            }
        }
#endif

		void Start()
		{
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent += OnNetworkEvent;
			SystemEventController.Instance.Event += OnSystemEvent;
			NetworkController.Instance.DelayNetworkEvent(EventNetworkObjectIDRequestStart, 0.1f, -1, -1, GetViewID(), NetworkController.Instance.IsServer);
		}

		void OnDestroy()
		{			
			if (SystemEventController.Instance != null)
			{
				SystemEventController.Instance.Event -= OnSystemEvent;
				SystemEventController.Instance.DispatchSystemEvent(EventNetworkObjectIDReportDestroyed, this.gameObject);
			} 
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
		}

		public void Initialize(string nameEvent)
        {
#if ENABLE_PHOTON
            if (PhotonView != null)
            {
                if (PhotonView.InstantiationData != null)
                {
                    if (PhotonView.InstantiationData.Length > 0)
                    {
                        int animationInitial = (int)PhotonView.InstantiationData[0];
                        StartCoroutine(InitializeAnimationAvatar(nameEvent, animationInitial));
                    }
                }
            }
#endif
        }

		public void DisableNetworkComponents()
		{
#if ENABLE_PHOTON
            if (this.GetComponent<PhotonView>() != null) this.GetComponent<PhotonView>().enabled = false;
			if (this.GetComponent<PhotonTransformView>() != null) this.GetComponent<PhotonTransformView>().enabled = false;
#elif ENABLE_MIRROR
            if (this.GetComponent<NetworkIdentity>() != null) this.GetComponent<NetworkIdentity>().enabled = false;
			if (this.GetComponent<NetworkTransformUnreliable>() != null) this.GetComponent<NetworkTransformUnreliable>().enabled = false;
#elif ENABLE_NAKAMA
            if (this.GetComponent<NakamaIdentity>() != null) this.GetComponent<NakamaIdentity>().enabled = false;
			if (this.GetComponent<NakamaTransform>() != null) this.GetComponent<NakamaTransform>().enabled = false;
#elif ENABLE_NETCODE
            if (this.GetComponent<NetworkObject>() != null) this.GetComponent<NetworkObject>().enabled = false;
			if (this.GetComponent<ClientNetworkTransform>() != null) this.GetComponent<ClientNetworkTransform>().enabled = false;
#elif ENABLE_SOCKETS	
            if (this.GetComponent<SocketIdentity>() != null) this.GetComponent<SocketIdentity>().enabled = false;
			if (this.GetComponent<SocketTransform>() != null) this.GetComponent<SocketTransform>().enabled = false;
#endif
		}

        IEnumerator InitializeAnimationAvatar(string nameEvent, int animationInitial)
        {
            yield return new WaitForSeconds(0.1f);
            NetworkController.Instance.DispatchEvent(nameEvent, -1, -1, this.gameObject, animationInitial);
        }

        public bool IsConnected()
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

        public int GetViewID()
        {
#if ENABLE_PHOTON
            if (PhotonView != null) return PhotonView.ViewID;
#elif ENABLE_MIRROR
            if (MirrorView != null) return (int)MirrorView.netId;
#elif ENABLE_NAKAMA
            if (NakamaView != null) return (int)NakamaView.NetID;
#elif ENABLE_NETCODE
            if (NetCodeView != null) return (int)NetCodeView.NetworkObjectId;
#elif ENABLE_SOCKETS	
			if (SocketView != null) return (int)SocketView.NetID;
#endif
            return -1;
        }

        public bool IsMine()
        {
#if ENABLE_PHOTON
            if (PhotonView != null) return PhotonView.IsMine;
#elif ENABLE_MIRROR
            if ((MirrorView != null) && (NetworkController.Instance != null)) return Owner == NetworkController.Instance.UniqueNetworkID;
#elif ENABLE_NAKAMA
            if ((NakamaView != null) && (NetworkController.Instance != null))  return NakamaView.Owner == NetworkController.Instance.UniqueNetworkID;
#elif ENABLE_NETCODE
            if ((NetCodeView != null) && (NetworkController.Instance != null)) return (int)NetCodeView.OwnerClientId == NetworkController.Instance.UniqueNetworkID;
#elif ENABLE_SOCKETS	
			if ((SocketView != null) && (NetworkController.Instance != null))  return SocketView.Owner == NetworkController.Instance.UniqueNetworkID;
#endif
            return false;
        }

        public bool AmOwner()
        {
#if ENABLE_PHOTON
            if (PhotonView != null) return PhotonView.AmOwner;
#elif ENABLE_MIRROR
            if ((MirrorView != null)&& (NetworkController.Instance != null)) return Owner == NetworkController.Instance.UniqueNetworkID;
#elif ENABLE_NAKAMA
            if ((NakamaView != null) && (NetworkController.Instance != null)) return NakamaView.Owner == NetworkController.Instance.UniqueNetworkID;
#elif ENABLE_NETCODE
            if ((NetCodeView != null)&& (NetworkController.Instance != null)) return (int)NetCodeView.OwnerClientId == NetworkController.Instance.UniqueNetworkID;
#elif ENABLE_SOCKETS	
			if ((SocketView != null) && (NetworkController.Instance != null))  return SocketView.Owner == NetworkController.Instance.UniqueNetworkID;
#endif
            return false;
        }

        public int GetOwnerID()
        {
#if ENABLE_PHOTON
            if (PhotonView != null) return PhotonView.OwnerActorNr;
#elif ENABLE_MIRROR
            if ((MirrorView != null)&& (NetworkController.Instance != null)) return Owner;
#elif ENABLE_NAKAMA
            if ((NakamaView != null) && (NetworkController.Instance != null)) return NakamaView.Owner;
#elif ENABLE_NETCODE
            if ((NetCodeView != null)&& (NetworkController.Instance != null)) return (int)NetCodeView.OwnerClientId;
#elif ENABLE_SOCKETS
            if ((SocketView != null) && (NetworkController.Instance != null)) return SocketView.Owner;
#endif
            return -1;
        }

		public int GetIndexPrefab()
		{
#if ENABLE_NAKAMA
            if ((NakamaView != null) && (NetworkController.Instance != null)) return NakamaView.IndexPrefab;
#elif ENABLE_SOCKETS
            if ((SocketView != null) && (NetworkController.Instance != null)) return SocketView.IndexPrefab;
#endif
            return -1;			
		}

        public void Destroy()
        {
#if ENABLE_PHOTON
			if ((PhotonView != null) && PhotonView.IsMine) PhotonNetwork.Destroy(PhotonView);
#elif ENABLE_MIRROR
			if (MirrorView != null) MirrorController.Instance.Connection.CmdDestroy(MirrorView);
#elif ENABLE_NETCODE
			if (NetCodeView != null) NetCodeController.Instance.Connection.DestroyServerRpc((int)NetCodeView.NetworkObjectId);
#else
			GameObject.Destroy(this.gameObject);
#endif
        }

        public void SetEnabled(bool enabled)
        {
#if ENABLE_PHOTON
            if (PhotonView != null) PhotonView.enabled = enabled;
            if (PhotonTransformView != null) PhotonTransformView.enabled = enabled;
#elif ENABLE_MIRROR
            if (MirrorView != null) MirrorView.enabled = enabled;
#elif ENABLE_NAKAMA
            if (NakamaView != null) NakamaView.enabled = enabled;
#elif ENABLE_NETCODE
            if (NetCodeView != null) NetCodeView.enabled = enabled;
#elif ENABLE_SOCKETS
            if (SocketView != null) SocketView.enabled = enabled;
#endif
        }

		public void RefreshAuthority()
		{
#if ENABLE_MIRROR
			if (AmOwner())
			{
				if (MirrorController.Instance.Connection != null)
				{
					MirrorController.Instance.Connection.CmdAssignNetworkAuthority(this.GetComponent<NetworkIdentity>(), MirrorController.Instance.Connection.netIdentity);
				}				
			}
#endif
		}

		public void RequestAuthority()
		{			
#if ENABLE_MIRROR
			if (!AmOwner())
			{
				if (MirrorController.Instance.Connection != null)
				{
					MirrorController.Instance.Connection.CmdAssignNetworkAuthority(this.GetComponent<NetworkIdentity>(), MirrorController.Instance.Connection.netIdentity);
				}				
			}
#elif ENABLE_PHOTON
			if (!AmOwner())
			{
				PhotonController.Instance.TransferOwnership(this.GetComponent<PhotonView>());
				NetworkController.Instance.DelayNetworkEvent(EventNetworkObjectIDTransferOwnership, 0.2f, -1, -1, GetViewID(), NetworkController.Instance.UniqueNetworkID);
			}		
#elif ENABLE_NAKAMA
			if (!AmOwner())
			{
				NetworkController.Instance.DispatchNetworkEvent(EventNetworkObjectIDTransferOwnership, -1, -1, GetViewID(), NetworkController.Instance.UniqueNetworkID);
			}		
#elif ENABLE_NETCODE
			if (!AmOwner())
			{
				if (NetCodeController.Instance.Connection != null)
				{
					NetCodeController.Instance.Connection.AssignNetworkAuthorityServerRpc((int)this.GetComponent<NetworkObject>().NetworkObjectId, NetCodeController.Instance.UniqueNetworkID);
				}				
			}
#elif ENABLE_SOCKETS
			if (!AmOwner())
			{
				NetworkController.Instance.DispatchNetworkEvent(EventNetworkObjectIDTransferOwnership, -1, -1, GetViewID(), NetworkController.Instance.UniqueNetworkID);
			}		
#endif
		}

		private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			if (nameEvent.Equals(EventNetworkObjectIDRequestStart))
			{
				if (NetworkController.Instance.IsServer)
				{
					int targetNetID = (int)parameters[0];
					if (GetViewID() == targetNetID)
					{
						NetworkController.Instance.DispatchNetworkEvent(EventNetworkObjectIDIdentity, -1, -1, GetOwnerID(), GetViewID(), GetIndexPrefab(), NameObject, NetworkController.Instance.IsMultipleScene);
					}
				}
			}
			if (nameEvent.Equals(EventNetworkObjectIDRequestData))
			{
				int targetNetID = (int)parameters[0];
				bool isTargetRequest = false;
#if ENABLE_MIRROR				
				isTargetRequest = (GetViewID() == targetNetID) && _hasBeenInited;				
#else
				isTargetRequest = (GetViewID() == targetNetID) && AmOwner() && _hasBeenInited;
#endif
				if (isTargetRequest)
				{
#if ENABLE_MIRROR
					if (this.GetComponent<NetworkTransformUnreliable>() != null)
					{
						this.GetComponent<NetworkTransformUnreliable>().SetDirty();
					}
#endif
					NetworkController.Instance.DispatchNetworkEvent(EventNetworkObjectIDResponseData, -1, -1, GetViewID(), _initialInitializationData);
				}
			}
			if (nameEvent.Equals(EventNetworkObjectIDResponseData))
			{
				int targetNetID = (int)parameters[0];
				if ((GetViewID() == targetNetID) && !AmOwner() && !_hasBeenInited)
				{
					_hasBeenInited = true;
					_initialInitializationData = (string)parameters[1];
					DispatchInitedEvent(_initialInitializationData);
				}
			}
			if (nameEvent.Equals(NetworkController.EventNetworkControllerNewPlayerJoinedRoom))
			{
				if (AmOwner())	
				{
#if ENABLE_MIRROR					
					if (this.GetComponent<NetworkTransformUnreliable>() != null)
					{
						this.GetComponent<NetworkTransformUnreliable>().SetDirty();
					}
#endif					
				}
			}
			if (nameEvent.Equals(EventNetworkObjectIDTransferOwnership))
			{
				int targetNetID = (int)parameters[0];
				if (GetViewID() == targetNetID)
				{
#if ENABLE_NAKAMA					
					this.GetComponent<NakamaIdentity>().Owner = (int)parameters[1];
#elif ENABLE_SOCKETS
					this.GetComponent<SocketIdentity>().Owner = (int)parameters[1];
#elif ENABLE_MIRROR 
					Owner = (int)parameters[1];
#endif			

					if (NetworkController.Instance.IsServer && AmOwner())
					{
						NetworkController.Instance.DispatchNetworkEvent(EventNetworkObjectIDOwnershipOfServer, -1, -1, targetNetID);
					}					
					NetworkController.Instance.DispatchEvent(EventNetworkObjectIDTransferCompletedOwnership, targetNetID);
				}
			}
			if (nameEvent.Equals(EventNetworkObjectIDIdentity))
			{
				int owner = (int)parameters[0];
				int netID = (int)parameters[1];
				int indexPrefab = (int)parameters[2];
				if (netID == GetViewID())
				{					
					if (NameConfirmationObject == null)
					{						
						string nameObject = (string)parameters[3];
						NameObject = nameObject;
						NameConfirmationObject = nameObject;		
						bool dontDestroyOnLoad = (bool)parameters[4];
						if (dontDestroyOnLoad)
						{
							DontDestroyOnLoad(this.gameObject);
						}
#if ENABLE_NAKAMA					
						this.gameObject.GetComponent<NakamaIdentity>().Set(owner, netID, indexPrefab);
#elif ENABLE_SOCKETS
						this.gameObject.GetComponent<SocketIdentity>().Set(owner, netID, indexPrefab);
#endif					
						SystemEventController.Instance.DispatchSystemEvent(EventNetworkObjectIDStarted, _nameObject, this);
						if (this.GetComponent<INetworkObject>() != null)
						{
							this.GetComponent<INetworkObject>().ActivatePhysics(true);
						}
					}
				}				
			}
			if (nameEvent.Equals(EventNetworkObjectIDDestroy))
			{
				int netID = (int)parameters[0];
				if (netID == GetViewID())
				{
					if (AmOwner())
					{
						Destroy();
					}
				}
			}
			if (nameEvent.Equals(EventNetworkObjectIDUpdateScale))
			{
				int netID = (int)parameters[0];
				if (netID == GetViewID())
				{
					this.gameObject.transform.localScale = (Vector3)parameters[1];
				}
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(EventNetworkObjectIDRefreshTransform))
			{
				int netID = (int)parameters[0];
				if (netID == GetViewID())
				{
					this.GetComponent<INetworkObject>().ActivatePhysics(true);
					this.transform.position = (Vector3)parameters[1];
					this.transform.rotation = (Quaternion)parameters[2];
				}
			}
		}

		private void RequestInitializationDataToOwner()
		{
			if (!_hasBeenInited)
			{
				if (!AmOwner())
				{
					if (_requestedTime == -1)
					{
						if (NetworkController.Instance.IsInRoom)
						{
							_requestedTime = 1;
							NetworkController.Instance.DispatchNetworkEvent(EventNetworkObjectIDRequestData, -1, -1, GetViewID());
						}
					}
					else
					{
						if (_requestedTime > 0)
						{
							_requestedTime -= Time.deltaTime;
							if (_requestedTime <= 0)
							{
								_requestedTime = -1;
							}
						}
					}					
				}
			}
		}

		private void Update()
		{
			RequestInitializationDataToOwner();
		}

    }
}
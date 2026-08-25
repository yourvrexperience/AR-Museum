using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;
#if ENABLE_MIRROR	
using Mirror;
#endif

namespace yourvrexperience.Networking
{
	public class NetworkPrefab : 
#if ENABLE_MIRROR	
	NetworkBehaviour
#else
	MonoBehaviour
#endif	
	, INetworkInitialData
	{
		public const string EventNetworkPrefabHasStarted = "EventNetworkPrefabHasStarted";
		public const string EventNetworkPrefabUpdateNameObject = "EventNetworkPrefabUpdateNameObject";
		public const string EventNetworkPrefabRefreshTransform = "EventNetworkPrefabRefreshTransform";
		public const string EventNetworkPrefabTakeControlConfirmation = "EventNetworkPrefabTakeControlConfirmation";
		public const string EventNetworkPrefabObjectReleasedControlConfirmed = "EventNetworkPrefabObjectReleasedControlConfirmed";

		public const string Separator = "<np>";

		[SerializeField] protected bool IsInLevel = false;
		[SerializeField] protected string NetworkPrefabName = "";
		[SerializeField] protected string NetworkPathName = "";

		private Rigidbody _networkRigidBody;
		private Collider _networkCollider;
		protected bool _requestedOwnership = false;
		protected float _restoreServerAuthority = 0;

		private NetworkObjectID _networkGameID;
		public NetworkObjectID NetworkGameIDView
		{
			get
			{
				if (_networkGameID == null)
				{
					if (this != null)
					{
						_networkGameID = GetComponent<NetworkObjectID>();
					}
				}
				return _networkGameID;
			}
		}
		public string ProviderName
		{
			get { return "NetworkPrefab"; }
		}
		protected Rigidbody NetworkRigidBody
		{
			get {
				if (_networkRigidBody == null)
				{
					_networkRigidBody = this.GetComponent<Rigidbody>();
				}
				return _networkRigidBody;
			}
		}
		protected Collider NetworkCollider
		{
			get {
				if (_networkCollider == null)
				{
					_networkCollider = this.GetComponent<Collider>();
				}
				return _networkCollider;
			}
		}

		public string NameNetworkPrefab
		{ 
			get { return NetworkPrefabName; }
		}
		public string NameNetworkPath
		{ 
			get { return NetworkPathName; } 
		}
		public virtual bool LinkedToCurrentLevel
		{
			get { return false; }
		}

		public virtual string GetInitialData()
		{
			string output = yourvrexperience.Utils.Utilities.SerializeVector3(this.transform.position) + Separator + 
					yourvrexperience.Utils.Utilities.SerializeQuaternion(this.transform.rotation) + Separator + 
					yourvrexperience.Utils.Utilities.SerializeVector3(this.transform.localScale);
			return output;
		}

		public virtual void ApplyInitialData(string data, bool linkedToLevel)
		{
			string[] dataArray = data.Split(Separator, StringSplitOptions.None);
			this.transform.position = yourvrexperience.Utils.Utilities.DeserializeVector3(dataArray[0]);
			this.transform.rotation = yourvrexperience.Utils.Utilities.DeserializeQuaternion(dataArray[1]);
			this.transform.localScale = yourvrexperience.Utils.Utilities.DeserializeVector3(dataArray[2]);
			NetworkGameIDView.LinkedToCurrentLevel = linkedToLevel;	
		}

		protected virtual void Start()
		{
			if (!IsInLevel)
			{
				NetworkController.Instance.DispatchEvent(EventNetworkPrefabHasStarted, this.gameObject, IsInLevel, NetworkPrefabName, NetworkPathName);
#if ENABLE_MIRROR
                if (NetworkController.Instance.IsServer)
                {
                    NetworkGameIDView.Owner = -1;
                }
#endif                				
			}
			else
			{
				NetworkController.Instance.DispatchEvent(EventNetworkPrefabHasStarted, this.gameObject, IsInLevel, NetworkPrefabName, NetworkPathName);
			}

			NetworkController.Instance.NetworkEvent += OnNetworkEvent;
		}

		public bool GetIsInLevel()
		{
			return IsInLevel;
		}

		protected virtual void OnDestroy()
		{
			if (NetworkController.Instance != null)	NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
		}

		protected virtual void ReportedConfirmationOfOwnerShip()
		{
			SystemEventController.Instance.DelaySystemEvent(EventNetworkPrefabTakeControlConfirmation, 0.01f, this.gameObject);
		}

		protected virtual void ReportReleaseConfirmation()
		{
			SystemEventController.Instance.DispatchSystemEvent(EventNetworkPrefabObjectReleasedControlConfirmed, this.gameObject);
		}

		public virtual void RequestAuthority()
		{
			if (!NetworkGameIDView.AmOwner())
			{
				_requestedOwnership = true;
				NetworkGameIDView.RequestAuthority();
			}
			else
			{
				ReportedConfirmationOfOwnerShip();
			} 		
		}

		public virtual void ReleaseAuthority()
		{
			if (NetworkController.Instance.IsServer)
			{
				_restoreServerAuthority = 0.2f;
			}	
			ReportReleaseConfirmation();
        }

		protected virtual void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			if (nameEvent.Equals(NetworkObjectID.EventNetworkObjectIDTransferCompletedOwnership))
			{
				int targetNetID = (int)parameters[0];
				if (NetworkGameIDView.GetViewID() == targetNetID)
				{
					if (NetworkGameIDView.AmOwner())
					{
						if (_requestedOwnership)
						{
							_requestedOwnership = false;
							ReportedConfirmationOfOwnerShip();
						}
					}					
				}
			}
			if (nameEvent.Equals(NetworkObjectID.EventNetworkObjectIDOwnershipOfServer))			
			{
				int targetNetID = (int)parameters[0];
				if (NetworkGameIDView.GetViewID() == targetNetID)
				{
					if (NetworkGameIDView.AmOwner())
					{
						NetworkController.Instance.DispatchNetworkEvent(EventNetworkPrefabRefreshTransform, -1, -1, NetworkGameIDView.GetViewID(), this.transform.position, this.transform.rotation, this.transform.localScale);
#if ENABLE_MIRROR						
						NetworkController.Instance.DelayNetworkEvent(EventNetworkPrefabRefreshTransform, 0.2f, -1, -1, NetworkGameIDView.GetViewID(), this.transform.position, this.transform.rotation, this.transform.localScale);
#endif						
					}					
				}
			}	
			if (nameEvent.Equals(EventNetworkPrefabRefreshTransform))
			{
				int targetNetID = (int)parameters[0];
				if (NetworkGameIDView.GetViewID() == targetNetID)
				{
					if (!NetworkGameIDView.AmOwner())
					{
						this.transform.position = (Vector3)parameters[1];
						this.transform.rotation = (Quaternion)parameters[2];
						this.transform.localScale = (Vector3)parameters[3];
					}					
				}
			}
			if (nameEvent.Equals(NetworkController.EventNetworkControllerClientLevelReady))
			{
				if (NetworkController.Instance.IsServer)
				{
					NetworkController.Instance.DispatchNetworkEvent(EventNetworkPrefabUpdateNameObject, -1, -1, NetworkGameIDView.GetViewID(), this.name, this.gameObject.transform.position, this.gameObject.transform.rotation, this.gameObject.transform.localScale);
				}
			}
			if (nameEvent.Equals(EventNetworkPrefabUpdateNameObject))
			{
				if (!NetworkController.Instance.IsServer)
				{
					int viewID = (int)parameters[0];
					string nameNetworkObject = (string)parameters[1];
					if (NetworkGameIDView.GetViewID() == viewID)
					{
						this.gameObject.name = nameNetworkObject;
						this.gameObject.transform.position = (Vector3)parameters[2];
						this.gameObject.transform.rotation = (Quaternion)parameters[3];
						this.gameObject.transform.localScale = (Vector3)parameters[4];
					}
				}
			}
		}

		protected virtual bool RestoreServerAuthority()
		{
			if (NetworkController.Instance.IsServer)
			{
				if (_restoreServerAuthority > 0)
				{
					_restoreServerAuthority -= Time.deltaTime;
					if (_restoreServerAuthority <= 0)
					{
						if (!NetworkGameIDView.AmOwner())
						{
							NetworkGameIDView.RequestAuthority();
						}
						else
						{
							NetworkController.Instance.DispatchNetworkEvent(NetworkObjectID.EventNetworkObjectIDOwnershipOfServer, -1, -1, NetworkGameIDView.GetViewID());
						}
						return true;
					}
				}
			}
			return false;
		}

		protected virtual void Update()
		{
			RestoreServerAuthority();
		}
	}
}
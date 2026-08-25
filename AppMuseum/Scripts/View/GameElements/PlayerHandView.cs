using yourvrexperience.Utils;
using yourvrexperience.VR;
using UnityEngine;
using System;
using yourvrexperience.Networking;

namespace yourvrexperience.template6dof
{
	[RequireComponent(typeof(Collider))]
	[RequireComponent(typeof(Rigidbody))]	
	public class PlayerHandView : MonoBehaviour, INetworkObject
	{
		public const string EventPlayerViewHandHasStarted = "EventPlayerViewHandHasStarted";
		public const string EventPlayerViewHandDestroyedAvatar = "EventPlayerViewHandDestroyedAvatar";
		public const string EventPlayerHandViewRequestBody = "EventPlayerHandViewRequestBody";
		public const string EventPlayerHandViewInitBody = "EventPlayerHandViewInitBody";

		[SerializeField] private GameObject Mesh;
		[SerializeField] private XR_HAND Hand;
		[SerializeField] private string NameAssetBody;
		
		private GameObject _bodyAsset;
		private Color _color;
		private PlayerView _player;

		public PlayerView Player
		{
			get { return _player; }
			set { _player = value; }
		}

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

		public Color PlayerColor
		{
			get {return _color;}
			set { _color = value; 
				SetInitData(yourvrexperience.Utils.Utilities.PackColor(_color));
			}
		}
		public string NameNetworkPrefab 
		{
			get { return null; }
		}

		public string NameNetworkPath 
		{
			get { return null; }
		}
		public bool LinkedToCurrentLevel
		{
			get { return false; }
		}

		void Start()
		{
			SystemEventController.Instance.Event += OnSystemEvent;

			if (Hand == XR_HAND.left)
			{
				NameAssetBody = "ModelHandLeft";
			}
			else
			{
				NameAssetBody = "ModelHandRight";
			}

			NetworkGameIDView.InitedEvent += OnInitDataEvent;
#if ENABLE_MIRROR			
			NetworkGameIDView.RefreshAuthority();
#endif			

			if (NetworkGameIDView.AmOwner())
			{
				SystemEventController.Instance.DispatchSystemEvent(EventPlayerViewHandHasStarted, this);

				Mesh.SetActive(false);
				
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR
				VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerLinkWithHand, NetworkGameIDView.AmOwner(), this.gameObject, Hand);
#endif
				
				NetworkController.Instance.DelayNetworkEvent(EventPlayerHandViewInitBody, 0.1f, -1, -1, NetworkController.Instance.UniqueNetworkID, NetworkGameIDView.GetViewID(), NameAssetBody);
			}
			else
			{
				NetworkController.Instance.DelayNetworkEvent(EventPlayerHandViewRequestBody, 1f, -1, -1, NetworkController.Instance.UniqueNetworkID, NetworkGameIDView.GetViewID());
			}
			NetworkController.Instance.NetworkEvent += OnNetworkEvent;
		}

		void OnDestroy()
		{
			NetworkGameIDView.InitedEvent -= OnInitDataEvent;
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
			_player = null;
		}

        public void SetInitData(string initializationData)
		{
			NetworkGameIDView.InitialInstantiationData = initializationData;
		}

		public void OnInitDataEvent(string initializationData)
		{
			PlayerColor = yourvrexperience.Utils.Utilities.UnpackColor(initializationData);
			yourvrexperience.Utils.Utilities.ApplyColor(Mesh.transform, PlayerColor);
		}

		public void ActivatePhysics(bool activation, bool force = false)
		{
		}

        private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
        {
			if (nameEvent.Equals(EventPlayerHandViewRequestBody))
			{
				int netID = (int)parameters[0];
				int playerNetID = (int)parameters[1];
				if (NetworkGameIDView.GetViewID() == playerNetID)
				{
					if (NetworkGameIDView.AmOwner())
					{
						NetworkController.Instance.DelayNetworkEvent(EventPlayerHandViewInitBody, 0.1f, -1, -1, NetworkController.Instance.UniqueNetworkID, NetworkGameIDView.GetViewID(), NameAssetBody);
					}
				}
			}
            if (nameEvent.Equals(EventPlayerHandViewInitBody))
			{
				int netID = (int)parameters[0];
				int playerNetID = (int)parameters[1];
				string bodyPrefab = (string)parameters[2];
				if (NetworkGameIDView.GetViewID() == playerNetID)
				{
					if (!NetworkGameIDView.AmOwner())
					{
						if (_bodyAsset == null)
						{
							_bodyAsset = AssetBundleController.Instance.CreateGameObject(bodyPrefab) as GameObject;
#if UNITY_EDITOR || ENABLE_URP_SHADERS						
							yourvrexperience.Utils.Utilities.ResetMaterials(_bodyAsset);
#endif						
							_bodyAsset.transform.rotation = Mesh.transform.rotation;
							_bodyAsset.transform.parent = Mesh.transform;
							_bodyAsset.transform.localPosition = Vector3.zero;							
							_bodyAsset.transform.localScale *= 0.6f;
						}
					}
				}
			}
        }

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(EventPlayerViewHandDestroyedAvatar))
			{
				PlayerView playerDestroyed = (PlayerView)parameters[0];
				if (_player == playerDestroyed)
				{
					GameObject.Destroy(this.gameObject);
				}
			}
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))	
			{
				DontDestroyOnLoad(this.gameObject);
			}			
		}
	}
}

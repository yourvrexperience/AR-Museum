using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Networking;
using yourvrexperience.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.Networking
{
	public class PlayerNetwork : MonoBehaviour, INetworkObject
	{
		public const string EventPlayerNetworkStarted = "EventPlayerNetworkStarted";
		public const string EventPlayerNetworkAskBullet = "EventPlayerNetworkAskBullet";
		public const string EventPlayerNetworkCreateBullet = "EventPlayerNetworkCreateBullet";
		public const string EventPlayerNetworkImpact = "EventPlayerNetworkImpact";
		public const string EventPlayerNetworkUpdateLife = "EventPlayerNetworkUpdateLife";

		public const float Speed = 200;

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

		public Image Background;
		public Text NetIdentification;
		public Text NetLife;

		private Transform _content;
		private Color _color;

		private float _requestedTime = -1;
		private int _life = 100;

		public string NameNetworkPrefab 
		{
			get { return null; }
		}

		public string NameNetworkPath 
		{
			get { return null; }
		}
		
		public Color PlayerColor
		{
			get {return _color;}
			set {
				_color = value;
				Background.color = _color;
				SetInitData(yourvrexperience.Utils.Utilities.PackColor(_color));
			}
		}

		public bool LinkedToCurrentLevel
		{
			get { return false; }
		}

		void Start()
		{
			_content = this.transform.Find("Content");
			_content.gameObject.SetActive(false);

			NetworkGameIDView.InitedEvent += OnInitDataEvent;
			NetworkController.Instance.NetworkEvent += OnNetworkEvent;

			SystemEventController.Instance.DispatchSystemEvent(EventPlayerNetworkStarted, this, NetworkGameIDView.AmOwner());

			NetworkGameIDView.RefreshAuthority();
		}

		void OnDestroy()
		{
			if (NetworkGameIDView != null) NetworkGameIDView.InitedEvent -= OnInitDataEvent;
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
		}

		public void ActivatePhysics(bool activation, bool force = false)
		{
			
		}

		public void SetInitData(string initializationData)
		{
			NetworkGameIDView.InitialInstantiationData = initializationData;
			NetIdentification.text = NetworkGameIDView.GetViewID().ToString();
			if (NetLife != null) NetLife.text = _life.ToString();
			_content.gameObject.SetActive(true);
		}

		public void OnInitDataEvent(string initializationData)
		{
			PlayerColor = yourvrexperience.Utils.Utilities.UnpackColor(initializationData);
			NetIdentification.text = NetworkGameIDView.GetViewID().ToString();
			if (NetLife != null) NetLife.text = _life.ToString();
			_content.gameObject.SetActive(true);
		}

		void OnTriggerEnter(Collider other)
		{
			Bullet bullet = other.gameObject.GetComponent<Bullet>();
			if (bullet != null)
			{
				if (bullet.Owner != NetworkGameIDView.GetOwnerID())
				{
					if (NetworkController.Instance.UniqueNetworkID != bullet.Owner)
					{
						NetworkController.Instance.DispatchNetworkEvent(EventPlayerNetworkImpact, -1, -1, NetworkGameIDView.GetViewID(), bullet.Id);
					}
				}
			}
		}

		private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			if (nameEvent.Equals(EventPlayerNetworkImpact))
			{
				int viewIdImpactedPlayer = (int)parameters[0];
				if (NetworkGameIDView.AmOwner() && (NetworkGameIDView.GetViewID() == viewIdImpactedPlayer))
				{
					_life -= 10;
					NetworkController.Instance.DispatchNetworkEvent(EventPlayerNetworkUpdateLife, -1, -1, viewIdImpactedPlayer, _life);
				}
			}
			if (nameEvent.Equals(EventPlayerNetworkUpdateLife))
			{
				int viewIdImpactedPlayer = (int)parameters[0];
				if (NetworkGameIDView.GetViewID() == viewIdImpactedPlayer)
				{
					_life = (int)parameters[1];
					if (NetLife != null) NetLife.text = _life.ToString();
				}
			}
		}

		void Update()
		{
			if (NetworkGameIDView.HasBeenInited)
			{
				if (NetworkGameIDView.AmOwner())
				{
					Vector2 movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * Speed;
					if (movement != Vector2.zero)
					{
						this.transform.position += new Vector3(movement.x * Time.deltaTime, movement.y * Time.deltaTime, 0);
					}

					if (Input.GetMouseButtonDown(0))
					{
						Vector3 target = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
						string packedLocalPosition = yourvrexperience.Utils.Utilities.SerializeVector3(this.GetComponent<RectTransform>().anchoredPosition);
						string packedWorldPosition = yourvrexperience.Utils.Utilities.SerializeVector3(this.transform.position);
						string packedTargetPosition = yourvrexperience.Utils.Utilities.SerializeVector3(target);
						NetworkController.Instance.DispatchNetworkEvent(EventPlayerNetworkAskBullet, -1, -1, NetworkController.Instance.UniqueNetworkID, yourvrexperience.Utils.Utilities.PackColor(_color), packedLocalPosition, packedWorldPosition, packedTargetPosition);
					}
				}
			}
		}
	}
}
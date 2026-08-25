using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;
#if ENABLE_MIRROR
using Mirror;
#endif
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
using yourvrexperience.VR;
#endif
#if ENABLE_ULTIMATEXR
using UltimateXR.Manipulation;
#endif

namespace yourvrexperience.Networking
{
	[RequireComponent(typeof(ObjectPickable))]
	public class NetworkPickable : NetworkPrefab, INetworkObject, INetworkPickable
	{
		public const string EventNetworkPickableTakeControl = "EventNetworkPickableTakeControl";
		public const string EventNetworkPickableReleaseControl = "EventNetworkPickableReleaseControl";

		protected bool _enabled = true;

		protected int _layer;
        		
		protected IObjectPickable _objectPickable;

		public override bool LinkedToCurrentLevel
		{
			get { return true; }
		}
		
        public GameObject GetGameObject()
        {
            return this.gameObject;
        }

		protected override void Start()
		{
			base.Start();

			_layer = this.gameObject.layer;
			_objectPickable = this.GetComponent<IObjectPickable>();

			if (!IsInLevel)
			{
				bool shouldActivate = true;
				NetworkGameIDView.InitedEvent += OnInitDataEvent;
				shouldActivate = NetworkGameIDView.AmOwner();
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (!IsInLevel)
			{
				NetworkGameIDView.InitedEvent -= OnInitDataEvent;
			}
		}

		public virtual void SetInitData(string initializationData)
		{
			NetworkGameIDView.InitialInstantiationData = initializationData;
		}

		public virtual void OnInitDataEvent(string initializationData)
		{
		}

		public virtual void ActivatePhysics(bool activation, bool force = false)
		{
            if (_enabled || force)
            {
				if (_objectPickable == null)
				{
					_objectPickable = this.GetComponent<IObjectPickable>();
				}
				_objectPickable.ActivatePhysics(activation);
            }
		}

		protected override void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			base.OnNetworkEvent(nameEvent, originNetworkID, targetNetworkID, parameters);

			if (nameEvent.Equals(EventNetworkPickableTakeControl))
			{
				int viewID = (int)parameters[0];
				if (viewID == NetworkGameIDView.GetViewID())
				{
					_enabled = false;
					ActivatePhysics(false, true);
					this.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
				}
			}
			if (nameEvent.Equals(EventNetworkPickableReleaseControl))
			{
				int viewID = (int)parameters[0];
				if (viewID == NetworkGameIDView.GetViewID())
				{
					_enabled = true;
					this.gameObject.layer = _layer;
					ActivatePhysics(true);
					ReleaseAuthority();					
				}
			}			
		}

		public virtual bool ToggleControl()
		{
			_objectPickable.ToggleControl();
			if (_enabled)
			{
				RequestAuthority();
				NetworkController.Instance.DispatchNetworkEvent(EventNetworkPickableTakeControl, -1, -1, NetworkGameIDView.GetViewID());
			}
			else
			{
				if (NetworkGameIDView.AmOwner())
				{
					_enabled = true;
					NetworkController.Instance.DispatchNetworkEvent(EventNetworkPickableReleaseControl, -1, -1, NetworkGameIDView.GetViewID());
				}
			}
			return true;
		}

		protected override void Update()
		{
			base.Update();

			if (NetworkGameIDView.AmOwner())
			{
				if (!_enabled)
				{
					_objectPickable.IsGrabbed = true;
				}
				else
				{
					_objectPickable.IsGrabbed = false;
				}
			}
			AfterUpdate();
		}

		public virtual void AfterUpdate(){}

    }
}
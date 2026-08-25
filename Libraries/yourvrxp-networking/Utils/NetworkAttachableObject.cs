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

namespace yourvrexperience.Networking
{
	public class NetworkAttachableObject : NetworkPickable, INetworkObject, INetworkPickable
	{
		public const string EventNetworkAttachableObjectReset = "EventNetworkAttachableObjectReset";

		private LinkedAttachedObject _linkAttachedObject;
        private LinkedAttachedObject _linkAttachedObjectConfirmation;

		protected string _nameObjectToLink = "";

		void OnTriggerEnter(Collider collision)
        {
			if (!_enabled)
			{
				LinkedAttachedObject linkAttachedObject = collision.gameObject.GetComponent<LinkedAttachedObject>();
				if (linkAttachedObject != null)
				{
					if (_nameObjectToLink == linkAttachedObject.NameObject)
					{
						_linkAttachedObject = linkAttachedObject;
					}					
				}
			}
        }

        void OnTriggerExit(Collider collision)
        {
			if (!_enabled)
			{
				LinkedAttachedObject linkAttachedObject = collision.gameObject.GetComponent<LinkedAttachedObject>();
				if ((_linkAttachedObject != null) && (linkAttachedObject != null))
				{
					if (_nameObjectToLink == linkAttachedObject.NameObject)
					{
						_linkAttachedObject = null;
					}
				}
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
                    _linkAttachedObject = null;
                    _linkAttachedObjectConfirmation = null;
                    NetworkController.Instance.DispatchNetworkEvent(EventNetworkAttachableObjectReset, -1, -1, NetworkGameIDView.GetViewID());
                }
            }
            if (nameEvent.Equals(EventNetworkAttachableObjectReset))
            {
                int viewID = (int)parameters[0];
                if (viewID == NetworkGameIDView.GetViewID())
                {
                    _linkAttachedObject = null;
                    _linkAttachedObjectConfirmation = null;
                }
            }
        }

        protected override void ReportReleaseConfirmation()
        {
            base.ReportReleaseConfirmation();
            if (_linkAttachedObject != null)
            {
                _linkAttachedObjectConfirmation = _linkAttachedObject;
            }
        }

		protected override void Update()
		{
			base.Update();

			if (_linkAttachedObjectConfirmation != null)
			{
				this.transform.position = _linkAttachedObjectConfirmation.transform.position;
				this.transform.rotation = _linkAttachedObjectConfirmation.transform.rotation;
			}
		}
	}
}
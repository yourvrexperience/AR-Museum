using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace yourvrexperience.Utils
{
	[RequireComponent(typeof(Collider))]
	public class ColliderEventDispatcher : MonoBehaviour
	{
		public const string EventColliderEventDispatcherEntered = "EventColliderEventDispatcherEntered";
		public const string EventColliderEventDispatcherExited = "EventColliderEventDispatcherExited";
		
        void OnTriggerEnter(Collider collision)
        {
			SystemEventController.Instance.DispatchSystemEvent(EventColliderEventDispatcherEntered, this.gameObject, collision.gameObject);
        }

        void OnTriggerExit(Collider collision)
        {
			SystemEventController.Instance.DispatchSystemEvent(EventColliderEventDispatcherExited, this.gameObject, collision.gameObject);
		}

		void OnCollisionEnter(Collision collision)
        {
			SystemEventController.Instance.DispatchSystemEvent(EventColliderEventDispatcherEntered, this.gameObject, collision.gameObject);
		}

        void OnCollisionExit(Collision collision)
        {
			SystemEventController.Instance.DispatchSystemEvent(EventColliderEventDispatcherExited, this.gameObject, collision.gameObject);
		}
	}
}
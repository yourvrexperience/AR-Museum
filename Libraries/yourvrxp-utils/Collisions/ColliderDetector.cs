using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace yourvrexperience.Utils
{
	[RequireComponent(typeof(Collider))]
	public class ColliderDetector : MonoBehaviour
	{
		public delegate void CollisionEnterDetectorEvent(GameObject collider, GameObject other);
		public delegate void CollisionExitDetectorEvent(GameObject collider, GameObject other);

		public event CollisionEnterDetectorEvent CollisionEnterEvent;
		public event CollisionExitDetectorEvent CollisionExitEvent;

		private bool _isTrigger = true;

		public bool IsTrigger
		{
			get { return _isTrigger; }
			set { _isTrigger = value; }
		}

		public void DispatchCollisionEnterEvent(GameObject collider, GameObject other)
		{
			if (CollisionEnterEvent != null)
			{
				CollisionEnterEvent(collider, other);
			}
		}
		public void DispatchCollisionExitEvent(GameObject collider, GameObject other)
		{
			if (CollisionExitEvent != null)
			{
				CollisionExitEvent(collider, other);
			}
		}

        void OnTriggerEnter(Collider collision)
        {
			DispatchCollisionEnterEvent(this.gameObject, collision.gameObject);
        }

        void OnTriggerExit(Collider collision)
        {
			DispatchCollisionExitEvent(this.gameObject, collision.gameObject);
		}

        void OnCollisionEnter(Collision collision)
        {
			DispatchCollisionEnterEvent(this.gameObject, collision.gameObject);
        }

        void OnCollisionExit(Collision collision)
        {
			DispatchCollisionExitEvent(this.gameObject, collision.gameObject);
		}
	}
}
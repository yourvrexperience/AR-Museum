using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.VR
{
	public class FacePointerDetector : MonoBehaviour
	{
		public Action<bool, GameObject> DetectedFaceCollision;

		public GameObject Origin;
		public GameObject Target;
		public GameObject Menu;

		public float AngleActivation = 45;
		public float AngleDeactivation = 85;

		private bool _isColliding = false;
		private int _layerHand;

		void Start()
		{
			_layerHand = LayerMask.GetMask(PalmMenuController.LAYER_HAND);
		}

		void Update()
		{
			Vector3 forward = (Target.transform.position - Origin.transform.position).normalized;
			float angleToHead = Vector3.Angle(forward, -VRInputController.Instance.VRController.HeadController.transform.forward);
			if (!_isColliding)
			{
				if (angleToHead < AngleActivation)
				{
					RaycastHit collisionHandCheck = new RaycastHit();
					
					Vector3 collidedHand = RaycastingTools.GetRaycastOriginForward(VRInputController.Instance.VRController.HeadController.transform.position, VRInputController.Instance.VRController.HeadController.transform.forward, ref collisionHandCheck, 100,  _layerHand);
					if (collidedHand != Vector3.zero)
					{
						_isColliding = true;
						Origin.SetActive(false);
						DetectedFaceCollision?.Invoke(true, Menu);
					}
				}
			}
			else
			{
				if (angleToHead > AngleDeactivation)
				{
					_isColliding = false;
					Origin.SetActive(true);
					DetectedFaceCollision?.Invoke(false, Menu);
				}
			}
		}
	}
}

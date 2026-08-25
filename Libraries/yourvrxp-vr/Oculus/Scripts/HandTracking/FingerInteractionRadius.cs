using UnityEngine;
using System.Collections.Generic;
using System;

namespace yourvrexperience.VR
{

	public class FingerInteractionRadius : MonoBehaviour
	{
		public const string EventSphereInteractionRadiusInited = "EventSphereInteractionRadiusInited";

		public XR_HAND Hand;

		private SphereCollider _sphereCollider;

		private void Start()
		{
			_sphereCollider = this.gameObject.GetComponent<SphereCollider>();
			if (_sphereCollider == null)
			{
				throw new Exception("This component pattern should have the component SphereCollider to work[" + this.name + "]");
			}
			else
			{
				Invoke("ReportInteractionRadius", 0.1f);
				SetActive(false);
			}
			SetDebugMode(false);
		}

		private void ReportInteractionRadius()
		{
			VRInputController.Instance.DispatchVREvent(EventSphereInteractionRadiusInited, this.gameObject);
		}

		public void SetActive(bool _active)
        {
			this.gameObject.SetActive(_active);
        }

		public void SetDebugMode(bool _enableDebug)
		{
			if (_sphereCollider != null)
			{
				if (_sphereCollider.gameObject.activeSelf)
				{
					if (_sphereCollider.gameObject.GetComponent<MeshRenderer>() != null)
					{
						_sphereCollider.gameObject.GetComponent<MeshRenderer>().enabled = false;
					}					
				}
			}
		}

		public void SetRadius(float _radius)
		{
			if (_sphereCollider != null)
			{
				if (_sphereCollider.gameObject.activeSelf)
				{
					_sphereCollider.gameObject.transform.localScale = new Vector3(_radius, _radius, _radius);
					_sphereCollider.enabled = true;
				}
			}
		}
	}
}

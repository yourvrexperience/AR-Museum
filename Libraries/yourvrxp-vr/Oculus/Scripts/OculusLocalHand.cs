#if ENABLE_OCULUS
using Oculus.Interaction;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using yourvrexperience.Utils;

namespace yourvrexperience.VR
{
    public class OculusLocalHand : MonoBehaviour
    {
		[SerializeField] private GameObject Mesh;

		[SerializeField] private bool IsRightHand;

#if ENABLE_OCULUS

		void Start()
		{
			VRInputController.Instance.Event += OnVREvent;
		}

		void OnDestroy()
		{
			if (VRInputController.Instance != null) VRInputController.Instance.Event -= OnVREvent;
		}

		private void OnVREvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(OculusHandsManager.EventOculusHandsManagerStateChanged))
			{
				bool isActivatedHands = (bool)parameters[0];
				Mesh.SetActive(!isActivatedHands);
			}
		}
#endif
    }
}
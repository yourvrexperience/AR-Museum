#if ENABLE_OCULUS
using Oculus.Interaction;
using Oculus.Interaction.Input;
#endif
using UnityEngine;
using System;
using yourvrexperience.Utils;

namespace yourvrexperience.VR
{
	public class HandPalmToFacePoseRecognizer : MonoBehaviour
	{
		public const string EventHandPalmToFacePoseRecognizerTriggered = "EventHandPalmToFacePoseRecognizerTriggered";
		public const string EventHandPalmToFacePoseRecognizerReleased = "EventHandPalmToFacePoseRecognizerReleased";
#if ENABLE_OCULUS		
		[SerializeField] private SelectorUnityEventWrapper _selector = null;
		[SerializeField] private bool _isRightHand = false;

		private bool _handTrackingState = false;

		void Start()
		{
			_selector = this.GetComponent<SelectorUnityEventWrapper>();
			_selector.WhenSelected.AddListener(OnDetectedGesture);
			_selector.WhenUnselected.AddListener(OnUnDetectedGesture);

			VRInputController.Instance.Event += OnVREvent;
		}

		void OnDestroy()
		{
			if (VRInputController.Instance != null) VRInputController.Instance.Event -= OnVREvent;
		}

		private void OnDetectedGesture()
		{
			if (_handTrackingState)
			{
				VRInputController.Instance.DispatchVREvent(EventHandPalmToFacePoseRecognizerTriggered,  _isRightHand? XR_HAND.right: XR_HAND.left);
			}
		}

		private void OnUnDetectedGesture()
		{
			if (_handTrackingState)
			{
				VRInputController.Instance.DispatchVREvent(EventHandPalmToFacePoseRecognizerReleased, _isRightHand? XR_HAND.right: XR_HAND.left);
			}
		}

		private void OnVREvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(OculusHandsManager.EventOculusHandsManagerStateChanged) || nameEvent.Equals(OculusHandsManager.EventOculusHandsManagerStateInited))
            {
                _handTrackingState = (bool)parameters[0];
            }
		}
#endif
	}
}

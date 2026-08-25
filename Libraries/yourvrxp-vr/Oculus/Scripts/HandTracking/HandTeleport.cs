using UnityEngine;

namespace yourvrexperience.VR
{
	public class HandTeleport : MonoBehaviour
	{
		[SerializeField] private Transform _targetTeleport = null;
		[SerializeField] private XR_HAND _handTeleport = XR_HAND.none;

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
			if (nameEvent.Equals(OculusHandsManager.EventOculusHandsManagerStateChanged) || nameEvent.Equals(OculusHandsManager.EventOculusHandsManagerStateInited))
            {
                bool handTrackingState = (bool)parameters[0];
				if (handTrackingState)
				{
					VRInputController.Instance.DispatchVREvent(TeleportController.EventTeleportControllerUpdateTransformForward, true, _handTeleport, _targetTeleport);
				}
				else
				{
					VRInputController.Instance.DispatchVREvent(TeleportController.EventTeleportControllerUpdateTransformForward, false);
				}
            }
		}
#endif
	}
}

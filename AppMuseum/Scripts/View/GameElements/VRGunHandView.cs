using yourvrexperience.Utils;
using yourvrexperience.VR;

using UnityEngine;

namespace yourvrexperience.template6dof
{
	public class VRGunHandView : MonoBehaviour
	{
		public const string EventVRGunHandViewGrabGun = "EventVRGunHandViewGrabGun";
		public const string EventVRGunHandViewReleaseGun = "EventVRGunHandViewReleaseGun";

		[SerializeField] private GameObject OculusHand;
		[SerializeField] private GameObject GunHand;
		[SerializeField] private bool IsRightHand;

		void Start()
		{
			SystemEventController.Instance.Event += OnSystemEvent;
		}

        void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(EventVRGunHandViewGrabGun))
			{
				if ((VRInputController.Instance.VRController.HandSelected == XR_HAND.right) && IsRightHand)
				{
					OculusHand.SetActive(false);
					GunHand.SetActive(true);
				}
				if ((VRInputController.Instance.VRController.HandSelected == XR_HAND.left) && !IsRightHand)
				{
					OculusHand.SetActive(false);
					GunHand.SetActive(true);
				}
			}
			if (nameEvent.Equals(EventVRGunHandViewReleaseGun))
			{				
				OculusHand.SetActive(true);
				GunHand.SetActive(false);
			}
        }
    }
}

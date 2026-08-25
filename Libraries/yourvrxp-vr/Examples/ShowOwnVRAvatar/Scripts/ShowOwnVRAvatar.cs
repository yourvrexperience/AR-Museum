using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.VR
{
	public class ShowOwnVRAvatar : MonoBehaviour
	{
		[SerializeField] private GameObject VRHead;
		[SerializeField] private GameObject LeftHand;
		[SerializeField] private GameObject RightHand;

		[SerializeField] private Vector3 ShiftForward;

		void Start()
		{
			VRInputController.Instance.Initialize();
		}

		void Update()
		{
			VRHead.transform.position = VRInputController.Instance.VRController.HeadController.transform.position + ShiftForward;
			VRHead.transform.rotation = VRInputController.Instance.VRController.HeadController.transform.rotation;

			LeftHand.transform.position = VRInputController.Instance.VRController.HandLeftController.transform.position + ShiftForward;
			LeftHand.transform.rotation = VRInputController.Instance.VRController.HandLeftController.transform.rotation;

			RightHand.transform.position = VRInputController.Instance.VRController.HandRightController.transform.position + ShiftForward;
			RightHand.transform.rotation = VRInputController.Instance.VRController.HandRightController.transform.rotation;
		}
	}
}
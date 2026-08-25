using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.VR
{
	public class ScreenConfig : BaseScreenView, IScreenView
	{
		public const string EventScreenConfigChangeLocomotion = "EventScreenConfigChangeLocomotion";
		public const string EventScreenConfigDestroyed = "EventScreenConfigDestroyed";
		public const string EventScreenConfigDisconnect = "EventScreenConfigDisconnect";

		public const string ScreenName = "ScreenConfig";

		[SerializeField] private Button buttonLocomotionLeft;
		[SerializeField] private Button buttonLocomotionRight;
		[SerializeField] private TextMeshProUGUI leftHandInfo;
		[SerializeField] private TextMeshProUGUI rightHandInfo;
		[SerializeField] private Button buttonExit;

		private LocomotionMode _leftHand;
		private LocomotionMode _rightHand;


		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR	|| ENABLE_NREAL				
			_leftHand = VRInputController.Instance.LocomotionLeftHand;
			_rightHand = VRInputController.Instance.LocomotionRightHand;
#endif			
			buttonLocomotionLeft.onClick.AddListener(OnLocomotionLeft);
			buttonLocomotionRight.onClick.AddListener(OnLocomotionRight);
			leftHandInfo.text = _leftHand.ToString();
			rightHandInfo.text = _rightHand.ToString();
			buttonExit.onClick.AddListener(OnButtonExit);

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR		
			VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerEnableLocomotion, false);
#endif			
		}

		private void OnLocomotionRight()
		{
			_rightHand++;
			if ((int)_rightHand > 3) _rightHand = 0;
			rightHandInfo.text = _rightHand.ToString();
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR	|| ENABLE_NREAL				
			VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerChangeLocomotion, true, _rightHand);
#endif			
		}

		private void OnLocomotionLeft()
		{
			_leftHand++;
			if ((int)_leftHand > 3) _leftHand = 0;
			leftHandInfo.text = _leftHand.ToString();
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR	|| ENABLE_NREAL
			VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerChangeLocomotion, false, _leftHand);
#endif			
		}

		private void OnButtonExit()
		{
			UIEventController.Instance.DispatchUIEvent(EventScreenConfigDestroyed);
		}

		public override void Destroy()
		{
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR	|| ENABLE_NREAL				
			VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerEnableLocomotion, true);
#endif			
			base.Destroy();
		}
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
using UnityEngine;

namespace yourvrexperience.VR
{
	public class LocomotionTest : MonoBehaviour
	{
		private bool _configScreenVisible = false;

		void Start()
		{
			VRInputController.Instance.Initialize();
			VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerEnableLocomotion, true);

			UIEventController.Instance.Event += OnUIEvent;
		}

		void OnDestroy()
		{
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}

		private void OnUIEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(ScreenConfig.EventScreenConfigDestroyed))
			{
				ScreenController.Instance.DestroyScreens();
				_configScreenVisible = false;
			}
		}

		void Update()
		{
			if (VRInputController.Instance.ActionMenuPressed())
			{
				_configScreenVisible = !_configScreenVisible;
				if (_configScreenVisible)
				{
					ScreenController.Instance.CreateScreen(ScreenConfig.ScreenName, true, false);
				}
				else
				{
					UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyAllScreens);
				}
			}
		}
	}
}
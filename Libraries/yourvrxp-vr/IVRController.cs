using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace yourvrexperience.VR
{
    public interface IVRController
    {
		Camera Camera { get; }
		GameObject Container { get; }
		GameObject CurrentController { get; }
		GameObject OtherController { get; }
		XR_HAND HandSelected { get; }
		GameObject HeadController { get; }
		GameObject HandLeftController { get; }
        GameObject HandRightController { get; }
		LineRenderer RaycastLineLeft  { get; }
        LineRenderer RaycastLineRight  { get; }
		Vector3 PositionCollisionRaycasted  { get; set; }
		bool HandTrackingActive { get; }

		Vector2 GetVector2Joystick(XR_HAND hand);
		bool GetThumbstickDown(XR_HAND hand);
		bool GetThumbstickUp(XR_HAND hand);
		bool GetThumbstick(XR_HAND hand);
		bool GetIndexTriggerDown(XR_HAND hand, bool consume = true);
		bool GetIndexTriggerUp(XR_HAND hand, bool consume = true);
		bool GetIndexTrigger(XR_HAND hand);
		bool GetHandTriggerDown(XR_HAND hand, bool consume = true);
		bool GetHandTriggerUp(XR_HAND hand, bool consume = true);
		bool GetHandTrigger(XR_HAND hand);
		bool GetOneButtonDown(XR_HAND hand);
		bool GetOneButtonUp(XR_HAND hand);
		bool GetOneButton(XR_HAND hand);
		bool GetTwoButtonDown(XR_HAND hand);
		bool GetTwoButtonUp(XR_HAND hand);
		bool GetTwoButton(XR_HAND hand);		
		void UpdateHandSideController();
		void ResetState();
    }
}
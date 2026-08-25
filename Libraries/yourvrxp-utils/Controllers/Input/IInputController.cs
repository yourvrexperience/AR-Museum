using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace yourvrexperience.Utils
{
    public interface IInputController
    {
        public bool IsVR { get; }

        public Transform RayPointerVR { get; }
		public GameObject Content { get; }
		public Camera Camera { get; set; }
        public float SpeedJoystickMovement { get; set; }

        void Initialize();
        bool EnableMouseRotation();
        float GetMouseAxisHorizontal();
        float GetMouseAxisVertical();
        bool IsMoving();
        float GetAxisHorizontal();
        float GetAxisVertical();
        public bool ActionSecondaryDown();
        public bool ActionPrimaryDown();
        public bool ActionSecondaryUp();
        public bool ActionPrimaryUp();
        public bool ActionSecondary();
        public bool ActionPrimary();
		public bool ActionMenuPressed();
        bool SwitchedCameraPressed();
		void SetInitialPosition(Vector3 position, Quaternion rotation);
    }
}
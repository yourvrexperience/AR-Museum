using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.Utils
{
    public class MobileInputManager : StateMachine
    {
        public delegate void PrimaryActionButtonEvent();
        public delegate void SecondaryActionButtonEvent();
		public delegate void MenuActionButtonEvent();
        public delegate void CameraButtonEvent();

        public event PrimaryActionButtonEvent PrimaryActionEvent;
        public event SecondaryActionButtonEvent SecondaryActionEvent;
		public event MenuActionButtonEvent MenuActionEvent;
        public event CameraButtonEvent CameraEvent;

        public TouchJoystick MoveJoystick;
        public TouchJoystick RotateJoystick;
        public Button PrimaryActionButton;
        public Button SecondaryActionButton;
		public Button MenuActionButton;
        public Button CameraButton;

        protected virtual void Start()
        {
            if (PrimaryActionButton != null) PrimaryActionButton.onClick.AddListener(OnPrimaryActionButton);
            if (SecondaryActionButton != null) SecondaryActionButton.onClick.AddListener(OnSecondaryActionButton);
			if (MenuActionButton != null) MenuActionButton.onClick.AddListener(OnMenuActionButton);
            if (CameraButton != null) CameraButton.onClick.AddListener(OnCameraButton);
        }

        private void OnCameraButton()
        {
            if (CameraEvent != null) CameraEvent();
        }

        private void OnSecondaryActionButton()
        {
            if (SecondaryActionEvent != null) SecondaryActionEvent();
        }

        private void OnPrimaryActionButton()
        {
            if (PrimaryActionEvent != null) PrimaryActionEvent();
        }

        private void OnMenuActionButton()
        {
            if (MenuActionEvent != null) MenuActionEvent();
        }
    }
}
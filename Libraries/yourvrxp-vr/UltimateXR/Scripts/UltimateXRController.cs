using System;
using System.Collections;
using System.Collections.Generic;
#if ENABLE_ULTIMATEXR
using UltimateXR.Avatar;
using UltimateXR.Core;
using UltimateXR.Devices;
using UltimateXR.Locomotion;
#endif
using UnityEngine;

namespace yourvrexperience.VR
{
    public class UltimateXRController : MonoBehaviour
#if ENABLE_ULTIMATEXR
, IVRController
#endif
    {
		public const string EventUltimateXRControllerTeleportDone = "EventUltimateXRControllerTeleportDone";

		private static UltimateXRController instance;

        public static UltimateXRController Instance
        {
            get
            {
                if (!instance)
                {
                    instance = GameObject.FindObjectOfType(typeof(UltimateXRController)) as UltimateXRController;
                }
                return instance;
            }
        }
		[SerializeField] private Camera UltimateXRCamera;
        [SerializeField] public GameObject UltimateXRLeftController;
        [SerializeField] public GameObject UltimateXRRightController;

		[SerializeField] public GameObject UltimateXRLeftRay;
        [SerializeField] public GameObject UltimateXRRightRay;


#if ENABLE_ULTIMATEXR
		private Camera _mainCamera;
        private XR_HAND _handSelected = XR_HAND.none;
        private GameObject _currentController;	
		private GameObject _otherController;
        private LineRenderer _raycastLineLeft;
        private LineRenderer _raycastLineRight;
		private bool _controllersEnabled = false;

		public Camera Camera
		{
			get { 
				if (_mainCamera == null)
				{
					_mainCamera = UltimateXRCamera.GetComponentInChildren<Camera>();
				}
				return _mainCamera;
			}
		}
		public XR_HAND HandSelected
        {
            get {  return _handSelected; }
        }
		public GameObject Container 
		{
			get { return this.gameObject; }
		}
        public GameObject HeadController
		{
			get { return UltimateXRCamera.gameObject; }
		}
        public GameObject HandLeftController
		{
			get  {  return UltimateXRLeftRay; }
		}
		public GameObject HandRightController
		{
			get  {  return UltimateXRRightRay;  }
		}
		public GameObject CurrentController 
		{ 
			get { return _currentController; } 
		}
		public GameObject OtherController 
		{ 
			get { return _otherController; } 
		}

		public LineRenderer RaycastLineLeft
        {
            get {  return _raycastLineLeft; }
        }
        public LineRenderer RaycastLineRight
        {
            get { return _raycastLineRight; }
        }

		private UxrAvatar _uxrAvatar;
		public UxrAvatar UltimateXRAvatar
		{
			get { return _uxrAvatar; }
		}
		public bool HandTrackingActive
		{
			get { return !_controllersEnabled; }
		}
        public Vector3 PositionCollisionRaycasted 
		{ 
			get { return Vector3.zero; }
			set {}
		}

        private UxrControllerInput _controllerInput;

		private bool _rTriggerButtonDown = false, _lTriggerButtonDown = false, _rGripButtonDown = false, _lGripButtonDown = false, _rPrimaryButtonDown = false, _lPrimaryButtonDown = false, _rSecondaryButtonDown = false, _lSecondaryButtonDown = false;
		private bool _rTriggerButtonUp = false, _lTriggerButtonUp = false, _rGripButtonUp = false, _lGripButtonUp = false, _rPrimaryButtonUp = false, _lPrimaryButtonUp = false, _rSecondaryButtonUp = false, _lSecondaryButtonUp = false;

		private bool _rTriggerButtonState = false, _lTriggerButtonState = false, _rGripButtonState = false, _lGripButtonState = false, _rPrimaryButtonState = false, _lPrimaryButtonState = false, _rSecondaryButtonState = false, _lSecondaryButtonState = false;
		private bool _rTriggerButtonPrevState = false, _lTriggerButtonPrevState = false, _rGripButtonPrevState = false, _lGripButtonPrevState = false, _rPrimaryButtonPrevState = false, _lPrimaryButtonPrevState = false, _rSecondaryButtonPrevState = false, _lSecondaryButtonPrevState = false;

		public bool _rThumbstickButtonDown = false, _lThumbstickButtonDown = false;
		public bool _rThumbstickButtonUp = false, _lThumbstickButtonUp = false;

		public bool _rThumbstickButtonState = false, _lThumbstickButtonState = false;
		public bool _rThumbstickButtonPrevState = false, _lThumbstickButtonPrevState = false;

		private Vector2 _axisJoystickRight;
		private Vector2 _axisJoystickLeft;

		private bool _teleportActivated = false;
		private Vector3 _teleportStartingPosition = Vector3.zero;

		void Start()
		{
		}

		void OnDestroy()
        {
			if (_controllerInput != null)
			{
				_controllerInput.ButtonStateChanged -= ControllerInput_ButtonStateChanged;
				_controllerInput.Input2DChanged     -= ControllerInput_Axis2D;
			}
        }

		private void SetListeners()
		{
			UxrAvatar          avatar          = UxrAvatar.LocalAvatar;
            UxrControllerInput controllerInput = avatar != null ? avatar.ControllerInput : null;

            if (avatar != _uxrAvatar || controllerInput != _controllerInput)
            {
                if (_controllerInput != null)
                {
                    _controllerInput.ButtonStateChanged -= ControllerInput_ButtonStateChanged;
                    _controllerInput.Input2DChanged     -= ControllerInput_Axis2D;
                }

                _uxrAvatar                = avatar;
                _controllerInput = controllerInput;

                if (_controllerInput != null)
                {
                    _controllerInput.ButtonStateChanged += ControllerInput_ButtonStateChanged;
                    _controllerInput.Input2DChanged     += ControllerInput_Axis2D;
                }
			}
		}

		public Vector2 GetVector2Joystick(XR_HAND hand)
		{
			Vector2 rAxisJoystick = Vector2.zero, lAxisJoystick = Vector2.zero;
			switch (hand)
			{
				case XR_HAND.right:
					rAxisJoystick = _axisJoystickRight;
					break;
				case XR_HAND.left:
					lAxisJoystick = _axisJoystickLeft;
					break;
				case XR_HAND.both:
					rAxisJoystick = _axisJoystickRight;
					lAxisJoystick = _axisJoystickLeft;
					break;
			}
			
			return rAxisJoystick + lAxisJoystick;
		}

		private void ControllerInput_Axis2D(object sender, UxrInput2DEventArgs e)
		{
			UxrHandSide handSelected = e.HandSide;
			UxrInput2D input2D = e.Target;
			Vector2 axisValue = e.Value;

			switch (handSelected)
			{
				case UxrHandSide.Left:
					_axisJoystickLeft = axisValue;
					break;

				case UxrHandSide.Right:
					_axisJoystickRight = axisValue;
					break;
			}
		}

		private void ControllerInput_ButtonStateChanged(object sender, UxrInputButtonEventArgs e)
		{
            UxrControllerInput    controllerInput   = (UxrControllerInput)sender;
            UxrControllerElements controllerElement = UxrControllerInput.ButtonToControllerElement(e.Button);

			switch (controllerElement)
			{
				case UxrControllerElements.Button1:
					switch (e.ButtonEventType)
					{
						case UxrButtonEventType.PressDown:
							if (e.HandSide  == UxrHandSide.Right)
							{
								_rPrimaryButtonDown = true;
								SetLaserToRightHand();							
							}
							else
							{
								_lPrimaryButtonDown = true;
								SetLaserToLeftHand();							
							}
							break;
						case UxrButtonEventType.Pressing:
							if (e.HandSide  == UxrHandSide.Right)
							{
								_rPrimaryButtonState = true;
								SetLaserToRightHand();
							}
							else
							{
								_lPrimaryButtonState = true;
								SetLaserToLeftHand();
							}
							break;
						case UxrButtonEventType.PressUp:
							if (e.HandSide  == UxrHandSide.Right)
							{
								_rPrimaryButtonUp = true;
							}
							else
							{
								_lPrimaryButtonUp = true;
							}
							break;
					}
					break;
				
				case UxrControllerElements.Button2:
					switch (e.ButtonEventType)
					{
						case UxrButtonEventType.PressDown:
							if (e.HandSide  == UxrHandSide.Right)
							{
								_rSecondaryButtonDown = true;
								SetLaserToRightHand();
							}
							else
							{
								_lSecondaryButtonDown = true;
								SetLaserToLeftHand();							
							}
							break;
						case UxrButtonEventType.Pressing:
							if (e.HandSide  == UxrHandSide.Right)
							{
								_rSecondaryButtonState = true;
								SetLaserToRightHand();
							}
							else
							{
								_lSecondaryButtonState = true;
								SetLaserToLeftHand();							
							}
							break;
						case UxrButtonEventType.PressUp:
							if (e.HandSide  == UxrHandSide.Right)
							{
								_rSecondaryButtonUp = true;
							}
							else
							{
								_rSecondaryButtonUp = false;
							}
							break;
					}
					break;
				case UxrControllerElements.Trigger:
					switch (e.ButtonEventType)
					{
						case UxrButtonEventType.PressDown:
							if (e.HandSide  == UxrHandSide.Right)
							{
								_rTriggerButtonDown = true;
								SetLaserToRightHand();
							}
							else
							{
								_lTriggerButtonDown = true;
								SetLaserToLeftHand();
							}
							break;
						case UxrButtonEventType.Pressing:
							if (e.HandSide == UxrHandSide.Right)
							{
								_rTriggerButtonState = true;
								SetLaserToRightHand();
							}
							else
							{
								_lTriggerButtonState = false;
								SetLaserToLeftHand();
							}						
							break;
						case UxrButtonEventType.PressUp:
							if (e.HandSide == UxrHandSide.Right)
							{
								_rTriggerButtonUp = true;
							}
							else
							{
								_lTriggerButtonUp = true;
							}													
							break;
					}
					break;
				case UxrControllerElements.Grip:
					switch (e.ButtonEventType)
					{
						case UxrButtonEventType.PressDown:
							if (e.HandSide  == UxrHandSide.Right)
							{
								_rGripButtonDown = true;
								SetLaserToRightHand();
							}
							else
							{
								_lGripButtonDown = true;
								SetLaserToLeftHand();
							}
							break;
						case UxrButtonEventType.Pressing:
							if (e.HandSide  == UxrHandSide.Right)
							{
								_rGripButtonState = true;
								SetLaserToRightHand();
							}
							else
							{
								_lGripButtonState = true;
								SetLaserToLeftHand();
							}
							break;
						case UxrButtonEventType.PressUp:
							if (e.HandSide  == UxrHandSide.Right)
							{
								_rGripButtonUp = true;
								SetLaserToRightHand();
							}
							else
							{
								_lGripButtonUp = true;
								SetLaserToLeftHand();
							}
							break;
					}
					break;						
				case UxrControllerElements.Joystick:
					switch (e.ButtonEventType)
					{
						case UxrButtonEventType.PressDown:
							if (e.HandSide  == UxrHandSide.Right)
							{
								_rThumbstickButtonDown = true;
							}
							else
							{
								_lThumbstickButtonDown = true;
							}						
							break;
						case UxrButtonEventType.Pressing:
							if (e.HandSide  == UxrHandSide.Right)
							{
								_rThumbstickButtonState = true;
							}
							else
							{
								_lThumbstickButtonState = true;
							}						
							break;
						case UxrButtonEventType.PressUp:
							if (e.HandSide  == UxrHandSide.Right)
							{
								_rThumbstickButtonUp = true;
							}
							else
							{
								_lThumbstickButtonUp = true;
							}							
							break;
					}
					break;						
			}			
		}

		public bool GetThumbstickDown(XR_HAND hand)
		{
			bool lThumbstickButtonDown = _lThumbstickButtonDown;
			bool rThumbstickButtonDown = _rThumbstickButtonDown;

			switch (hand)
			{
				case XR_HAND.right:
					_rThumbstickButtonDown = false;
					return rThumbstickButtonDown;
				case XR_HAND.left:
					_lThumbstickButtonDown = false;
					return lThumbstickButtonDown;
				case XR_HAND.both:
					_rThumbstickButtonDown = false;
					_lThumbstickButtonDown = false;
					return rThumbstickButtonDown || lThumbstickButtonDown;
			}
			return false;
		}

		public bool GetThumbstickUp(XR_HAND hand)
		{
			bool lThumbstickButtonUp = _lThumbstickButtonUp;
			bool rThumbstickButtonUp = _rThumbstickButtonUp;

			switch (hand)
			{
				case XR_HAND.right:
					_rThumbstickButtonUp = false;
					return rThumbstickButtonUp;
				case XR_HAND.left:
					_lThumbstickButtonUp = false;
					return lThumbstickButtonUp;
				case XR_HAND.both:
					_rThumbstickButtonUp = false;
					_lThumbstickButtonUp = false;
					return rThumbstickButtonUp || lThumbstickButtonUp;
			}
			return false;
		}

		public bool GetThumbstick(XR_HAND hand)
		{
			switch (hand)
			{
				case XR_HAND.right:
					return _rThumbstickButtonState;
				case XR_HAND.left:
					return _lThumbstickButtonState;
				case XR_HAND.both:
					return _rThumbstickButtonState || _lThumbstickButtonState;
			}
			return false;
		}

		public bool GetIndexTriggerDown(XR_HAND hand, bool consume = true)
		{
			bool lTriggerButtonDown = _lTriggerButtonDown;
			bool rTriggerButtonDown = _rTriggerButtonDown;

			switch (hand)
			{
				case XR_HAND.right:
					if (consume) _rTriggerButtonDown = false;
					return rTriggerButtonDown;
				case XR_HAND.left:
					if (consume) _lTriggerButtonDown = false;
					return lTriggerButtonDown;
				case XR_HAND.both:
					if (consume) _rTriggerButtonDown = false;
					if (consume) _lTriggerButtonDown = false;
					return rTriggerButtonDown || lTriggerButtonDown;
			}
			return false;
		}

		public bool GetIndexTriggerUp(XR_HAND hand, bool consume = true)
		{
			bool lTriggerButtonUp = _lTriggerButtonUp;
			bool rTriggerButtonUp = _rTriggerButtonUp;

			switch (hand)
			{
				case XR_HAND.right:
					if (consume) _rTriggerButtonUp = false;
					return rTriggerButtonUp;
				case XR_HAND.left:
					if (consume) _lTriggerButtonUp = false;
					return lTriggerButtonUp;
				case XR_HAND.both:
					if (consume) _rTriggerButtonUp = false;
					if (consume) _lTriggerButtonUp = false;
					return rTriggerButtonUp || lTriggerButtonUp;
			}
			return false;
		}

		public bool GetIndexTrigger(XR_HAND hand)
		{
			switch (hand)
			{
				case XR_HAND.right:
					return _rTriggerButtonState;
				case XR_HAND.left:
					return _lTriggerButtonState;
				case XR_HAND.both:
					return _lTriggerButtonState || _rTriggerButtonState;
			}
			return false;
		}

		public bool GetHandTriggerDown(XR_HAND hand, bool consume = true)
		{
			bool lGripButtonDown = _lGripButtonDown;
			bool rGripButtonDown = _rGripButtonDown;

			switch (hand)
			{
				case XR_HAND.right:
					if (consume) _rGripButtonDown = false;
					return rGripButtonDown;
				case XR_HAND.left:
					if (consume) _lGripButtonDown = false;
					return lGripButtonDown;
				case XR_HAND.both:
					if (consume) _rGripButtonDown = false;
					if (consume) _lGripButtonDown = false;
					return rGripButtonDown || lGripButtonDown;
			}
			return false;
		}

		public bool GetHandTriggerUp(XR_HAND hand, bool consume = true)
		{
			bool lGripButtonUp = _lGripButtonUp;
			bool rGripButtonUp = _rGripButtonUp;

			switch (hand)
			{
				case XR_HAND.right:
					if (consume) _rGripButtonUp = false;
					return rGripButtonUp;
				case XR_HAND.left:
					if (consume) _lGripButtonUp = false;
					return lGripButtonUp;
				case XR_HAND.both:
					if (consume) _rGripButtonUp = false;
					if (consume) _lGripButtonUp = false;
					return rGripButtonUp || lGripButtonUp;
			}
			return false;
		}

		public bool GetHandTrigger(XR_HAND hand)
		{
			switch (hand)
			{
				case XR_HAND.right:
					return _rGripButtonState;
				case XR_HAND.left:
					return _lGripButtonState;
				case XR_HAND.both:
					return _lGripButtonState || _rGripButtonState;
			}
			return false;
		}

		public bool GetOneButtonDown(XR_HAND hand)
		{
			bool lPrimaryButtonDown = _lPrimaryButtonDown;
			bool rPrimaryButtonDown = _rPrimaryButtonDown;

			switch (hand)
			{
				case XR_HAND.right:
					_rPrimaryButtonDown = false;
					return rPrimaryButtonDown;
				case XR_HAND.left:
					_lPrimaryButtonDown = false;
					return lPrimaryButtonDown;
				case XR_HAND.both:
					_rPrimaryButtonDown = false;
					_lPrimaryButtonDown = false;
					return rPrimaryButtonDown || lPrimaryButtonDown;
			}
			return false;
		}

		public bool GetOneButtonUp(XR_HAND hand)
		{
			bool lPrimaryButtonUp = _lPrimaryButtonUp;
			bool rPrimaryButtonUp = _rPrimaryButtonUp;

			switch (hand)
			{
				case XR_HAND.right:
					_rPrimaryButtonUp = false;
					return rPrimaryButtonUp;
				case XR_HAND.left:
					_lPrimaryButtonUp = false;
					return lPrimaryButtonUp;
				case XR_HAND.both:
					_rPrimaryButtonUp = false;
					_lPrimaryButtonUp = false;
					return rPrimaryButtonUp || lPrimaryButtonUp;
			}
			return false;
		}

		public bool GetOneButton(XR_HAND hand)
		{
			switch (hand)
			{
				case XR_HAND.right:
					return _rPrimaryButtonState;
				case XR_HAND.left:
					return _lPrimaryButtonState;
				case XR_HAND.both:
					return _lPrimaryButtonState || _rPrimaryButtonState;
			}
			return false;
		}

		public bool GetTwoButtonDown(XR_HAND hand)
		{
			bool lSecondaryButtonDown = _lSecondaryButtonDown;
			bool rSecondaryButtonDown = _rSecondaryButtonDown;

			switch (hand)
			{
				case XR_HAND.right:
					_rSecondaryButtonDown = false;
					return rSecondaryButtonDown;
				case XR_HAND.left:
					_lSecondaryButtonDown = false;
					return lSecondaryButtonDown;
				case XR_HAND.both:
					_rSecondaryButtonDown = false;
					_lSecondaryButtonDown = false;
					return rSecondaryButtonDown || lSecondaryButtonDown;
			}
			return false;
		}

		public bool GetTwoButtonUp(XR_HAND hand)
		{
			bool lSecondaryButtonUp = _lSecondaryButtonUp;
			bool rSecondaryButtonUp = _rSecondaryButtonUp;

			switch (hand)
			{
				case XR_HAND.right:
					_rSecondaryButtonUp = false;
					return rSecondaryButtonUp;
				case XR_HAND.left:
					_lSecondaryButtonUp = false;
					return lSecondaryButtonUp;
				case XR_HAND.both:
					_rSecondaryButtonUp = false;
					_lSecondaryButtonUp = false;
					return rSecondaryButtonUp || lSecondaryButtonUp;
			}
			return false;
		}

		public bool GetTwoButton(XR_HAND hand)
		{
			switch (hand)
			{
				case XR_HAND.right:
					return _rSecondaryButtonState;
				case XR_HAND.left:
					return _lSecondaryButtonState;
				case XR_HAND.both:
					return _lSecondaryButtonState || _rSecondaryButtonState;
			}
			return false;
		}

        private void SetLaserToLeftHand()
        {
            if (_handSelected != XR_HAND.left)
            {
                _handSelected = XR_HAND.left;
				VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerChangedHandSide, _handSelected);
				SetMainLaserPoint(UltimateXRLeftRay, UltimateXRRightRay);
            }
        }

        private void SetLaserToRightHand()
        {
            if (_handSelected != XR_HAND.right)
            {
                _handSelected = XR_HAND.right;
				VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerChangedHandSide, _handSelected);
				SetMainLaserPoint(UltimateXRRightRay, UltimateXRLeftRay);
            }
        }

        private void SetMainLaserPoint(GameObject controller, GameObject other)
        {
			_currentController = controller;
			_otherController = other;
        }

		public void UpdateHandSideController()
        {
			if (_rPrimaryButtonDown)
			{
				SetLaserToRightHand();
			}
		
			if (_lPrimaryButtonDown)
			{
				SetLaserToLeftHand();                
			}
        }

		public void ResetState()
		{
			_rTriggerButtonDown = false; _lTriggerButtonDown = false; _rGripButtonDown = false; _lGripButtonDown = false; _rPrimaryButtonDown = false; _lPrimaryButtonDown = false; _rSecondaryButtonDown = false; _lSecondaryButtonDown = false;
			_rTriggerButtonUp = false; _lTriggerButtonUp = false; _rGripButtonUp = false; _lGripButtonUp = false; _rPrimaryButtonUp = false; _lPrimaryButtonUp = false; _rSecondaryButtonUp = false; _lSecondaryButtonUp = false;

			_rTriggerButtonState = false; _lTriggerButtonState = false; _rGripButtonState = false; _lGripButtonState = false; _rPrimaryButtonState = false; _lPrimaryButtonState = false; _rSecondaryButtonState = false; _lSecondaryButtonState = false;
			_rTriggerButtonPrevState = false; _lTriggerButtonPrevState = false; _rGripButtonPrevState = false; _lGripButtonPrevState = false; _rPrimaryButtonPrevState = false; _lPrimaryButtonPrevState = false; _rSecondaryButtonPrevState = false; _lSecondaryButtonPrevState = false;

			_rThumbstickButtonDown = false; _lThumbstickButtonDown = false;
			_rThumbstickButtonUp = false; _lThumbstickButtonUp = false;
		}

		void Update()
        {
			SetListeners();

			_rTriggerButtonPrevState = _rTriggerButtonState;
			_lTriggerButtonPrevState = _lTriggerButtonState;

			_rGripButtonPrevState = _rGripButtonState;
			_lGripButtonPrevState = _lGripButtonState;
			
			_rPrimaryButtonPrevState = _rPrimaryButtonState; 
			_lPrimaryButtonPrevState = _lPrimaryButtonState;
			
			_rSecondaryButtonPrevState = _rSecondaryButtonState;
			_lSecondaryButtonPrevState = _lSecondaryButtonState;

			_rThumbstickButtonPrevState = _rThumbstickButtonState;
			_lThumbstickButtonPrevState = _lThumbstickButtonState;

			if ((_controllerInput.IsControllerEnabled(UxrHandSide.Left) && _controllerInput.IsControllerEnabled(UxrHandSide.Right)))
			{
				if (!_controllersEnabled)
				{
					_controllersEnabled = true;
					VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerChangedHandTrackingState, _controllersEnabled);
				}
			}
			else
			{
				if (_controllersEnabled)
				{
					_controllersEnabled = false;
					VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerChangedHandTrackingState, _controllersEnabled);
				}				
			}			
        }

#endif		
	}
}

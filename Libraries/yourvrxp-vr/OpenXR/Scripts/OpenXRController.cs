using yourvrexperience.Utils;
using UnityEngine;
#if ENABLE_OPENXR
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
#endif

namespace yourvrexperience.VR
{
    public class OpenXRController : MonoBehaviour
#if ENABLE_OPENXR
, IVRController
#endif
    {
        private static OpenXRController instance;

        public static OpenXRController Instance
        {
            get
            {
                if (!instance)
                {
                    instance = GameObject.FindObjectOfType(typeof(OpenXRController)) as OpenXRController;
                }
                return instance;
            }
        }

        public Camera OpenXRCamera;
        public GameObject OpenXRLeftController;
        public GameObject OpenXRRightController;

#if ENABLE_OPENXR
        private XR_HAND _handSelected = XR_HAND.none;
        private GameObject _currentController;
		private GameObject _otherController;

        private LineRenderer _raycastLineLeft;
        private LineRenderer _raycastLineRight;

		private bool _rTriggerButtonDown = false, _lTriggerButtonDown = false, _rGripButtonDown = false, _lGripButtonDown = false, _rPrimaryButtonDown = false, _lPrimaryButtonDown = false, _rSecondaryButtonDown = false, _lSecondaryButtonDown = false;
		private bool _rTriggerButtonUp = false, _lTriggerButtonUp = false, _rGripButtonUp = false, _lGripButtonUp = false, _rPrimaryButtonUp = false, _lPrimaryButtonUp = false, _rSecondaryButtonUp = false, _lSecondaryButtonUp = false;

		private bool _rTriggerButtonState = false, _lTriggerButtonState = false, _rGripButtonState = false, _lGripButtonState = false, _rPrimaryButtonState = false, _lPrimaryButtonState = false, _rSecondaryButtonState = false, _lSecondaryButtonState = false;
		private bool _rTriggerButtonPrevState = false, _lTriggerButtonPrevState = false, _rGripButtonPrevState = false, _lGripButtonPrevState = false, _rPrimaryButtonPrevState = false, _lPrimaryButtonPrevState = false, _rSecondaryButtonPrevState = false, _lSecondaryButtonPrevState = false;

		public bool _rThumbstickButtonDown = false, _lThumbstickButtonDown = false;
		public bool _rThumbstickButtonUp = false, _lThumbstickButtonUp = false;

		public bool _rThumbstickButtonState = false, _lThumbstickButtonState = false;
		public bool _rThumbstickButtonPrevState = false, _lThumbstickButtonPrevState = false;

		private UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual _leftLineVisual;
		private UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual _rightLineVisual;

		private Camera _mainCamera;
		private XRUIInputModule[] _eventSystemOpenXR;
		private GameObject _interactionManager;
		private Vector3 _positionCollisionRaycasted;
		private	Vector3 _originLineLeft;
		private	Vector3 _targetLineLeft;
		private	Vector3 _originLineRight;
		private	Vector3 _targetLineRight;
		
		public Camera Camera
		{
			get { 
				if (_mainCamera == null)
				{
					_mainCamera = OpenXRCamera.GetComponentInChildren<Camera>();
				}
				return _mainCamera;
			}
		}
		public GameObject Container 
		{
			get { return this.gameObject; }
		}
        public GameObject HeadController
		{
			get { return OpenXRCamera.gameObject; }
		}
        public GameObject HandLeftController
		{
			get { return OpenXRLeftController; }
		}
		public GameObject HandRightController
		{
			get { return OpenXRRightController; }
		}

        public GameObject CurrentController
        {
            get { return _currentController; }
        }
 		public GameObject OtherController
        {
            get { return _otherController; }
        }		
        public XR_HAND HandSelected
        {
            get {  return _handSelected; }
        }
		public LineRenderer RaycastLineLeft
        {
            get {  return _raycastLineLeft; }
        }
        public LineRenderer RaycastLineRight
        {
            get { return _raycastLineRight; }
        }

        public Vector3 PositionCollisionRaycasted 
		{ 
			get { return _positionCollisionRaycasted; }
			set { _positionCollisionRaycasted = value; 

				Vector3 positionOriginRay = CurrentController.transform.position;
				float distanceToCollider = 	Vector3.Distance(_positionCollisionRaycasted, positionOriginRay);
				if (_handSelected == XR_HAND.right)
				{
					float originalDistanceRight = Vector3.Distance(_originLineRight, _targetLineRight);
					if (originalDistanceRight > distanceToCollider)
					{
						_raycastLineRight.SetPosition(1, new Vector3(0, 0, distanceToCollider));
					}
					else
					{
						_raycastLineRight.SetPosition(1, new Vector3(0, 0, originalDistanceRight));
					}
				}
				else
				{
					float originalDistanceLeft = Vector3.Distance(_originLineLeft, _targetLineLeft);
					if (originalDistanceLeft > distanceToCollider)
					{
						_raycastLineLeft.SetPosition(1, new Vector3(0, 0, distanceToCollider));
					}
					else
					{
						_raycastLineLeft.SetPosition(1, new Vector3(0, 0, originalDistanceLeft));
					}
				}
			}
		}
        public bool HandTrackingActive 
		{
			get { return false; }			
		}

        private void Start()
        {
			_raycastLineLeft = OpenXRLeftController.GetComponentInChildren<LineRenderer>();
			_raycastLineRight = OpenXRRightController.GetComponentInChildren<LineRenderer>();

			_originLineLeft = _raycastLineLeft.GetPosition(0);
			_targetLineLeft = _raycastLineLeft.GetPosition(1);
			
			_originLineRight = _raycastLineRight.GetPosition(0);
			_targetLineRight = _raycastLineRight.GetPosition(1);

			_leftLineVisual = OpenXRLeftController.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>();
			_rightLineVisual = OpenXRRightController.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>();

			SetLaserToRightHand();
			
			SystemEventController.Instance.Event += OnSystemEvent;
			VRInputController.Instance.Event += OnVREvent;
        }

        void OnDestroy()
        {
			DestroyXRUIResources();
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (VRInputController.Instance != null) VRInputController.Instance.Event -= OnVREvent;
        }

		private void DestroyXRUIResources()
		{
			if (_eventSystemOpenXR != null)
			{
				foreach (XRUIInputModule xrInput in _eventSystemOpenXR)
				{
					if (xrInput != null)
					{
						GameObject.Destroy(xrInput.gameObject);
					}
				}
				_eventSystemOpenXR = null;
			}
			if (_interactionManager != null)
			{
				GameObject.Destroy(_interactionManager);
				_interactionManager = null;
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))
			{
				if (Instance)
				{
					DontDestroyOnLoad(Instance.gameObject);
					if (_eventSystemOpenXR == null)
					{
						_eventSystemOpenXR = GameObject.FindObjectsOfType<XRUIInputModule>();
						foreach (XRUIInputModule xrInput in _eventSystemOpenXR)
						{
							if (xrInput != null)
							{
								DontDestroyOnLoad(xrInput.gameObject);
							}
						}
					}
					if (_interactionManager == null)
					{
						_interactionManager = GameObject.FindObjectOfType<XRInteractionManager>().gameObject;
						DontDestroyOnLoad(_interactionManager);
					}
				}
			}
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerReleaseAllResources))
			{
				DestroyXRUIResources();
			}
		}		

		private void OnVREvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(VRInputController.EventVRInputControllerAssignCurrentPointer))
			{
				SetLaserToRightHand();
			}
		}

        private void DisableRays()
        {
            if (_leftLineVisual != null) _leftLineVisual.enabled = false;
            if (_rightLineVisual != null) _rightLineVisual.enabled = false;
        }

        public void Deactivate()
        {
            if (OpenXRLeftController != null) OpenXRLeftController.SetActive(false);
            if (OpenXRRightController != null) OpenXRRightController.SetActive(false);

            if (_leftLineVisual != null) _leftLineVisual.enabled = false;
            if (_rightLineVisual != null) _rightLineVisual.enabled = false;
        }

        public void Activate()
        {
            OpenXRLeftController.SetActive(true);
            OpenXRRightController.SetActive(true);

            if (_leftLineVisual != null) _leftLineVisual.enabled = false;
            if (_rightLineVisual != null) _rightLineVisual.enabled = false;
        }

        private void SetLaserToLeftHand()
        {
            if (_handSelected != XR_HAND.left)
            {
                _handSelected = XR_HAND.left;
				VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerChangedHandSide, _handSelected);
                if (_leftLineVisual != null)
                {
                    if (_rightLineVisual != null) _rightLineVisual.enabled = false;
                    if (_leftLineVisual != null) _leftLineVisual.enabled = true;
                }
				SetMainLaserPoint(OpenXRLeftController, OpenXRRightController);
            }
        }

        private void SetLaserToRightHand()
        {
            if (_handSelected != XR_HAND.right)
            {
                _handSelected = XR_HAND.right;
				VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerChangedHandSide, _handSelected);
                if (_rightLineVisual != null)
                {
					if (_leftLineVisual != null) _leftLineVisual.enabled = false;
                    if (_rightLineVisual != null) _rightLineVisual.enabled = true;
                }
                SetMainLaserPoint(OpenXRRightController, OpenXRLeftController);
            }
        }

        private void SetMainLaserPoint(GameObject controller, GameObject other)
        {
			_currentController = controller;
			_otherController = other;
        }

		public Vector2 GetVector2Joystick(XR_HAND hand)
		{
			Vector2 rAxisJoystick = Vector2.zero, lAxisJoystick = Vector2.zero;
			switch (hand)
			{
				case XR_HAND.right:
					InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.primary2DAxis, out rAxisJoystick);
					break;
				case XR_HAND.left:
					InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.primary2DAxis, out lAxisJoystick);
					break;
				case XR_HAND.both:
					InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.primary2DAxis, out rAxisJoystick);
					InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.primary2DAxis, out lAxisJoystick);
					break;
			}
			
			return rAxisJoystick + lAxisJoystick;
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

		public void UpdateHandSideController()
        {
			bool rTriggerButton, lTriggerButton;
            if (InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.triggerButton, out rTriggerButton))
            {
				if (rTriggerButton)
				{
					SetLaserToRightHand();
				}
            }
            else if (InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.triggerButton, out lTriggerButton))
            {
				if (lTriggerButton)
				{
					SetLaserToLeftHand();                
				}
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
			UpdateHandSideController();
			
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

			if (InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.triggerButton, out _rTriggerButtonState))
			{
				if (_rTriggerButtonPrevState != _rTriggerButtonState)
				{
					_rTriggerButtonDown = _rTriggerButtonState;
					_rTriggerButtonUp = !_rTriggerButtonState;
				}
				if (_rTriggerButtonState) 
				{
					SetLaserToRightHand();
				}
			}
			if (InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.triggerButton, out _lTriggerButtonState))
			{
				if (_lTriggerButtonPrevState != _lTriggerButtonState)
				{
					_lTriggerButtonDown = _lTriggerButtonState;
					_lTriggerButtonUp = !_lTriggerButtonState;
				}
				if (_lTriggerButtonState)
				{
					SetLaserToLeftHand();
				}				
			}
			if (InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.gripButton, out _rGripButtonState))
			{				
				if (_rGripButtonPrevState != _rGripButtonState)
				{
					_rGripButtonDown = _rGripButtonState;
					_rGripButtonUp = !_rGripButtonState;
				}
				if (_rGripButtonState)
				{
					SetLaserToRightHand();
				}
			}
			if (InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.gripButton, out _lGripButtonState))
			{
				if (_lGripButtonPrevState != _lGripButtonState)
				{
					_lGripButtonDown = _lGripButtonState;
					_lGripButtonUp = !_lGripButtonState;
				}
				if (_lGripButtonState)
				{
					SetLaserToLeftHand();
				}				
			}
			if (InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.primaryButton, out _rPrimaryButtonState))
			{
				if (_rPrimaryButtonPrevState != _rPrimaryButtonState)
				{
					_rPrimaryButtonDown = _rPrimaryButtonState;
					_rPrimaryButtonUp = !_rPrimaryButtonState;
				}
			}
			if (InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.primaryButton, out _lPrimaryButtonState))
			{
				if (_lPrimaryButtonPrevState != _lPrimaryButtonState)
				{
					_lPrimaryButtonDown = _lPrimaryButtonState;
					_lPrimaryButtonUp = !_lPrimaryButtonState;
				}
			}
			if (InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.secondaryButton, out _rSecondaryButtonState))
			{
				if (_rPrimaryButtonPrevState != _rSecondaryButtonState)
				{
					_rPrimaryButtonDown = _rSecondaryButtonState;
					_rPrimaryButtonUp = !_rSecondaryButtonState;
				}
			}
			if (InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.secondaryButton, out _lSecondaryButtonState))
			{
				if (_lSecondaryButtonPrevState != _lSecondaryButtonState)
				{
					_lSecondaryButtonDown = _lSecondaryButtonState;
					_lSecondaryButtonUp = !_lSecondaryButtonState;
				}
			}
			if (InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.primary2DAxisClick, out _rThumbstickButtonState))
			{
				if (_rThumbstickButtonPrevState != _rThumbstickButtonState)
				{
					_rThumbstickButtonDown = _rThumbstickButtonState;
					_rThumbstickButtonUp = !_rThumbstickButtonState;
				}
			}
			if (InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.primary2DAxisClick, out _lThumbstickButtonState))
			{
				if (_lThumbstickButtonPrevState != _lThumbstickButtonState)
				{
					_lThumbstickButtonDown = _lThumbstickButtonState;
					_lThumbstickButtonUp = !_lThumbstickButtonState;
				}
			}
        }
#endif
	}
}


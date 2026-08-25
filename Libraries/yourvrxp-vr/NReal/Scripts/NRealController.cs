using yourvrexperience.Utils;
using UnityEngine;
using System;
#if ENABLE_NREAL
using NRKernal;
#endif

namespace yourvrexperience.VR
{
#if ENABLE_NREAL	
	[RequireComponent(typeof(NRHMDPoseTracker))] 
#endif	
    public class NRealController : MonoBehaviour
#if ENABLE_NREAL
, IVRController
#endif
    {
        private static NRealController instance;

        public static NRealController Instance
        {
            get
            {
                if (!instance)
                {
                    instance = GameObject.FindObjectOfType(typeof(NRealController)) as NRealController;
                }
                return instance;
            }
        }

#if ENABLE_NREAL
		private Camera _centerCamera;
        private GameObject _currentController;
		private GameObject _rigthController;
		private GameObject _leftController;
		private LineRenderer _raycastLineRight;
		private LineRenderer _raycastLineLeft;
		private XR_HAND _handSelected = XR_HAND.none;
		private bool _rTriggerButtonDown = false;
		private bool _lTriggerButtonDown = false;
		private bool _rPrimaryButtonDown = false;
		private bool _inited = false;
		
		public Camera Camera
		{
			get {  
				if (_centerCamera == null)
				{
					_centerCamera = this.GetComponent<NRHMDPoseTracker>().centerCamera;
				}
				return _centerCamera;
			}
		}
		public GameObject Container 
		{
			get { return this.gameObject; }
		}
        public GameObject HeadController
		{
			get { return _centerCamera.transform.parent.gameObject; }
		}
        public GameObject HandLeftController
		{
			get { return _leftController; }
		}
		public GameObject HandRightController
		{
			get { return _rigthController; }
		}
        public GameObject CurrentController
        {
            get { return _currentController; }
        }
 		public GameObject OtherController
        {
            get { return null; }
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
			get { return Vector3.zero; }
			set { }
		}
        public bool HandTrackingActive
		{
			get { return false; }			
		}
        private void Start()
        {
			InitializeControls();

			SystemEventController.Instance.Event += OnSystemEvent;
			VRInputController.Instance.Event += OnVREvent;

            NRInput.AddClickListener(ControllerHandEnum.Right, ControllerButton.HOME, OnHomeButtonClick);
            NRInput.AddClickListener(ControllerHandEnum.Left, ControllerButton.HOME, OnHomeButtonClick);
            NRInput.AddClickListener(ControllerHandEnum.Right, ControllerButton.APP, OnAppButtonClickRight);
            NRInput.AddClickListener(ControllerHandEnum.Left, ControllerButton.APP, OnAppButtonClickLeft);
        }

		private void InitializeControls()
		{
			if (!_inited)
			{
				ControllerTracker[] controllers = GameObject.FindObjectsOfType<ControllerTracker>();
				if ((controllers != null) && (controllers.Length > 0))
				{
					for (int i = 0; i < controllers.Length; i++)
					{
						ControllerTracker controller = controllers[i];
						if (controller.defaultHandEnum == ControllerHandEnum.Right)
						{
							_rigthController = controller.gameObject;
							NRLaserVisual laserRight = controller.gameObject.GetComponent<NRLaserVisual>();
							if (laserRight != null)
							{
								_raycastLineRight = laserRight.gameObject.GetComponent<LineRenderer>();
							}
						}
						else
						{
							_leftController = controller.gameObject;
							NRLaserVisual laserLeft = controller.gameObject.GetComponent<NRLaserVisual>();
							if (laserLeft != null)
							{
								_raycastLineLeft = laserLeft.gameObject.GetComponent<LineRenderer>();
							}
						}
					}
				}
				if ((_rigthController != null) && (_raycastLineRight != null))
				{
					_inited = true;
					SetLaserToRightHand();
				}
				else
				{
					if ((_leftController != null) && (_raycastLineLeft != null))
					{
						_inited = true;
						SetLaserToLeftHand();
					}
				}
			}
		}

        void OnDestroy()
        {
			DestroyNRealResources();
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (VRInputController.Instance != null) VRInputController.Instance.Event -= OnVREvent;
            NRInput.RemoveClickListener(ControllerHandEnum.Right, ControllerButton.HOME, OnHomeButtonClick);
            NRInput.RemoveClickListener(ControllerHandEnum.Left, ControllerButton.HOME, OnHomeButtonClick);
            NRInput.RemoveClickListener(ControllerHandEnum.Right, ControllerButton.APP, OnAppButtonClickRight);
            NRInput.RemoveClickListener(ControllerHandEnum.Left, ControllerButton.APP, OnAppButtonClickLeft);
        }

        private void DestroyNRealResources()
		{

		}

        private void OnAppButtonClickRight()
        {
            _rTriggerButtonDown = true;		
			SetLaserToRightHand();
        }
        private void OnAppButtonClickLeft()
        {
            _lTriggerButtonDown = true;		
			SetLaserToLeftHand();
        }

        private void OnHomeButtonClick()
        {
            _rPrimaryButtonDown = true;
        }

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))
			{
				if (Instance)
				{
					DontDestroyOnLoad(Instance.gameObject);
				}
			}
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerReleaseAllResources))
			{
				DestroyNRealResources();
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
        }

        public void Deactivate()
        {
        }

        public void Activate()
        {
        }

        private void SetLaserToLeftHand()
        {
 			if (_handSelected != XR_HAND.left)
            {
                _handSelected = XR_HAND.left;
				VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerChangedHandSide, _handSelected);
                if (_raycastLineLeft != null)
                {
					if (_raycastLineLeft != null) _raycastLineLeft.enabled = true;
                    if (_raycastLineRight != null) _raycastLineRight.enabled = false;
                }
                SetMainLaserPoint(_leftController, _rigthController);
            }
        }

        private void SetLaserToRightHand()
        {
 			if (_handSelected != XR_HAND.right)
            {
                _handSelected = XR_HAND.right;
				VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerChangedHandSide, _handSelected);
                if (_raycastLineRight != null)
                {
					if (_raycastLineLeft != null) _raycastLineLeft.enabled = false;
                    if (_raycastLineRight != null) _raycastLineRight.enabled = true;
                }
                SetMainLaserPoint(_rigthController, _leftController);
            }
        }

        private void SetMainLaserPoint(GameObject controller, GameObject other)
        {
			_currentController = controller;
        }

		public Vector2 GetVector2Joystick(XR_HAND hand)
		{
			return Vector2.zero;
		}

		public bool GetThumbstickDown(XR_HAND hand)
		{
			return false;
		}

		public bool GetThumbstickUp(XR_HAND hand)
		{
			return false;
		}

		public bool GetThumbstick(XR_HAND hand)
		{
			return false;
		}

		public bool GetIndexTriggerDown(XR_HAND hand, bool consume = true)
		{
			bool triggerButtonDown = _rTriggerButtonDown || _lTriggerButtonDown;
			_rTriggerButtonDown = false;
			_lTriggerButtonDown = false;
			return triggerButtonDown;
		}

		public bool GetIndexTriggerUp(XR_HAND hand, bool consume = true)
		{
			return false;
		}

		public bool GetIndexTrigger(XR_HAND hand)
		{
			return false;
		}

		public bool GetHandTriggerDown(XR_HAND hand, bool consume = true)
		{
			return false;
		}

		public bool GetHandTriggerUp(XR_HAND hand, bool consume = true)
		{
			return false;
		}

		public bool GetHandTrigger(XR_HAND hand)
		{			
			return false;
		}

		public bool GetOneButtonDown(XR_HAND hand)
		{
			bool rPrimaryButtonDown = _rPrimaryButtonDown;
			return rPrimaryButtonDown;
		}

		public bool GetOneButtonUp(XR_HAND hand)
		{
			return false;
		}

		public bool GetOneButton(XR_HAND hand)
		{
			return false;
		}

		public bool GetTwoButtonDown(XR_HAND hand)
		{
			return false;
		}

		public bool GetTwoButtonUp(XR_HAND hand)
		{
			return false;
		}

		public bool GetTwoButton(XR_HAND hand)
		{
			return false;
		}

		public void UpdateHandSideController()
        {
        }

		public void ResetState()
		{
			_rTriggerButtonDown = false; 
			_rPrimaryButtonDown = false; 
		}

        void Update()
        {
			InitializeControls();
			UpdateHandSideController();
        }
#endif
	}
}


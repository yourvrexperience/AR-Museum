using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace yourvrexperience.Utils
{
    public class InputController : MonoBehaviour, IInputController
    {
        public const string EventInputControllerEnableMobileHUD = "EventInputControllerEnableMobileHUD";
        public const string EventInputControllerHasStarted = "EventInputControllerHasStarted";

        private Camera _camera;
        private bool _isVR = false;
        private Transform _rayPointerVR;
        private MobileInputManager _mobileJoysticks;
#if ENABLE_MOBILE
        private bool _triggeredCameraChange = false;
        private bool _triggeredGrip = false;
        private bool _triggeredAction = false;
		private bool _triggeredMenu = false;
#endif

        public virtual bool IsVR
        {
            get { return _isVR; }
        }
		public GameObject Content 
		{ 
			get { return this.gameObject; }
		}

        public virtual Transform RayPointerVR
        {
            get
            {
                return _rayPointerVR;
            }
        }
        
		public virtual Camera Camera
		{
			get { return _camera; }
            set { 
                if (_camera != value)
                {
                    _camera.tag = "Untagged";
                    _camera.enabled = false;
                }
                _camera = value;
                _camera.tag = "MainCamera";
            }
		}

        public float SpeedJoystickMovement 
        { 
            get { return -1; }
            set {}
        }

        void Start()
        {
            _camera = Camera.main;
            SystemEventController.Instance.DispatchSystemEvent(EventInputControllerHasStarted, this.gameObject);
        }

        public virtual void Initialize()
        {
#if ENABLE_MOBILE
        _mobileJoysticks = Instantiate(Resources.Load("Mobile/MobileHUD") as GameObject).GetComponent<MobileInputManager>();
        _mobileJoysticks.PrimaryActionEvent += OnMobilePrimaryActionEvent;
        _mobileJoysticks.SecondaryActionEvent += OnMobileSecondaryActionEvent;
		_mobileJoysticks.MenuActionEvent += OnMobileMenuActionEvent;
        _mobileJoysticks.CameraEvent += OnMobileCameraEvent;
#endif
            SystemEventController.Instance.Event += OnSystemEvent;
        }

        public virtual void OnDestroy()
        {
#if ENABLE_MOBILE
        if (_mobileJoysticks != null)
        {
            _mobileJoysticks.PrimaryActionEvent -= OnMobilePrimaryActionEvent;
            _mobileJoysticks.SecondaryActionEvent -= OnMobileSecondaryActionEvent;
			_mobileJoysticks.MenuActionEvent -= OnMobileMenuActionEvent;
            _mobileJoysticks.CameraEvent -= OnMobileCameraEvent;
        }
#endif
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
        }

        protected virtual void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(EventInputControllerEnableMobileHUD))
            {
                bool activate = (bool)parameters[0];
                if (_mobileJoysticks != null)
                {
                    _mobileJoysticks.gameObject.SetActive(activate);
                }
            }
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))
			{
				DontDestroyOnLoad(this.gameObject);
			}
        }

        public virtual bool EnableMouseRotation()
        {
#if ENABLE_MOBILE
        return false;
#else
            return true;
#endif
        }

#if ENABLE_MOBILE
        private void OnMobileCameraEvent()
        {
            _triggeredCameraChange = true;
        }
        private void OnMobileSecondaryActionEvent()
        {
            _triggeredGrip = true;
        }
        private void OnMobilePrimaryActionEvent()
        {
            _triggeredAction = true;
        }
		private void OnMobileMenuActionEvent()
        {
            _triggeredMenu = true;
        }
#endif

        public virtual bool IsMoving()
        {
            Vector2 joystick = GetMovementJoystick();
			InGameDebugInfo.Instance?.SetText("Axis=" + joystick.ToString());
            if (joystick.x != 0) return true;
            if (joystick.y != 0) return true;

            return false;
        }

        public float GetAxisHorizontal()
        {
            Vector2 joystick = GetMovementJoystick();
            return joystick.x;
        }

        public float GetAxisVertical()
        {
            Vector2 joystick = GetMovementJoystick();
            return joystick.y;
        }

        public float GetMouseAxisHorizontal()
        {
            Vector2 rotationJoystick = GetRotationJoystick();
            return rotationJoystick.x;
        }
       
        public float GetMouseAxisVertical()
        {
            Vector2 rotationJoystick = GetRotationJoystick();
            return rotationJoystick.y;
        }

        public virtual bool ActionMenuPressed()
        {
#if ENABLE_MOBILE
        if (_triggeredMenu)
        {
            _triggeredMenu = false;
            return true;
        }
        else
        {
            return false;
        }
#else
            return Input.GetKeyDown(KeyCode.P);
#endif
        }



        public virtual bool SwitchedCameraPressed()
        {
#if ENABLE_MOBILE
        if (_triggeredCameraChange)
        {
            _triggeredCameraChange = false;
            return true;
        }
        else
        {
            return false;
        }
#else
            return Input.GetKeyDown(KeyCode.Y);
#endif
        }


        public virtual Vector2 GetMovementJoystick()
        {
#if ENABLE_MOBILE
        return new Vector2(_mobileJoysticks.MoveJoystick.Horizontal, _mobileJoysticks.MoveJoystick.Vertical);
#else
            return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
#endif
        }

        private Vector2 GetRotationJoystick()
        {
#if ENABLE_MOBILE
        return new Vector2(_mobileJoysticks.RotateJoystick.Horizontal / 25, _mobileJoysticks.RotateJoystick.Vertical / 25);
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
        }

        public virtual bool ActionPrimaryDown()
        {
#if ENABLE_MOBILE
        if (_triggeredAction)
        {
            _triggeredAction = false;
            return true;
        }
        else
        {
            return false;
        }
#else
            return Input.GetButtonDown("Fire1") || Input.GetMouseButtonDown(0);
#endif
        }

        public virtual bool ActionPrimaryUp()
        {
#if ENABLE_MOBILE
            return false;
#else
            return Input.GetButtonUp("Fire1") || Input.GetMouseButtonUp(0);
#endif
        }

		public virtual bool ActionPrimary()
		{
			return Input.GetButton("Fire1") || Input.GetMouseButton(0);
 		}

		public virtual bool ActionSecondaryDown()
        {
#if ENABLE_MOBILE
			if (_triggeredGrip)
			{
				_triggeredGrip = false;
				return true;
			}
			else
			{
				return false;
			}
#else
            return Input.GetButtonDown("Fire2") || Input.GetKeyDown(KeyCode.Space);
#endif
        }

		public virtual bool ActionSecondaryUp()
        {
#if ENABLE_MOBILE
			return false;
#else
            return Input.GetButtonUp("Fire2") || Input.GetKeyUp(KeyCode.Space);
#endif
        }

		public virtual bool ActionSecondary()
		{
			return Input.GetButton("Fire2") || Input.GetKey(KeyCode.Space);
 		}

		public virtual void SetInitialPosition(Vector3 position, Quaternion rotation)
		{
			Camera.transform.position = position;
			Camera.transform.rotation = rotation;
		}
	}
}
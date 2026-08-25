using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
using yourvrexperience.VR;
#endif

namespace yourvrexperience.Utils
{
    public class CameraController : StateMachine
    {
        public const string EventCameraSwitchedTo1stPerson = "EventCameraSwitchedTo1stPerson";
        public const string EventCameraSwitchedTo3rdPerson = "EventCameraSwitchedTo3rdPerson";
        public const string EventCameraSwitchedToFreeCamera = "EventCameraSwitchedToFreeCamera";
        
        public const string EventCameraPlayerReadyForCamera = "EventCameraPlayerReadyForCamera";
        public const string EventCameraPlayerUnlinkCameraFromPlayer = "EventCameraPlayerUnlinkCameraFromPlayer";

        public enum CameraStates { Camera1stPerson = 0, Camera3rdPerson, CameraFrozen, CameraFree }

        public const float SpeedRotation = 10f;

        private static CameraController _instance;
        public static CameraController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType<CameraController>();
                }
                return _instance;
            }
        }

        public Vector3 Offset = new Vector3(0, 3, 5);
        public float Speed = 20;

        protected ICameraPlayer _player;
        protected Camera _gameCamera;
        protected GameObject _XRRig;

		public float Sensitivity = 7F;
        private float _rotationY = 0F;

        public Camera GameCamera
        {
            get
            {
                if (_gameCamera == null)
                {
                    _gameCamera = Camera.main;
                    if (_gameCamera == null)
                    {
                        CameraFinder cameraFinder = GameObject.FindObjectOfType<CameraFinder>();
                        if (cameraFinder != null)
                        {
                            _gameCamera = cameraFinder.MainCamera;
                        }
                        
                    }
                }
                return _gameCamera;
            }
        }

        public GameObject ContainerCamera
        {
            get
            {
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
				if (InputControls.IsVR)
				{
					return VRInputController.Instance.CameraGO;
				}
				else
				{
					return GameCamera.gameObject;
				}
				
#else
                return GameCamera.gameObject;
#endif
            }
        }
        protected IInputController _inputControls;
        public IInputController InputControls
        {
            get { return _inputControls; }
        }

        void Awake()
        {
            _state = (int)CameraStates.Camera1stPerson;
        }

		void Start()
		{
            SystemEventController.Instance.Event += OnSystemEvent;
		}

        void OnDestroy()
        {
			Destroy();
        }

		private void Destroy()
		{
			if (_instance != null)
			{
				_instance = null;
				_inputControls = null;
				if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;

				GameObject.Destroy(this.gameObject);
			}
		}

		private void LinkAvatarWithVRCamera()
		{
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			if ((_player != null) && (_inputControls != null))
			{
				VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerLinkWithAvatar, _player.GetGameObject());
			}					
#endif				
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerReleaseAllResources))
            {
                Destroy();
            }
            if (nameEvent.Equals(InputController.EventInputControllerHasStarted))
            {
                _inputControls = ((GameObject)parameters[0]).GetComponent<IInputController>();
				LinkAvatarWithVRCamera();
            }
            if (nameEvent.Equals(EventCameraPlayerReadyForCamera))
            {
                ICameraPlayer player = (ICameraPlayer)parameters[0];
                if (player.IsOwner())
                {
                    _player = player;
					LinkAvatarWithVRCamera();
                }
            }
            if (nameEvent.Equals(EventCameraPlayerUnlinkCameraFromPlayer))
            {                
                _player = null;
            }
        }

        public void SetCameraTo1stPerson()
        {
            ChangeState((int)CameraStates.Camera1stPerson);
        }

        protected virtual bool SwitchCameraState()
        {
            bool changed = false;
            if (_inputControls.SwitchedCameraPressed())
            {
                changed = true;
                switch ((CameraStates)_state)
                {
                    case CameraStates.Camera1stPerson:
                        ChangeState((int)CameraStates.Camera3rdPerson);
                        break;

                    case CameraStates.Camera3rdPerson:
                        ChangeState((int)CameraStates.CameraFree);
                        break;

                    case CameraStates.CameraFree:
                        ChangeState((int)CameraStates.Camera1stPerson);
                        break;
                }
            }
            return changed;
        }

        public bool IsFirstPersonCamera()
        {
            return _state == (int)CameraStates.Camera1stPerson;
        }

        protected void CameraFollowAvatar()
        {
            Offset = Quaternion.AngleAxis(_inputControls.GetMouseAxisHorizontal() * SpeedRotation, Vector3.up) * Offset;
            if (_player != null)
            {
                GameCamera.transform.position = _player.GetGameObject().transform.position + Offset;
                GameCamera.transform.forward = (_player.GetGameObject().transform.position - GameCamera.transform.position).normalized;
            }
        }

        public void FreezeCamera(bool _activateFreeze)
        {
            if (_activateFreeze)
            {
                ChangeState((int)CameraStates.CameraFrozen);
            }
            else
            {
                RestorePreviousState();
            }
        }

        private void MoveCameraFree()
        {
            float axisVertical = _inputControls.GetAxisVertical();
            float axisHorizontal = _inputControls.GetAxisHorizontal();
            Vector3 forward = axisVertical * GameCamera.transform.forward * Speed * Time.deltaTime;
            Vector3 lateral = axisHorizontal * GameCamera.transform.right * Speed * Time.deltaTime;
            GameCamera.transform.position += forward + lateral;
        }

        private void RotateCameraFree()
        {
            float rotationX = GameCamera.transform.localEulerAngles.y + _inputControls.GetMouseAxisHorizontal() * Sensitivity;
            _rotationY = _rotationY + _inputControls.GetMouseAxisVertical() * Sensitivity;
            _rotationY = Mathf.Clamp(_rotationY, -60, 60);
            Quaternion rotation = Quaternion.Euler(-_rotationY, rotationX, 0);
            GameCamera.transform.forward = rotation * Vector3.forward;
        }

        protected override void ChangeState(int newState)
        {
            base.ChangeState(newState);

            switch ((CameraStates)_state)
            {
                case CameraStates.Camera1stPerson:
                    SystemEventController.Instance.DispatchSystemEvent(EventCameraSwitchedTo1stPerson);
                    break;

                case CameraStates.Camera3rdPerson:
                    SystemEventController.Instance.DispatchSystemEvent(EventCameraSwitchedTo3rdPerson);
                    break;

                case CameraStates.CameraFree:
                    SystemEventController.Instance.DispatchSystemEvent(EventCameraSwitchedToFreeCamera);
                    break;

                case CameraStates.CameraFrozen:
                    break;
            }
        }

        protected virtual void Update()
        {
            if ((_player == null) || (_inputControls == null)) return;

            switch ((CameraStates)_state)
            {
                case CameraStates.Camera1stPerson:
                    SwitchCameraState();
                    if (_player != null)
                    {
                        if (!_inputControls.IsVR)
                        {
							ContainerCamera.transform.position = _player.PositionCamera;
                            GameCamera.transform.forward = _player.ForwardCamera;
                        }
                    }
                    break;

                case CameraStates.Camera3rdPerson:
                    SwitchCameraState();
                    CameraFollowAvatar();
                    break;

                case CameraStates.CameraFree:
                    if (SwitchCameraState()) return;
                    bool shouldRun = Input.GetKey(KeyCode.LeftShift);
#if ENABLE_MOBILE
                shouldRun = true;
#endif
                    if (shouldRun)
                    {
                        MoveCameraFree();
                        RotateCameraFree();
                    }
                    break;

                case CameraStates.CameraFrozen:
                    break;
            }
        }
    }
}
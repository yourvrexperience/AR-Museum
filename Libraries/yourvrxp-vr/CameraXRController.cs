using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
#if ENABLE_NREAL
using NRKernal;
#endif
#if ENABLE_OCULUS
using Oculus.Interaction;
#endif

namespace yourvrexperience.VR
{
	public class CameraXRController : StateMachine
    {
        public const string EventCameraSwitchedTo1stPerson = "EventCameraSwitchedTo1stPerson";
        public const string EventCameraSwitchedTo3rdPerson = "EventCameraSwitchedTo3rdPerson";
        public const string EventCameraSwitchedToFreeCamera = "EventCameraSwitchedToFreeCamera";
        
        public const string EventCameraPlayerReadyForCamera = "EventCameraPlayerReadyForCamera";
        public const string EventCameraResponseToPlayer = "EventCameraResponseToPlayer";

        public enum CameraStates { Camera1stPerson = 0, Camera3rdPerson, CameraFrozen, CameraFree }

        public const float SpeedRotation = 10f;

        private static CameraXRController _instance;
        public static CameraXRController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType<CameraXRController>();
                }
                return _instance;
            }
        }

		[SerializeField] private GameObject CameraDesktop;
		[SerializeField] private GameObject EventSystemDesktop;
		[SerializeField] private GameObject CameraOculus;
		[SerializeField] private GameObject GazePointerOculus;
		[SerializeField] private GameObject HandManagerOculus;
		[SerializeField] private GameObject EventSystemOculus;
		[SerializeField] private GameObject CameraOpenXR;
		[SerializeField] private GameObject EventSystemOpenXR;
		[SerializeField] private GameObject CameraUltimateXR;
		[SerializeField] private GameObject EventSystemUltimateXR;
		[SerializeField] private GameObject NRealCameraXR;
		[SerializeField] private GameObject NRealInputXR;
        [SerializeField] private GameObject EventSystemNReal;

		[SerializeField] private GameObject DesktopInputControllerPrefab;
		[SerializeField] private GameObject VRInputControllerPrefab;

		[SerializeField] private Vector3 Offset = new Vector3(0, 3, 5);
        [SerializeField] private float Speed = 20;
        [SerializeField] private bool EnableCameraSwitch = false;

        protected ICameraPlayer _player;
        protected Camera _gameCamera;
        protected GameObject _XRRig;

		public float Sensitivity = 7F;
        private float _rotationY = 0F;

#if ENABLE_OCULUS 
		private OVRGazePointer _gazePointer;
		private OVRInputModule _inputModule;
		private OculusHandsManager _handsManager;
#elif ENABLE_OPENXR			
		private GameObject _eventSystemOpenXR;
#elif ENABLE_ULTIMATEXR					
		private GameObject _avatarUltimateXR;
		private GameObject _eventSystemUltimateXR;
#elif ENABLE_NREAL
		private GameObject _cameraNRealXR;
        private GameObject _inputNRealXR;
		private GameObject _eventSystemNRealXR;
#else
		private GameObject _cameraDesktop;
		private GameObject _eventSystemDesktop;
#endif

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

		private bool _instantiated = false;

        public void Initialize()
        {
#if ENABLE_OCULUS
			if ((GameObject.FindFirstObjectByType<OVRCameraRig>() == null) && (CameraOculus != null))
			{
				if (!_instantiated)
				{
					_instantiated = true;
					Instantiate(CameraOculus);
					_gazePointer = Instantiate(GazePointerOculus).GetComponent<OVRGazePointer>();
					_inputModule = Instantiate(EventSystemOculus).GetComponent<OVRInputModule>();
					_inputModule.rayTransform = new GameObject().transform;
					_handsManager = Instantiate(HandManagerOculus).GetComponent<OculusHandsManager>();
					_inputModule.m_Cursor = _gazePointer;
					Instantiate(VRInputControllerPrefab);
				}
			}
#elif ENABLE_OPENXR
			if ((GameObject.FindFirstObjectByType<OpenXRController>() == null) && (CameraOpenXR != null))
			{
				if (!_instantiated)
				{
					_instantiated = true;
					Instantiate(CameraOpenXR);
					_eventSystemOpenXR = Instantiate(EventSystemOpenXR) as GameObject;
					Instantiate(VRInputControllerPrefab);
				}
			}
#elif ENABLE_ULTIMATEXR
			if ((GameObject.FindFirstObjectByType<UltimateXRController>() == null) && (CameraUltimateXR != null))
			{
				if (!_instantiated)
				{
					_instantiated = true;
					_avatarUltimateXR = Instantiate(CameraUltimateXR) as GameObject;
					_eventSystemUltimateXR = Instantiate(EventSystemUltimateXR) as GameObject;
					Instantiate(VRInputControllerPrefab);
				}
			}
#elif ENABLE_NREAL
			if ((GameObject.FindFirstObjectByType<NRHMDPoseTracker>() == null) && (NRealCameraXR != null))
			{
				if (!_instantiated)
				{
					_instantiated = true;
					_cameraNRealXR = Instantiate(NRealCameraXR) as GameObject;
                    _inputNRealXR = Instantiate(NRealInputXR) as GameObject;
                    _eventSystemNRealXR = Instantiate(EventSystemNReal) as GameObject;
                    Instantiate(VRInputControllerPrefab);
				}
			}
#else
            if ((GameObject.FindFirstObjectByType<Camera>() == null) && (CameraDesktop != null))
			{
				if (!_instantiated)
				{
					_instantiated = true;
					_cameraDesktop = Instantiate(CameraDesktop) as GameObject;
					_eventSystemDesktop = Instantiate(EventSystemDesktop) as GameObject;                    
					Instantiate(DesktopInputControllerPrefab);
				}
			}
            else
            {
				if (!_instantiated)
				{
					_instantiated = true;
					_cameraDesktop = GameObject.FindFirstObjectByType<Camera>().gameObject;
					_eventSystemDesktop = Instantiate(EventSystemDesktop) as GameObject;
					Instantiate(DesktopInputControllerPrefab);
				}
            }
#endif
            _state = (int)CameraStates.Camera1stPerson;
            SystemEventController.Instance.Event += OnSystemEvent;
        }

        void Start()
        {
            if (VRInputController.Instance != null) VRInputController.Instance.Event += OnVREvent;
        }

        void OnDestroy()
        {			
			Destroy();
        }

		private void Destroy()
		{
			if (_instance != null)
			{
				_player = null;
				_instance = null;
				_inputControls = null;
				if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
                if (VRInputController.Instance != null) VRInputController.Instance.Event -= OnVREvent;

				GameObject.Destroy(this.gameObject);
			}
		}

		private void LinkAvatarWithVRCamera()
		{
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR
			if ((_player != null) && (_inputControls != null))
			{
				VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerLinkWithAvatar, _player.GetGameObject());
			}					
#endif				
		}

        private void OnVREvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(VRInputController.EventVRInputControllerDisconnectPlayer))
            {
                _player = null;
            }
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
					SystemEventController.Instance.DispatchSystemEvent(EventCameraResponseToPlayer, InputControls.Camera);
                }
            }
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))
			{
				if (Instance)
				{
					DontDestroyOnLoad(Instance.gameObject);
#if ENABLE_OCULUS
					DontDestroyOnLoad(_inputModule.gameObject);
					DontDestroyOnLoad(_gazePointer.gameObject);
					DontDestroyOnLoad(_handsManager.gameObject);
#elif ENABLE_OPENXR			
					DontDestroyOnLoad(_eventSystemOpenXR);
#elif ENABLE_ULTIMATEXR	
					DontDestroyOnLoad(_avatarUltimateXR);							
					DontDestroyOnLoad(_eventSystemUltimateXR);	
#elif ENABLE_NREAL
                    DontDestroyOnLoad(_cameraNRealXR);	
                    DontDestroyOnLoad(_inputNRealXR);	
                    DontDestroyOnLoad(_eventSystemNRealXR);	
#else
					DontDestroyOnLoad(_cameraDesktop);
					DontDestroyOnLoad(_eventSystemDesktop);
#endif
				}
			}
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerReleaseAllResources))
			{
				if (Instance)
				{
					_instance = null;
					GameObject.Destroy(this.gameObject);
#if ENABLE_OCULUS
					if (_gazePointer != null) GameObject.Destroy(_gazePointer.gameObject);
					if (_inputModule != null) GameObject.Destroy(_inputModule.gameObject);
					if (_handsManager != null) GameObject.Destroy(_handsManager.gameObject);
#elif ENABLE_OPENXR
					if (_eventSystemOpenXR != null) GameObject.Destroy(_eventSystemOpenXR);
#elif ENABLE_ULTIMATEXR						
					if (_avatarUltimateXR != null) GameObject.Destroy(_avatarUltimateXR);
					if (_eventSystemUltimateXR != null) GameObject.Destroy(_eventSystemUltimateXR);
#elif ENABLE_NREAL
					if (_cameraNRealXR != null) GameObject.Destroy(_cameraNRealXR);
					if (_inputNRealXR != null) GameObject.Destroy(_inputNRealXR);
					if (_eventSystemNRealXR != null) GameObject.Destroy(_eventSystemNRealXR);
#else
					if (_cameraDesktop != null) GameObject.Destroy(_cameraDesktop);
					if (_eventSystemDesktop != null) GameObject.Destroy(_eventSystemDesktop);
#endif
				}
			}
			if (nameEvent.Equals(InitialPositionPlayer.EventNetworkInitialPositionPlayerResponse))
			{
				InputControls.SetInitialPosition((Vector3)parameters[1], (Quaternion)parameters[2]);
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
            if (!EnableCameraSwitch) return;

            switch ((CameraStates)_state)
            {
                case CameraStates.Camera1stPerson:
                    SwitchCameraState();
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
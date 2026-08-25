using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using yourvrexperience.Utils;
#if ENABLE_ULTIMATEXR
using UltimateXR.Core;
#endif

namespace yourvrexperience.VR
{
	public enum XR_HAND { right = 0, left = 1, both = 2, none = 3 }

	public enum LocomotionMode { None = 0, Rotation, Movement, Teleport }

    public class VRInputController  : InputController, IInputController
    {
		public const string EventVRInputControllerResetToInitial = "EventVRInputControllerResetToInitial";
		public const string EventVRInputControllerEnableLocomotion = "EventVRInputControllerEnableLocomotion";
		public const string EventVRInputControllerLinkWithAvatar = "EventVRInputControllerLinkWithAvatar";
		public const string EventVRInputControllerLinkWithHand = "EventVRInputControllerLinkWithHand";
		public const string EventVRInputControllerAssignCurrentPointer = "EventVRInputControllerAssignCurrentPointer";
		public const string EventVRInputControllerIndexTriggered = "EventVRInputControllerIndexTriggered";
		public const string EventVRInputControllerHandTriggered = "EventVRInputControllerHandTriggered";
		public const string EventVRInputControllerChangedHandTrackingState = "EventVRInputControllerChangedHandTrackingState";
		public const string EventVRInputControllerChangeLocomotion = "EventVRInputControllerChangeLocomotion";
		public const string EventVRInputControllerResetAllInputs = "EventVRInputControllerResetAllInputs";
		public const string EventVRInputControllerSetFreeMovement = "EventVRInputControllerSetFreeMovement";
		public const string EventVRInputControllerChangedHandSide = "EventVRInputControllerChangedHandSide";
		public const string EventVRInputControllerDisconnectPlayer = "EventVRInputControllerDisconnectPlayer";
		
		public const float TimeoutCheckMovement = 0.1f;
		public const float SensivilityJoysticks = 0.5f;
		public const float RadiusVRPlayer = 0.5f;

        private static VRInputController _instance;

        public static VRInputController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(VRInputController)) as VRInputController;
                    if (_instance)
					{
						DontDestroyOnLoad(_instance.gameObject);
					}
                }
				return _instance;
            }
        }

		public delegate void VREvent(string nameEvent, params object[] parameters);

        public event VREvent Event;
		private List<TimedEventData> _listEvents = new List<TimedEventData>();


        public void DispatchVREvent(string nameEvent, params object[] parameters)
        {
            if (Event != null) Event(nameEvent, parameters);
        }
        public void DelayVREvent(string nameEvent, float delay, params object[] parameters)
        {
            _listEvents.Add(new TimedEventData(nameEvent, -1, -1, delay, parameters));
        }

		private void ProcessQueuedEvents()
		{
            for (int i = 0; i < _listEvents.Count; i++)
            {
                TimedEventData eventData = _listEvents[i];
                if (eventData.Time == -1000)
                {
                    eventData.Destroy();
                    _listEvents.RemoveAt(i);
                    break;
                }
                else
                {
                    eventData.Time -= Time.deltaTime;
                    if (eventData.Time <= 0)
                    {
                        if ((eventData != null) && (Event != null))
                        {
                            Event(eventData.NameEvent, eventData.Parameters);
                            eventData.Destroy();
                        }
                        _listEvents.RemoveAt(i);
                        break;
                    }
                }
            }
		}

		[Tooltip("The scale of movements in the horizontal plane X,Z")]
		[SerializeField] private float ScaleMovementXZ = 1;
		[Tooltip("The scale of movements in the vertical plane Y")]
		[SerializeField] private float ScaleMovementY = 1;
		[Tooltip("Camera vertical shift")]
		[SerializeField] private float VerticalCameraShift = 0;
		[Tooltip("The Y-axis of the linked avatar will not rotate")]
		[SerializeField] private bool FixLinkedAvatarYAxis = true;
		[Tooltip("The distance that should be considered in order to detect the player has moved")]
		[SerializeField] private float TriggerDistance = 0.5f;

		[Tooltip("Enable the joysticks to move the camera")]
		[SerializeField] private XR_HAND EnableJoystickMovement = XR_HAND.none;
		[Tooltip("The speed of movement of the joystick")]
		[SerializeField] private float speedJoystickMovement = 1;

		[Tooltip("Enable the joysticks to rotate the camera")]
		[SerializeField] private XR_HAND EnableJoystickRotation = XR_HAND.none;
		[Tooltip("The degrees the joystick is going to rotate")]
		[SerializeField] private float DegreesJoystickRotation = 30;

		[Tooltip("Enable the joysticks for teleportation")]
		[SerializeField] private XR_HAND EnableJoystickTeleport = XR_HAND.none;
		[Tooltip("Enable the re-orientation using also teleport")]
		[SerializeField] private bool EnableReorientTeleport = false;

		private bool _enableLocomotion = false;
		private LocomotionMode _locomotionLeftHand;
		private LocomotionMode _locomotionRightHand;
		private Vector3 _shiftCameraFromOrigin = Vector3.zero;
		private Vector3 _currentLocalCameraRotation  = Vector3.zero;
		private GameObject _linkedAvatar;		
		private Rigidbody _avatarRigidbody;
		private ICameraPlayer _cameraAvatar;		
		private GameObject _linkedHandLeft;	
		private GameObject _linkedHandRight;	
		private Vector3 _previousPosition = Vector3.zero;
		private float _timerPreviousPosition = 0;
        private IVRController _vrController;
		private bool _isMovingCamera = false;
		private bool _applyRotation = false;
		private bool _applyFree3DMovement = false;
		private Vector3 _centerLevel = Vector3.zero;
		private XR_HAND _enableJoystickRotation;
		private GameObject _sphereCollision;
		private Rigidbody _sphereRigidBody;

#if ENABLE_ULTIMATEXR
		private UltimateXRController _uxrController;

		private UltimateXRController UxrController
		{
			get {
				if (_uxrController == null)
				{
					_uxrController = GameObject.FindObjectOfType<UltimateXRController>();
				}
				return _uxrController;
			}
		}
#endif

		public float SpeedJoystickMovement
		{
			get { return speedJoystickMovement; }
			set { speedJoystickMovement = value; }
		}
        public override bool IsVR
        {
            get { return true; }
        }
		public GameObject CameraGO 
		{ 
			get { 
				if (VRController != null)
				{
					return VRController.Container; 
				}
				else
				{
					return Camera.main.transform.root.gameObject;
				}
			}
		}
		public IVRController VRController
		{
			get {
#if ENABLE_OCULUS
				if (_vrController == null)
				{
					_vrController = GameObject.FindObjectOfType<OculusController>() as IVRController;
				}
				return _vrController;
#elif ENABLE_OPENXR
				if (_vrController == null)
				{
					_vrController = GameObject.FindObjectOfType<OpenXRController>() as IVRController;
				}
				return _vrController;
#elif ENABLE_ULTIMATEXR
				if (_vrController == null)
				{
					_vrController = GameObject.FindObjectOfType<UltimateXRController>() as IVRController;
				}
				return _vrController;
#elif ENABLE_NREAL
				if (_vrController == null)
				{
					_vrController = GameObject.FindObjectOfType<NRealController>() as IVRController;
				}
				return _vrController;
#else
				return null;
#endif
			}
		}
		public bool IsMovingCamera
		{
			get { return _isMovingCamera; }
		}
		public LocomotionMode LocomotionLeftHand
		{
			get { return _locomotionLeftHand; }
		}
		public LocomotionMode LocomotionRightHand
		{
			get { return _locomotionRightHand; }
		}
		public override Camera Camera
		{
			get { 
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
				return VRController.Camera;
#else
				return Camera.main; 
#endif
			}
		}
		public XR_HAND GetEnableJoystickTeleport()
		{
			return EnableJoystickTeleport;
		}
		public bool GetEnableReorientTeleport()
		{
			return EnableReorientTeleport;
		}
		public XR_HAND GetEnableJoystickRotation()
		{
			return EnableJoystickRotation;
		}
		public XR_HAND CustomEnableJoystickRotation
		{
			get { return _enableJoystickRotation; }
			set {  _enableJoystickRotation = value;  }
		}
		private Quaternion RotationCamera
		{
			get { return CameraGO.transform.rotation; }
			set { 
#if ENABLE_ULTIMATEXR				
			if ((CameraGO != null) && (UxrController != null) && (UxrController.UltimateXRAvatar != null))
				UxrController.UltimateXRAvatar.transform.SetPositionAndRotation(UxrController.UltimateXRAvatar.transform.position, value);
#else			
			CameraGO.transform.rotation = value; 
#endif
			}
		}
		private Vector3 PositionCamera
		{
			get { 
#if ENABLE_ULTIMATEXR				
				return VRController.Camera.transform.position;
#else				
				return CameraGO.transform.position; 
#endif				

			}
			set { 
#if ENABLE_ULTIMATEXR				
				if ((UxrController != null) && (UxrController.UltimateXRAvatar != null))
					UxrManager.Instance.MoveAvatarTo(UxrController.UltimateXRAvatar, value);
#else				
				CameraGO.transform.position = value; 
#endif				
			}
		}

#if ENABLE_AVATAR_OCULUS		
		private OculusMetaAvatarEntity _avatarEntity;

		public OculusMetaAvatarEntity AvatarEntity
		{
			get { return _avatarEntity; }
			set { _avatarEntity = value; 
				if (_avatarEntity != null)
				{
					_avatarEntity.gameObject.transform.parent = VRController.Container.transform;
				}
			}
		}
#endif		

        public override void Initialize()
        {
			base.Initialize();

			CustomEnableJoystickRotation = EnableJoystickRotation;
			InitLocomotionInHands(ref _locomotionLeftHand, ref _locomotionRightHand);

			Event += OnVREvent;

			Invoke("InitializeCurrentPointer", 0.1f);
       }

	   void InitializeCurrentPointer()
	   {
			VRInputController.Instance.DispatchVREvent(EventVRInputControllerAssignCurrentPointer);
	   }

        public override void OnDestroy()
        {
			base.OnDestroy();

			Event -= OnVREvent;
        }

		public override Transform RayPointerVR
        {
            get { return VRController.CurrentController.transform; }
        }

        public override bool EnableMouseRotation()
        {
            return false;
        }

        public override bool IsMoving()
        {
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
            return _isMovingCamera;
#else
            return base.IsMoving();
#endif
        }

        public override Vector2 GetMovementJoystick()
        {
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			if (VRController != null)
			{
            	return VRController.GetVector2Joystick(XR_HAND.left);
			}
			else
			{
				return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
			}        	
#else
            return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
#endif
        }

        public override bool ActionPrimaryDown()
        {
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			if (VRController != null)
			{
            	return VRController.GetIndexTriggerDown(XR_HAND.both);
			}
			else
			{
				return base.ActionPrimaryDown();
			}        	
#else
            return base.ActionPrimaryDown();
#endif
        }

        public override bool ActionPrimaryUp()
        {
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			if (VRController != null)
			{
            	return VRController.GetIndexTriggerUp(XR_HAND.both);
			}
			else
			{
				return base.ActionPrimaryUp();
			}        	
#else
            return base.ActionPrimaryUp();
#endif
        }

        public override bool ActionPrimary()
        {
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			if (VRController != null)
			{
            	return VRController.GetIndexTrigger(XR_HAND.both);
			}
			else
			{
				return base.ActionPrimary();
			}        	
#else
            return base.ActionPrimary();
#endif
        }

        public override bool ActionSecondaryDown()
        {
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			if (VRController != null)
			{
				return VRController.GetHandTriggerDown(XR_HAND.both);
			}
			else
			{
				return base.ActionSecondaryDown();			
			}        	
#else
			return base.ActionSecondaryDown();			
#endif
        }

        public override bool ActionSecondaryUp()
        {
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			if (VRController != null)
			{
				return VRController.GetHandTriggerUp(XR_HAND.both);
			}
			else
			{
				return base.ActionSecondaryUp();			
			}        	
#else
			return base.ActionSecondaryUp();			
#endif
        }

        public override bool ActionSecondary()
        {
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			if (VRController != null)
			{
				return VRController.GetHandTrigger(XR_HAND.both);
			}
			else
			{
				return base.ActionSecondary();			
			}        	
#else
			return base.ActionSecondary();			
#endif
        }

		public override bool ActionMenuPressed()
        {
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			if (VRController != null)
			{
				return VRController.GetOneButtonDown(XR_HAND.both);
			}
			else
			{
				return base.ActionMenuPressed();
			}        	
#else
			return base.ActionMenuPressed();			
#endif
        }
		

        public override bool SwitchedCameraPressed()
        {
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
            return false;
#else
            return base.SwitchedCameraPressed();
#endif
        }

		public void UpdateHandSideController()
		{
			if (VRController != null)
			{
				VRController.UpdateHandSideController();
			}
		}

		private void EnableTeleport()
		{
			if (_linkedAvatar != null)
			{
				if (EnableJoystickTeleport != XR_HAND.none)
				{
					DispatchVREvent(TeleportController.EventTeleportControllerEnable, true, EnableJoystickTeleport, EnableReorientTeleport);
				}
				else
				{
					DispatchVREvent(TeleportController.EventTeleportControllerEnable, false);
				}
			}
		}

		private void OnVREvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(EventVRInputControllerSetFreeMovement))
			{				
				_applyFree3DMovement = (bool)parameters[0];
			}
			if (nameEvent.Equals(EventVRInputControllerResetAllInputs))
			{				
				VRController.ResetState();
			}
			if (nameEvent.Equals(EventVRInputControllerResetToInitial))
			{
				PositionCamera = Vector3.zero;
				if (parameters.Length > 0)
				{
					PositionCamera = (Vector3)parameters[0];
				}				
				Vector3 rotationApplied = new Vector3(0, 0, 0);
				RotationCamera =  Quaternion.Euler(rotationApplied);
				if (parameters.Length > 1)
				{
					RotationCamera = (Quaternion)parameters[1];
					rotationApplied = RotationCamera.eulerAngles;
				}				
				_currentLocalCameraRotation = rotationApplied;
				_shiftCameraFromOrigin = Vector3.zero;
			}
			if (nameEvent.Equals(EventVRInputControllerEnableLocomotion))
			{
				_enableLocomotion = (bool)parameters[0];
			}
			if (nameEvent.Equals(EventVRInputControllerChangeLocomotion))
			{				
				if ((bool)parameters[0])
				{
					_locomotionRightHand = (LocomotionMode)parameters[1];
				}
				else
				{
					_locomotionLeftHand = (LocomotionMode)parameters[1];
				}
			}
			if (nameEvent.Equals(VRInputController.EventVRInputControllerEnableLocomotion))
			{
				if ((bool)parameters[0])
				{
					ApplyLocomotionType();
				}
			}
			if (nameEvent.Equals(EventVRInputControllerLinkWithAvatar))
			{
				_linkedAvatar = (GameObject)parameters[0];
				_avatarRigidbody = _linkedAvatar.GetComponent<Rigidbody>();
				_cameraAvatar = _linkedAvatar.GetComponent<ICameraPlayer>();
				EnableTeleport();
			}
			if (nameEvent.Equals(EventVRInputControllerLinkWithHand))
			{
				bool iAmOwner = (bool)parameters[0];
				if (iAmOwner)
				{
					GameObject hand = (GameObject)parameters[1];
					XR_HAND targetHand = (XR_HAND)parameters[2];
					if (targetHand == XR_HAND.left)
					{
						_linkedHandLeft = hand;
					}
					else
					{
						_linkedHandRight = hand;
					}
				}
			}
			if (nameEvent.Equals(TeleportController.EventTeleportControllerConfirmation))
			{
				Vector3 shiftTeleport = (Vector3)parameters[0];
				Quaternion orientationTeleport = (Quaternion)parameters[1];
				if (_linkedAvatar != null)
				{
					_linkedAvatar.transform.position = new Vector3(_linkedAvatar.transform.position.x,
																	_linkedAvatar.transform.position.y,
																	_linkedAvatar.transform.position.z);
				}
				else
				{
					PositionCamera = new Vector3(PositionCamera.x,
															PositionCamera.y + (shiftTeleport.y + CameraGO.transform.localScale.y),
															PositionCamera.z);
				}				
				shiftTeleport.y = 0;
				_shiftCameraFromOrigin += shiftTeleport;
				if (EnableReorientTeleport && (orientationTeleport != Quaternion.identity))
				{
					float angle = 0.0f;
        			Vector3 axis = Vector3.zero;
        			orientationTeleport.ToAngleAxis(out angle, out axis);
					Vector3 rotationApplied = new Vector3(0, (angle * axis.y) - 180, 0);
					RotationCamera = Quaternion.Euler(rotationApplied);
					_currentLocalCameraRotation = rotationApplied;
#if ENABLE_OCULUS
					VRInputController.Instance.DispatchVREvent(OculusHandsManager.EventOculusHandsManagerRotationCameraApplied, true, rotationApplied);
#endif
				}
			}
			if (nameEvent.Equals(ScreenController.EventScreenControllerRequestCameraData))
            {
                GameObject targetScreen = (GameObject)parameters[0];
				if (VRController != null)
				{
					DispatchVREvent(ScreenController.EventScreenControllerResponseCameraData, targetScreen, VRController.HeadController.transform.position, VRController.HeadController.transform.forward);
				}
				else
				{
					DispatchVREvent(ScreenController.EventScreenControllerResponseCameraData, targetScreen, Camera.main.transform.position, Camera.main.transform.forward);
				}				
            }
			if (nameEvent.Equals(VRInputController.EventVRInputControllerDisconnectPlayer))
			{
				_linkedAvatar = null;
			}
		}

		private void RefreshMovementDetected()
		{
			// Check if there is movement
			_timerPreviousPosition += Time.deltaTime;
			if (_timerPreviousPosition > TimeoutCheckMovement)
			{
				_timerPreviousPosition = 0;
				float distance = Vector3.Distance(_previousPosition, PositionCamera);
				_previousPosition = PositionCamera;
				if (distance > TriggerDistance)
				{
					_isMovingCamera = true;
				}
				else
				{
					_isMovingCamera = false;
				}
			}
		}

		private void ApplyJoystickMovement()
		{
			if (EnableJoystickMovement != XR_HAND.none)
			{
				Vector2 joystick = Vector2.zero;
				Vector3 cameraForward = Vector3.zero;
				Vector3 cameraRigth = Vector3.zero;
				if (VRController != null)
				{
					joystick = VRController.GetVector2Joystick(EnableJoystickMovement);
					cameraForward = VRController.HeadController.transform.forward;
					cameraRigth = VRController.HeadController.transform.right;
				}

				float triggeredMovement = joystick.sqrMagnitude;
				if (triggeredMovement > SensivilityJoysticks)
				{
					Vector3 forward = joystick.y * cameraForward;
                	Vector3 lateral = joystick.x * cameraRigth;
					Vector3 upwards = Vector3.zero;
					if (_applyFree3DMovement)
					{
						if (VRController.GetHandTrigger(XR_HAND.both))
						{
							upwards = joystick.y * Vector3.up;
							forward = Vector3.zero;
							lateral = Vector3.zero;
						}
					}
					Vector3 shiftMovement = (forward + lateral + upwards).normalized;
 
 					if (_applyFree3DMovement)
					{
						_shiftCameraFromOrigin +=  shiftMovement * speedJoystickMovement * Time.deltaTime;
					}
					else
					{
						RaycastHit hitCollision = new RaycastHit();
						GameObject collideObject = RaycastingTools.GetRaycastObject(PositionCamera, shiftMovement, RadiusVRPlayer, ref hitCollision);
						bool applyShift = true;
						if (collideObject != null)
						{
							if (collideObject.GetComponent<Collider>() != null)
							{
								if (!collideObject.GetComponent<Collider>().isTrigger)
								{
									applyShift = false;
								}
							}
						}
						if (applyShift)
						{
							_shiftCameraFromOrigin +=  shiftMovement * speedJoystickMovement * Time.deltaTime;
							_shiftCameraFromOrigin.y = 0;
						}
					}
				}
			}
		}

		private void ApplyJoystickRotation()
        {
			if (_enableJoystickRotation != XR_HAND.none)
			{
				Vector2 joystick = Vector2.zero;
				if (VRController != null)
				{
					joystick = VRController.GetVector2Joystick(_enableJoystickRotation);
				}

				if (Mathf.Abs(joystick.x) > SensivilityJoysticks)
				{
					if (!_applyRotation)
					{
						_applyRotation = true;
						Vector3 rotationApplied = Vector3.zero;

						if (joystick.x > 0)
						{
							rotationApplied = new Vector3(0, DegreesJoystickRotation, 0);
						}
						else
						{
							rotationApplied = new Vector3(0, -DegreesJoystickRotation, 0);
						}
#if ENABLE_ULTIMATEXR							
						UxrManager.Instance.RotateAvatar(UxrController.UltimateXRAvatar, rotationApplied.y);
#else
						CameraGO.transform.Rotate(rotationApplied);
#endif							
						_currentLocalCameraRotation += rotationApplied;
#if ENABLE_OCULUS
						VRInputController.Instance.DispatchVREvent(OculusHandsManager.EventOculusHandsManagerRotationCameraApplied, false, rotationApplied);
#endif
					}
				}
				else
				{
					_applyRotation = false;
				}
			}
        }


		private void UpdateLinkedAvatar()
		{
			if (_linkedAvatar != null)
			{
				if (_applyFree3DMovement)
				{
					_linkedAvatar.transform.position = new Vector3(PositionCamera.x, PositionCamera.y, PositionCamera.z);
				}
				else
				{
					_linkedAvatar.transform.position = new Vector3(PositionCamera.x, _linkedAvatar.transform.position.y, PositionCamera.z);
				}
				Vector3 directionAvatar = VRController.HeadController.transform.forward;				
				if (FixLinkedAvatarYAxis)
				{
					_linkedAvatar.transform.forward = new Vector3(directionAvatar.x, 0, directionAvatar.z);
				}
				else
				{
					_linkedAvatar.transform.forward = directionAvatar;
				}
			}
			if (_linkedHandRight != null)
			{
				_linkedHandRight.transform.position = VRController.HandRightController.transform.position;
				_linkedHandRight.transform.rotation = VRController.HandRightController.transform.rotation;
			}
			if (_linkedHandLeft != null)
			{
				_linkedHandLeft.transform.position = VRController.HandLeftController.transform.position;
				_linkedHandLeft.transform.rotation = VRController.HandLeftController.transform.rotation;
			}
		}

		public void SetRotation(XR_HAND rotation)
		{
			EnableJoystickRotation = rotation;
			_enableJoystickRotation = rotation;
		}

		public void SetMovement(XR_HAND movement)
		{
			EnableJoystickMovement = movement;
		}

		public void SetTeleport(XR_HAND teleport)
		{
			EnableJoystickTeleport = teleport;
			if (EnableJoystickTeleport != XR_HAND.none)
			{
				DispatchVREvent(TeleportController.EventTeleportControllerEnable, true, EnableJoystickTeleport, EnableReorientTeleport);
			}
			else
			{
				DispatchVREvent(TeleportController.EventTeleportControllerEnable, false);
			}
		}

		public XR_HAND GetRotation()
		{
			return _enableJoystickRotation;
		}

		public XR_HAND GetMovement()
		{
			return EnableJoystickMovement;
		}

		public XR_HAND GetTeleport()
		{
			return EnableJoystickTeleport;
		}

		public void ApplyHandTrackingLocomotion()
		{
			SetMovement(XR_HAND.none);
			SetRotation(XR_HAND.none);
			SetMovement(XR_HAND.none);
			SetTeleport(XR_HAND.none);

			DispatchVREvent(TeleportController.EventTeleportControllerEnable, true, XR_HAND.both, false);
		}

		public void ApplyLocomotionType()
		{
			SetMovement(XR_HAND.none);
			SetRotation(XR_HAND.none);
			SetMovement(XR_HAND.none);
			SetTeleport(XR_HAND.none);

			if (_locomotionLeftHand == _locomotionRightHand)
			{
				switch (_locomotionLeftHand)
				{
					case LocomotionMode.None:
						break;
					case LocomotionMode.Rotation:
						SetRotation(XR_HAND.both);
						break;
					case LocomotionMode.Movement:
						SetMovement(XR_HAND.both);
						break;
					case LocomotionMode.Teleport:
						SetTeleport(XR_HAND.both);
						break;
				}
			}
			else
			{
				switch (_locomotionLeftHand)
				{
					case LocomotionMode.None:
						break;
					case LocomotionMode.Rotation:
						SetRotation(XR_HAND.left);
						break;
					case LocomotionMode.Movement:
						SetMovement(XR_HAND.left);
						break;
					case LocomotionMode.Teleport:
						SetTeleport(XR_HAND.left);
						break;
				}
				switch (_locomotionRightHand)
				{
					case LocomotionMode.None:
						break;
					case LocomotionMode.Rotation:
						SetRotation(XR_HAND.right);
						break;
					case LocomotionMode.Movement:
						SetMovement(XR_HAND.right);
						break;
					case LocomotionMode.Teleport:
						SetTeleport(XR_HAND.right);
						break;
				}
			}
		}

		public void InitLocomotionInHands(ref LocomotionMode leftHand, ref LocomotionMode rightHand)
		{
			if (GetRotation() == XR_HAND.left)
			{
				leftHand = LocomotionMode.Rotation;
			}
			if (GetRotation() == XR_HAND.right)
			{
				rightHand = LocomotionMode.Rotation;
			}
			if (GetRotation() == XR_HAND.both)
			{
				leftHand = LocomotionMode.Rotation;
				rightHand = LocomotionMode.Rotation;
			}
			if (GetMovement() == XR_HAND.left)
			{
				leftHand = LocomotionMode.Movement;
			}
			if (GetMovement() == XR_HAND.right)
			{
				rightHand = LocomotionMode.Movement;
			}
			if (GetMovement() == XR_HAND.both)
			{
				leftHand = LocomotionMode.Movement;
				rightHand = LocomotionMode.Movement;
			}
			if (GetTeleport() == XR_HAND.left)
			{
				leftHand = LocomotionMode.Teleport;
			}
			if (GetTeleport() == XR_HAND.right)
			{
				rightHand = LocomotionMode.Teleport;
			}
			if (GetTeleport() == XR_HAND.both)
			{
				leftHand = LocomotionMode.Teleport;
				rightHand = LocomotionMode.Teleport;
			}
		}

		public override void SetInitialPosition(Vector3 position, Quaternion rotation)
		{
			// POSTION
			_centerLevel = position;
			_shiftCameraFromOrigin = Vector3.zero;

			// ROTATION
			float angle = 0.0f;
			Vector3 axis = Vector3.zero;
			rotation.ToAngleAxis(out angle, out axis);
			Vector3 rotationApplied = new Vector3(0, (angle * axis.y), 0);
			RotationCamera = Quaternion.Euler(rotationApplied);
			_currentLocalCameraRotation = rotationApplied;

			VRInputController.Instance.DispatchVREvent(OculusHandsManager.EventOculusHandsManagerRotationCameraApplied, true, _currentLocalCameraRotation);
			UpdateCameraRigPositionWith6DOF();
		}

		void ReportHandGestures()
		{
			bool shouldCheckGestures = true;
#if ENABLE_OCULUS
			shouldCheckGestures = !OculusHandsManager.Instance.HandsBeingTracked;
#endif

			if (shouldCheckGestures)
			{
				if (VRController.GetIndexTriggerDown(XR_HAND.both, false))
				{
					if (VRController.GetIndexTriggerDown(XR_HAND.left, false))
					{
						VRInputController.Instance.DispatchVREvent(EventVRInputControllerIndexTriggered, true, XR_HAND.left);
					}
					if (VRController.GetIndexTriggerDown(XR_HAND.right, false))
					{
						VRInputController.Instance.DispatchVREvent(EventVRInputControllerIndexTriggered, true, XR_HAND.right);
					}					
				}
				if (VRController.GetIndexTriggerUp(XR_HAND.both, false))
				{
					if (VRController.GetIndexTriggerUp(XR_HAND.left, false))
					{
						VRInputController.Instance.DispatchVREvent(EventVRInputControllerIndexTriggered, false, XR_HAND.left);
					}
					if (VRController.GetIndexTriggerUp(XR_HAND.right, false))
					{
						VRInputController.Instance.DispatchVREvent(EventVRInputControllerIndexTriggered, false, XR_HAND.right);
					}
				}
				if (VRController.GetHandTriggerDown(XR_HAND.both, false))
				{
					if (VRController.GetHandTriggerDown(XR_HAND.left, false))
					{
						VRInputController.Instance.DispatchVREvent(EventVRInputControllerHandTriggered, true, XR_HAND.left);
					}					
					if (VRController.GetHandTriggerDown(XR_HAND.right, false))
					{
						VRInputController.Instance.DispatchVREvent(EventVRInputControllerHandTriggered, true, XR_HAND.right);
					}
				}
				if (VRController.GetHandTriggerUp(XR_HAND.both, false))
				{
					if (VRController.GetHandTriggerUp(XR_HAND.left, false))
					{
						VRInputController.Instance.DispatchVREvent(EventVRInputControllerHandTriggered, false, XR_HAND.left);
					}										
					if (VRController.GetHandTriggerUp(XR_HAND.right, false))
					{
						VRInputController.Instance.DispatchVREvent(EventVRInputControllerHandTriggered, false, XR_HAND.right);
					}
				}
			}
		}

		private void UpdateCameraRigPositionWith6DOF()
		{
#if ENABLE_NREAL			
			return;
#endif
			if  (VRController != null)
			{
#if ENABLE_ULTIMATEXR				
				Vector3 positionWorld = VRController.HeadController.transform.localPosition;
				Vector3 centerLevel = _centerLevel;
				if (_linkedAvatar != null)
				{
					centerLevel = new Vector3(_centerLevel.x, _cameraAvatar.PositionBase.y, _centerLevel.z);
				}
				else
				{
					centerLevel = new Vector3(_centerLevel.x, PositionCamera.y, _centerLevel.x);
				}
				Vector2 scaledPositionWorld = new Vector2(positionWorld.x * ScaleMovementXZ, positionWorld.z * ScaleMovementXZ);
				Vector3 posRotatedWorld = yourvrexperience.Utils.Utilities.RotatePoint(scaledPositionWorld, Vector2.zero, -_currentLocalCameraRotation.y);
				Vector3 nextPosition = centerLevel 
									+ new Vector3(posRotatedWorld.x, 0, posRotatedWorld.y) 
									+ _shiftCameraFromOrigin
									+ new Vector3(0, -VerticalCameraShift, 0);
				PositionCamera = nextPosition;
#else
				Vector3 positionWorld = VRController.HeadController.transform.localPosition;
				Vector3 centerLevel = _centerLevel;
				if (_linkedAvatar != null)
				{
					centerLevel = new Vector3(_centerLevel.x, _cameraAvatar.PositionBase.y, _centerLevel.z);
				}
				else
				{
					centerLevel = new Vector3(_centerLevel.x, PositionCamera.y, _centerLevel.x);
				}
				Vector3 scaledPositionWorld = new Vector2(positionWorld.x * ScaleMovementXZ, positionWorld.z * ScaleMovementXZ);
				Vector3 posRotatedWorld = yourvrexperience.Utils.Utilities.RotatePoint(scaledPositionWorld, Vector2.zero, -_currentLocalCameraRotation.y);
				Vector3 nextPosition = centerLevel + new Vector3(posRotatedWorld.x, 0, posRotatedWorld.y) + _shiftCameraFromOrigin;
				PositionCamera = nextPosition;
				float finalVerticalCameraShift = VerticalCameraShift;
#if ENABLE_OPENXR				
				// finalVerticalCameraShift += 1;
#endif
				Vector3 shiftToRecenter = -new Vector3(VRController.HeadController.transform.localPosition.x, finalVerticalCameraShift - (positionWorld.y * ScaleMovementY), VRController.HeadController.transform.localPosition.z);
				VRController.HeadController.transform.parent.localPosition = shiftToRecenter;
#endif
			}
			else
			{
				PositionCamera = new Vector3(0, PositionCamera.y, 0) + _shiftCameraFromOrigin;
			}
		}

		void Update()
		{
			ProcessQueuedEvents();
			
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			UpdateCameraRigPositionWith6DOF();
			if (_enableLocomotion)
			{
				ApplyJoystickRotation();
				ApplyJoystickMovement();
				RefreshMovementDetected();
			}
			UpdateLinkedAvatar();

			ReportHandGestures();

			if (VRController != null)
			{
				if (VRController.GetHandTrigger(XR_HAND.both) && VRController.GetThumbstickDown(XR_HAND.both))
				{
					UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerToggleInGameDebugConsole);
				}
			}
#endif			
		}
		

    }
}
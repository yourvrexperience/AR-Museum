#if ENABLE_OCULUS
using Oculus.Interaction;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using yourvrexperience.Utils;
using UnityEngine.Assertions;

namespace yourvrexperience.VR
{
	public class OculusHandsManager : MonoBehaviour
	{
		public const bool DEBUG_FINGERS = false;

		public const string EventOculusHandsManagerStateInited = "EventOculusHandsManagerStateInited";
		public const string EventOculusHandsManagerStateChanged = "EventOculusHandsManagerStateChanged";
		public const string EventOculusHandsManagerRotationCameraApplied = "EventOculusHandsManagerRotationCameraApplied";
		public const string EventOculusHandsManagerSetUpLaserPointerInitialize = "EventOculusHandsManagerSetUpLaserPointerInitialize";

		public const string EventPinchInteractionToolRequestRay = "EventPinchInteractionToolRequestRay";
		public const string EventPinchInteractionToolResponseRay = "EventPinchInteractionToolResponseRay";
		public const string EventPinchInteractionToolPinchPressed = "EventPinchInteractionToolPinchPressed";
		public const string EventPinchInteractionToolPinchReleased = "EventPinchInteractionToolPinchReleased";

#if ENABLE_OCULUS
		[SerializeField] private float InteractionFingerSize = 0.1f;

		private bool _handsBeingTracked = false;
		private bool _enableVisualRays = false;
		private List<GameObject> _fingersInteractionHand = new List<GameObject>();
		private List<GameObject> _fingersInteractionController = new List<GameObject>();

		protected XR_HAND _currentHandWithLaser = XR_HAND.none;

		private GameObject _leftController = null;
		private GameObject _rightController = null;
		private Transform _referenceToRay = null;

		private InteractableOculusHandsCreator _interactableOculusHandsCreator;

		private GameObject _leftHandGO;
		private GameObject _rightHandGO;
		private OVRHand _leftHand;
		private OVRHand _rightHand;
		private SkinnedMeshRenderer _leftMeshRenderer;
		private SkinnedMeshRenderer _rightMeshRenderer;

		private GameObject _leftHandContainer;
		private GameObject _rightHandContainer;
		private OVRInputModule _ovrInputModule;

		TeleportController _teleportRight;
		TeleportController _teleportLeft;

		public OVRHand LeftHand
		{
			get { return _leftHand; }
		}
		public OVRHand RightHand
		{
			get { return _rightHand; }
		}
		public bool HandsBeingTracked
		{
			get { return _handsBeingTracked; }
		}
		public XR_HAND CurrentHandWithLaser
		{
			get { return _currentHandWithLaser; }
		}
		public bool EnableVisualRays
		{
			get { return _enableVisualRays; }
		}
		public Transform ReferenceToRay
		{
			get { return _referenceToRay; }
		}
		public GameObject LeftHandContainer
		{
			get {
				if (_leftHandContainer == null)
				{
					_leftHandContainer = new GameObject();
					_leftHandContainer.name = "CONTAINER_LEFT_HAND";
				}
				if ((_leftHandContainer.transform.parent == null) && (_leftHandGO != null))
				{
					_leftHandContainer.transform.parent = _leftHandGO.transform;
					_leftHandContainer.transform.localPosition = new Vector3(0.1f, 0.05f, 0);
					_leftHandContainer.transform.localRotation = Quaternion.identity;
					_leftHandContainer.transform.Rotate(new Vector3(0, 90, 90));
				}
				return _leftHandContainer;
			}
		}
		public GameObject RightHandContainer
		{
			get {
				if (_rightHandContainer == null)
				{
					_rightHandContainer = new GameObject();
					_rightHandContainer.name = "CONTAINER_RIGHT_HAND";
				}
				if ((_rightHandContainer.transform.parent == null) && (_rightHandGO != null))
				{
					_rightHandContainer.transform.parent = _rightHandGO.transform;
					_rightHandContainer.transform.localPosition = new Vector3(-0.1f, -0.02f, 0);
					_rightHandContainer.transform.localRotation = Quaternion.identity;
					_rightHandContainer.transform.Rotate(new Vector3(0, -90, 90));
				}
				return _rightHandContainer;
			}
		}
		private OVRInputModule OvrInputModule
		{
			get {
					if (_ovrInputModule == null)
					{
						_ovrInputModule = GameObject.FindObjectOfType<OVRInputModule>();
					}
					return _ovrInputModule;
			}
		}

		public static OculusHandsManager Instance { get; private set; }

		public void Initialize(GameObject leftHand, GameObject leftController, GameObject rightHand, GameObject rightController)
		{
			if (Instance && Instance != this)
			{
				Destroy(this);
				return;
			}
			Instance = this;

			_leftHandGO = leftHand;
			_rightHandGO = rightHand;
			_leftController = leftController;
			_rightController = rightController;

			ResolveHandComponents();

			Invoke("ReportStartedHands", 0.1f);

			VRInputController.Instance.Event += OnVREvent;
			SystemEventController.Instance.Event += OnSystemEvent;
		}

		private void ResolveHandComponents()
		{
			if (_leftHandGO != null)
			{
				_leftHand = _leftHandGO.GetComponent<OVRHand>();
				if (_leftHand == null) _leftHand = _leftHandGO.GetComponentInChildren<OVRHand>(true);
				_leftMeshRenderer = _leftHandGO.GetComponentInChildren<SkinnedMeshRenderer>(true);
			}
			if (_rightHandGO != null)
			{
				_rightHand = _rightHandGO.GetComponent<OVRHand>();
				if (_rightHand == null) _rightHand = _rightHandGO.GetComponentInChildren<OVRHand>(true);
				_rightMeshRenderer = _rightHandGO.GetComponentInChildren<SkinnedMeshRenderer>(true);
			}

			if (_leftHand == null || _rightHand == null)
			{
				Debug.LogWarning("[OculusHandsManager] Could not resolve an OVRHand on one or both hand GameObjects. " +
					"Make sure the object passed to Initialize() is (or contains) an OVRHandPrefab.");
			}
		}

		private void ReportStartedHands()
		{
			VRInputController.Instance.DispatchVREvent(EventOculusHandsManagerStateInited, _handsBeingTracked);
		}

		public void Destroy()
		{
			if (Instance != null)
			{
				Instance = null;
				if (VRInputController.Instance != null) VRInputController.Instance.Event -= OnVREvent;
				if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
				if (_rightHandContainer != null) GameObject.Destroy(_rightHandContainer);
				if (_leftHandContainer != null) GameObject.Destroy(_leftHandContainer);
				OVRHand[] ovrHands = GameObject.FindObjectsOfType<OVRHand>();
				foreach (OVRHand ovrHand in ovrHands)
				{
					if (ovrHand != null)
					{
						if (ovrHand.PointerPose != null)
						{
							GameObject.Destroy(ovrHand.PointerPose.gameObject);
						}
					}
				}
				GameObject.Destroy(this.gameObject);
			}
		}

		private void RefreshLocomotionConfiguration()
		{
			if (_handsBeingTracked)
			{
				VRInputController.Instance.DispatchVREvent(EventPinchInteractionToolRequestRay, OculusController.Instance.HandSelected);
				VRInputController.Instance.ApplyHandTrackingLocomotion();
			}
			else
			{
				VRInputController.Instance.CustomEnableJoystickRotation = VRInputController.Instance.GetEnableJoystickRotation();
				VRInputController.Instance.ApplyLocomotionType();
			}
			if (OvrInputModule != null)
			{
				if (_handsBeingTracked)
				{
					OvrInputModule.joyPadClickButton = OVRInput.Button.One |  OVRInput.Button.Three;
				}
				else
				{
					OvrInputModule.joyPadClickButton = OVRInput.Button.PrimaryIndexTrigger | OVRInput.Button.SecondaryIndexTrigger;
				}
			}
		}


		private void CheckHandsBeingTracked()
		{
			bool handsTracked = false;

			if (_leftHand != null)
			{
				if (_leftHand.IsTracked)
				{
					handsTracked = true;
				}
			}

			if (_rightHand != null)
			{
				if (_rightHand.IsTracked)
				{
					handsTracked = true;
				}
			}

			if (handsTracked != _handsBeingTracked)
			{
				bool previousTracking = _handsBeingTracked;
				_handsBeingTracked = handsTracked;
				if (!previousTracking && handsTracked)
				{
					VRInputController.Instance.DispatchVREvent(EventOculusHandsManagerStateChanged, true);
				}
				else
				{
					VRInputController.Instance.DispatchVREvent(EventOculusHandsManagerStateChanged, false);
				}
			}
		}

		private void RefreshSphereInteractionRadius()
		{
			if (!_handsBeingTracked)
			{
				foreach (GameObject item in _fingersInteractionController)
				{
					item.GetComponent<FingerInteractionRadius>().SetActive(true);
					item.GetComponent<FingerInteractionRadius>().SetDebugMode(DEBUG_FINGERS);
					item.GetComponent<FingerInteractionRadius>().SetRadius(InteractionFingerSize);
				}
				foreach (GameObject item in _fingersInteractionHand)
				{
					item.GetComponent<FingerInteractionRadius>().SetActive(false);
				}
			}
			else
			{
				foreach (GameObject item in _fingersInteractionHand)
				{
					item.GetComponent<FingerInteractionRadius>().SetActive(true);
					item.GetComponent<FingerInteractionRadius>().SetDebugMode(DEBUG_FINGERS);
					item.GetComponent<FingerInteractionRadius>().SetRadius(InteractionFingerSize);
				}
				foreach (GameObject item in _fingersInteractionController)
				{
					item.GetComponent<FingerInteractionRadius>().SetActive(false);
				}
			}
		}

		protected void SwitchControlsHandToControllers(bool handTrackingState)
		{
			if (_interactableOculusHandsCreator == null)
			{
				_interactableOculusHandsCreator = GameObject.FindObjectOfType<InteractableOculusHandsCreator>();
				if (_interactableOculusHandsCreator != null) _interactableOculusHandsCreator.Initialize();
			}
			ActivateTrackingHands(handTrackingState);
			RefreshSphereInteractionRadius();
			VRInputController.Instance.DelayVREvent(EventOculusHandsManagerSetUpLaserPointerInitialize, 0.1f, handTrackingState, XR_HAND.right, _referenceToRay, false);
		}

		private void ActivateTrackingHands(bool activation)
		{
			if (_rightMeshRenderer != null) _rightMeshRenderer.enabled = activation;
			if (_leftMeshRenderer != null) _leftMeshRenderer.enabled = activation;
			_enableVisualRays = activation;

			SetRayVisualsActive(activation);
		}

		private void SetRayVisualsActive(bool activation)
		{
		}

		private void OnVREvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(InteractableOculusHandsCreator.EventInteractableOculusHandsCreatorStarted))
			{
				_interactableOculusHandsCreator = (InteractableOculusHandsCreator)parameters[0];
				_interactableOculusHandsCreator.Initialize();
				TeleportController[] teleportControllers = GameObject.FindObjectsOfType<TeleportController>();
				foreach(TeleportController teleporter in teleportControllers)
				{
					if (teleporter.IsRightHand())
					{
						_teleportRight = teleporter;
					}
					else
					{
						_teleportLeft = teleporter;
					}
				}
			}
			if (nameEvent.Equals(EventOculusHandsManagerStateChanged) || nameEvent.Equals(EventOculusHandsManagerStateInited))
			{
				bool handTrackingState = (bool)parameters[0];
				_currentHandWithLaser = XR_HAND.none;
				SwitchControlsHandToControllers(handTrackingState);
				RefreshLocomotionConfiguration();
			}
			if (nameEvent.Equals(VRInputController.EventVRInputControllerEnableLocomotion))
			{
				if ((bool)parameters[0])
				{
					Invoke("RefreshLocomotionConfiguration", 0.1f);
				}
			}
			if (nameEvent.Equals(FingerInteractionRadius.EventSphereInteractionRadiusInited))
			{
				GameObject targetFinger = (GameObject)parameters[0];
				if ((yourvrexperience.Utils.Utilities.FindGameObjectInChilds(_leftController, targetFinger))
					|| (yourvrexperience.Utils.Utilities.FindGameObjectInChilds(_rightController, targetFinger)))
				{
					if (_fingersInteractionController.Count < 2)
					{
						_fingersInteractionController.Add(targetFinger);
					}
				}
				else
				{
					if (_fingersInteractionHand.Count < 2)
					{
						_fingersInteractionHand.Add(targetFinger);
					}
				}
				RefreshSphereInteractionRadius();
			}
			if (nameEvent.Equals(EventOculusHandsManagerSetUpLaserPointerInitialize))
			{
				bool isHandTracking = (bool)parameters[0];
				_currentHandWithLaser = (XR_HAND)parameters[1];
				_referenceToRay = (Transform)parameters[2];
			}
			if (nameEvent.Equals(EventPinchInteractionToolPinchPressed))
			{
				_currentHandWithLaser = (XR_HAND)parameters[0];
				_referenceToRay = (Transform)parameters[2];
				OculusController.Instance.HandSelected = _currentHandWithLaser;
			}
			if (nameEvent.Equals(EventPinchInteractionToolPinchReleased))
			{
				XR_HAND targetHand = (XR_HAND)parameters[0];
				_referenceToRay = (Transform)parameters[2];
			}
			if (nameEvent.Equals(EventPinchInteractionToolResponseRay))
			{
				_currentHandWithLaser = (XR_HAND)parameters[0];
				_referenceToRay = (Transform)parameters[1];
				OculusController.Instance.HandSelected = _currentHandWithLaser;
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))
			{
				if (Instance)
				{
					if (_rightHandContainer != null)
					{
						DontDestroyOnLoad(_rightHandContainer);
					}
					if (_leftHandContainer != null)
					{
						DontDestroyOnLoad(_leftHandContainer);
					}

					OVRHand[] ovrHands = GameObject.FindObjectsOfType<OVRHand>();
					foreach (OVRHand ovrHand in ovrHands)
					{
						if (ovrHand != null)
						{
							DontDestroyOnLoad(ovrHand.PointerPose.gameObject);
						}
					}
					DontDestroyOnLoad(Instance.gameObject);
				}
			}
		}

		private void Update()
		{
			if ((_leftHandGO != null) && (_rightHandGO != null))
			{
				CheckHandsBeingTracked();
			}
		}
#endif
	}
}
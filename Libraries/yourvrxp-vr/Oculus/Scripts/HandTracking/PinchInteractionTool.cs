using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.VR
{
	/// <summary>
	/// Detects a pinch on a single hand and dispatches the pinch/ray events onto the
	/// VRInputController bus. Rebuilt on OVRHand (Meta XR Core SDK) — no OculusSampleFramework,
	/// no bone capsules, no OVRSkeleton.BoneId, so it is unaffected by the OVR -> OpenXR
	/// hand-skeleton change.
	///
	/// Supports two placement patterns:
	///   1. Static: drop it under a hand in the scene, set _isRightHand and (optionally)
	///      assign _ovrHand in the inspector. Start() initializes it automatically.
	///   2. Runtime: instantiated by InteractableOculusHandsCreator, parented to the
	///      CameraRig, with IsRightHandedTool set then Initialize() called. The OVRHand is
	///      then resolved by handedness from OculusHandsManager.Instance.
	/// </summary>
	public class PinchInteractionTool : MonoBehaviour
	{
		// EVENTS — names and string values unchanged, so existing listeners keep matching.
		public const string EventPinchInteractionToolPinchPressed = "EventPinchInteractionToolPinchPressed";
		public const string EventPinchInteractionToolPinchReleased = "EventPinchInteractionToolPinchReleased";
		public const string EventPinchInteractionToolPinchMantained = "EventPinchInteractionToolPinchMantained";
		public const string EventPinchInteractionToolRequestRay = "EventPinchInteractionToolRequestRay";
		public const string EventPinchInteractionToolResponseRay = "EventPinchInteractionToolResponseRay";

		private const float TIME_FOR_HAND_TRIGGER = 2f;

		[Tooltip("Tick for the right hand; leave unticked for the left. Can also be set at runtime before Initialize().")]
		[SerializeField] private bool _isRightHand = false;

#if ENABLE_OCULUS
		[Tooltip("OVRHand for this hand. Leave empty for runtime tools — it is resolved by handedness from OculusHandsManager.")]
		[SerializeField] private OVRHand _ovrHand = null;

		[Tooltip("Finger used to detect the pinch. Index reproduces the previous default.")]
		[SerializeField] private OVRHand.HandFinger _pinchFinger = OVRHand.HandFinger.Index;

		[Tooltip("Ignore pinches while finger tracking confidence is Low. Untick to match the old behaviour exactly.")]
		[SerializeField] private bool _requireHighConfidence = true;

		private bool _isInitialized = false;
		private bool _enabled = false;              // replaces _rayToolView.EnableState
		private bool _previousStatePinch = false;
		private bool _pressedStablePinch = false;
		private float _timeAcumDetectStablePinch = 0f;
		private Vector3 _rotationAcumulated = Vector3.zero;

		// Now settable so the creator can configure a runtime-instantiated tool before Initialize().
		public bool IsRightHandedTool
		{
			get { return _isRightHand; }
			set { _isRightHand = value; }
		}

		// Kept for API compatibility with callers that used _rayToolView.ReferenceRay.
		// Returns the transform whose position/forward define the ray origin — this
		// component's transform, driven each frame from OVRHand.PointerPose.
		public Transform GetLineRender
		{
			get { return transform; }
		}

		public Vector3 RotationAcumulated
		{
			set { _rotationAcumulated = value; }
		}

		private XR_HAND ThisHand
		{
			get { return _isRightHand ? XR_HAND.right : XR_HAND.left; }
		}

		private void Start()
		{
			// Covers static placement. For runtime tools the creator has already called
			// Initialize(); the guard makes this a no-op in that case.
			Initialize();
		}

		// Public so InteractableOculusHandsCreator can call it after setting handedness.
		// Idempotent.
		public void Initialize()
		{
			if (_isInitialized)
			{
				return;
			}
			_isInitialized = true;

			ResolveHand();

			VRInputController.Instance.Event += OnVREvent;
			SystemEventController.Instance.Event += OnSystemEvent;
		}

		private void ResolveHand()
		{
			// Primary path (runtime tools parented to the CameraRig): look up the hand by
			// handedness from OculusHandsManager — the replacement for HandsManager.Instance.RightHand.
			if (_ovrHand == null && OculusHandsManager.Instance != null)
			{
				_ovrHand = _isRightHand ? OculusHandsManager.Instance.RightHand : OculusHandsManager.Instance.LeftHand;
			}
			// Fallbacks for static placement under a hand hierarchy.
			if (_ovrHand == null) _ovrHand = GetComponentInParent<OVRHand>();
			if (_ovrHand == null) _ovrHand = GetComponentInChildren<OVRHand>(true);

			if (_ovrHand == null)
			{
				Debug.LogWarning("[PinchInteractionTool] Could not resolve an OVRHand for the " +
					(_isRightHand ? "right" : "left") + " hand. Ensure OculusHandsManager is initialized first, " +
					"or assign _ovrHand in the inspector.");
			}
		}

		private void OnDestroy()
		{
			if (VRInputController.Instance != null) VRInputController.Instance.Event -= OnVREvent;
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))
			{
				DontDestroyOnLoad(this.gameObject);
			}
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerReleaseAllResources))
			{
				if (this.gameObject != null)
				{
					GameObject.Destroy(this.gameObject);
				}
			}
		}

		private void OnVREvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(OculusHandsManager.EventOculusHandsManagerStateChanged)
				|| nameEvent.Equals(OculusHandsManager.EventOculusHandsManagerStateInited))
			{
				// Was: _rayToolView.EnableState = handTrackingState;
				_enabled = (bool)parameters[0];
			}
			if (nameEvent.Equals(OculusHandsManager.EventOculusHandsManagerRotationCameraApplied))
			{
				bool shouldSet = (bool)parameters[0];
				Vector3 rotationApplied = (Vector3)parameters[1];
				if (shouldSet)
				{
					_rotationAcumulated = rotationApplied;
				}
				else
				{
					_rotationAcumulated += rotationApplied;
				}
			}
			if (nameEvent.Equals(EventPinchInteractionToolRequestRay))
			{
				XR_HAND targetHand = (XR_HAND)parameters[0];
				if (ThisHand == targetHand)
				{
					VRInputController.Instance.DispatchVREvent(EventPinchInteractionToolResponseRay, targetHand, transform);
				}
			}
		}

		private void Update()
		{
			if (_ovrHand == null || !_enabled)
			{
				return;
			}
			if (!_ovrHand.IsTracked)
			{
				return;
			}

			// Keep this transform aligned with the hand's pointer pose so its position and
			// forward describe the ray origin (previously _rayToolView.ReferenceRay).
			// Optional refinement: also gate on _ovrHand.IsPointerPoseValid if your SDK
			// version exposes it — omitted so this compiles against any recent version.
			Transform pointer = _ovrHand.PointerPose;
			if (pointer != null)
			{
				transform.position = pointer.position;
				transform.rotation = Quaternion.Euler(_rotationAcumulated.x, _rotationAcumulated.y, _rotationAcumulated.z) * pointer.rotation;
			}

			// Pinch state, straight from OVRHand (no bone capsules, no focused-object gate).
			bool isPinching = _ovrHand.GetFingerIsPinching(_pinchFinger);
			if (_requireHighConfidence
				&& _ovrHand.GetFingerConfidence(_pinchFinger) != OVRHand.TrackingConfidence.High)
			{
				isPinching = false;
			}

			// Pressed / released edges.
			if (!_previousStatePinch && isPinching)
			{
				VRInputController.Instance.DispatchVREvent(EventPinchInteractionToolPinchPressed, ThisHand, transform, transform);
			}
			else if (_previousStatePinch && !isPinching)
			{
				VRInputController.Instance.DispatchVREvent(EventPinchInteractionToolPinchReleased, ThisHand, transform, transform);
				if (_pressedStablePinch)
				{
					VRInputController.Instance.DispatchVREvent(EventPinchInteractionToolPinchMantained, false, ThisHand);
				}
			}
			_previousStatePinch = isPinching;

			// Stable (held) pinch detection — preserves the original timing behaviour,
			// including that the accumulator is NOT reset when a short pinch is released
			// before reaching the threshold.
			if (_pressedStablePinch)
			{
				if (!isPinching)
				{
					_pressedStablePinch = false;
				}
			}
			else
			{
				if (isPinching)
				{
					_timeAcumDetectStablePinch += Time.deltaTime;
					if (_timeAcumDetectStablePinch > TIME_FOR_HAND_TRIGGER)
					{
						_timeAcumDetectStablePinch = 0f;
						_pressedStablePinch = true;
						VRInputController.Instance.DispatchVREvent(EventPinchInteractionToolPinchMantained, true, ThisHand, transform.gameObject, true, true);
					}
				}
			}
		}
#endif
	}
}

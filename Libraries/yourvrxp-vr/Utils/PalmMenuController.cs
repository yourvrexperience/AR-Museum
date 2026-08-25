using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
#if ENABLE_ULTIMATEXR
using UltimateXR.UI.UnityInputModule;
#endif
#if ENABLE_OPENXR
using UnityEngine.XR.Interaction.Toolkit.UI;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.VR
{
	public class PalmMenuController : MonoBehaviour
	{
		public const string EventPalmMenuControllerShow = "EventPalmMenuControllerShow";

		public const string LAYER_HAND = "Hand";

		[SerializeField] private GameObject _facePointerDetector;
		[SerializeField] private GameObject _visualsContainer;

		protected FacePointerDetector _leftHandCollider;
		protected FacePointerDetector _rightHandCollider;

		protected bool _isHandTrackingMode = false;
		protected bool _isActiveRight = false;
		protected bool _isActiveLeft = false;
		protected Quaternion _initialRotation;
		protected bool _allowMenuActivation = true;

		protected virtual void Start()
		{
#if ENABLE_OCULUS
			if (_visualsContainer.GetComponent<OVRRaycaster>() == null) _visualsContainer.AddComponent<OVRRaycaster>();
#elif ENABLE_OPENXR
			if (_visualsContainer.GetComponent<TrackedDeviceGraphicRaycaster>() == null) _visualsContainer.AddComponent<TrackedDeviceGraphicRaycaster>();
#elif ENABLE_ULTIMATEXR
			if (_visualsContainer.GetComponent<UxrCanvas>() == null) _visualsContainer.AddComponent<UxrCanvas>();
			if (_visualsContainer.GetComponent<UxrLaserPointerRaycaster>() == null) _visualsContainer.AddComponent<UxrLaserPointerRaycaster>();
			_visualsContainer.GetComponentInChildren<Canvas>().renderMode = RenderMode.WorldSpace;			
			UpdateUxrInteraction(!VRInputController.Instance.VRController.HandTrackingActive);
#endif
			_initialRotation = _visualsContainer.transform.localRotation;

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR	|| ENABLE_NREAL
			if ((VRInputController.Instance != null) && (VRInputController.Instance.VRController.HeadController != null))
			{
				// Left hand detector
				_leftHandCollider = Instantiate(_facePointerDetector).GetComponent<FacePointerDetector>();
				_leftHandCollider.transform.parent = VRInputController.Instance.VRController.HandLeftController.transform;
				_leftHandCollider.DetectedFaceCollision += OnDectectedLeftHandCollision;
				_leftHandCollider.transform.localPosition = Vector3.zero;
				_leftHandCollider.transform.localRotation = Quaternion.identity;

				// Right hand detector
				_rightHandCollider = Instantiate(_facePointerDetector).GetComponent<FacePointerDetector>();
				_rightHandCollider.transform.parent = VRInputController.Instance.VRController.HandRightController.transform;
				_rightHandCollider.DetectedFaceCollision += OnDectectedRightHandCollision;
				_rightHandCollider.transform.localPosition = Vector3.zero;
				_rightHandCollider.transform.localRotation = Quaternion.identity;
				_rightHandCollider.transform.Rotate(new Vector3(0,180,0), Space.Self);

				_visualsContainer.SetActive(false);
			}
			VRInputController.Instance.Event += OnVREvent;
#else			
			_visualsContainer.SetActive(false);
#endif			
			SystemEventController.Instance.Event += OnSystemEvent;
		}

		protected virtual void OnDestroy()
		{
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR	|| ENABLE_NREAL
			if (VRInputController.Instance != null)
			{
				VRInputController.Instance.Event -= OnVREvent;
			}
#endif						
			if (SystemEventController.Instance != null)
			{
				SystemEventController.Instance.Event -= OnSystemEvent;
			}
		}

#if ENABLE_ULTIMATEXR
		private void UpdateUxrInteraction(bool isController)
		{
			if (isController)
			{
				_visualsContainer.GetComponent<UxrCanvas>().CanvasInteractionType = UxrInteractionType.LaserPointers;
			}
			else
			{
				_visualsContainer.GetComponent<UxrCanvas>().CanvasInteractionType = UxrInteractionType.FingerTips;
			}			
		}
#endif		

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR	|| ENABLE_NREAL
		protected virtual void OnVREvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(VRInputController.EventVRInputControllerChangedHandTrackingState))
			{
				bool isController = (bool)parameters[0];
#if ENABLE_ULTIMATEXR				
				UpdateUxrInteraction(isController);
#endif				
			}
		}
#endif				

		protected virtual void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(EventPalmMenuControllerShow))
			{
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR	|| ENABLE_NREAL		
				_visualsContainer.transform.forward = VRInputController.Instance.VRController.HeadController.transform.forward;
				_visualsContainer.SetActive(true);
#endif			
			}
		}

		protected virtual void ActivationHandMenu(GameObject menuPosition)
		{
			this.transform.parent = menuPosition.transform;
			this.transform.position = menuPosition.transform.position;
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR	|| ENABLE_NREAL		
			_visualsContainer.transform.forward = VRInputController.Instance.VRController.HeadController.transform.forward;
			_visualsContainer.SetActive(true);
#endif			
		}

		protected virtual void DeactivationHandMenu()
		{
			this.transform.parent = null;
			_visualsContainer.SetActive(false);
			_visualsContainer.transform.localRotation = _initialRotation;
		}

		protected virtual void OnDectectedRightHandCollision(bool detected, GameObject menuPosition)
		{
			if (!_isHandTrackingMode && _allowMenuActivation)
			{
				if (detected && !_isActiveLeft)
				{
					ActivationHandMenu(menuPosition);
					_isActiveRight = true;
				}
				else
				{
					if (_isActiveRight)
					{
						DeactivationHandMenu();
						_isActiveRight = false;
					}
				}
			}
		}

		protected virtual void OnDectectedLeftHandCollision(bool detected, GameObject menuPosition)
		{
			if (!_isHandTrackingMode && _allowMenuActivation)
			{
				if (detected && !_isActiveRight)
				{
					ActivationHandMenu(menuPosition);
					_isActiveLeft = true;
				}
				else
				{
					if (_isActiveLeft)
					{
						DeactivationHandMenu();
						_isActiveLeft = false;
					}
				}
			}
		}

		protected virtual void Update()
		{
			if (_isActiveRight || _isActiveLeft)
			{
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR	|| ENABLE_NREAL
				_visualsContainer.transform.forward = -(VRInputController.Instance.VRController.HeadController.transform.position - this.transform.position).normalized;
#endif				
			}		
		}
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.VR
{
	public class InteractableOculusHandsCreator : MonoBehaviour
	{
		public const string EventInteractableOculusHandsCreatorStarted = "EventInteractableOculusHandsCreatorStarted";

		public Transform CameraRig;
		[SerializeField] private Transform[] LeftHandTools = null;
		[SerializeField] private Transform[] RightHandTools = null;

#if ENABLE_OCULUS
		private bool _initedLeft = false;
		private bool _initedRight = false;
		private bool _initedGeneral = false;
		private Vector3 _rotationAcumulated = Vector3.zero;

		private List<Transform> m_toolInstances = new List<Transform>();

		void Start()
		{
			VRInputController.Instance.DispatchVREvent(EventInteractableOculusHandsCreatorStarted, this);
		}

		public void Initialize()
		{
			CameraRig = OculusController.Instance.transform;

			if (!_initedGeneral)
			{
				_initedGeneral = true;
				VRInputController.Instance.Event += OnVREvent;
			}

			if (LeftHandTools != null && LeftHandTools.Length > 0 && !_initedLeft)
			{
				_initedLeft = true;
				StartCoroutine(AttachToolsToHands(LeftHandTools, false));
			}

			if (RightHandTools != null && RightHandTools.Length > 0 && !_initedRight)
			{
				_initedRight = true;
				StartCoroutine(AttachToolsToHands(RightHandTools, true));
			}
		}

		void OnDestroy()
		{
			if (VRInputController.Instance != null) VRInputController.Instance.Event -= OnVREvent;
		}

		private IEnumerator AttachToolsToHands(Transform[] toolObjects, bool isRightHand)
		{
			// Replaces the old wait on HandsManager.IsInitialized() + skeleton bones.
			// The tools no longer attach to bone capsules, so we only need the relevant
			// OVRHand to be resolved and available on OculusHandsManager before we
			// instantiate and initialize them.
			while (OculusHandsManager.Instance == null
				|| (isRightHand ? OculusHandsManager.Instance.RightHand : OculusHandsManager.Instance.LeftHand) == null)
			{
				yield return null;
			}

			// De-dupe the tool list (was a HashSet in the original).
			HashSet<Transform> toolObjectSet = new HashSet<Transform>();
			foreach (Transform toolTransform in toolObjects)
			{
				if (toolTransform != null) toolObjectSet.Add(toolTransform);
			}

			foreach (Transform toolObject in toolObjectSet)
			{
				AttachToolToHandTransform(toolObject, isRightHand);
			}
		}

		private void OnVREvent(string nameEvent, object[] parameters)
		{
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
				foreach (Transform tool in m_toolInstances)
				{
					if (shouldSet)
					{
						tool.rotation = Quaternion.identity;
					}
					tool.Rotate(rotationApplied);
				}
			}
		}

		private void AttachToolToHandTransform(Transform tool, bool isRightHanded)
		{
			var newTool = Instantiate(tool).transform;
			newTool.SetParent(CameraRig, false);
			newTool.localPosition = Vector3.zero;

			PinchInteractionTool toolComp = newTool.GetComponent<PinchInteractionTool>();
			if (toolComp != null)
			{
				// Set handedness BEFORE Initialize() so the tool resolves the correct
				// OVRHand from OculusHandsManager by handedness.
				toolComp.IsRightHandedTool = isRightHanded;
				toolComp.Initialize();
				toolComp.RotationAcumulated = _rotationAcumulated;
			}

			FingerInteractionRadius fingerRadius = newTool.GetComponentInChildren<FingerInteractionRadius>();
			if (fingerRadius != null)
			{
				fingerRadius.Hand = (isRightHanded ? XR_HAND.right : XR_HAND.left);
			}

			m_toolInstances.Add(newTool);
		}
#endif
	}
}

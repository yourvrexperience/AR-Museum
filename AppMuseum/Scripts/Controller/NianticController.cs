using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;
using System;
#if ENABLE_NIANTIC
using System.Threading.Tasks;
using NianticSpatial.NSDK.AR.VPS2;
using NianticSpatial.NSDK.AR.Subsystems;
using NianticSpatial.NSDK.AR.XRSubsystems;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;
#endif

namespace yourvrexperience.template6dof
{
    public class NianticController : MonoBehaviour
	{
		private static NianticController _instance;

        public static NianticController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(NianticController)) as NianticController;
                }
                return _instance;
            }
        }
		 		
#if ENABLE_NIANTIC		 		
		[SerializeField] private ARVps2Manager arVps2Manager;
#endif		
		[SerializeField] private GameObject anchorMarkerPrefab;
		[SerializeField] private Camera arNianticCamera;
		[SerializeField] private BoxCollider cameraVision;

		private bool _enableDetection = false;
		private bool _hasAreaBeenDetected = false;

		private string _levelAnchorPayload;
#if ENABLE_NIANTIC		
		private ARVps2Anchor _anchor;		
#endif		
		private GameObject _coarseMarker;
		private bool _isAnchorSet;

		public bool HasAreaBeenDetected
        {
			get { return _hasAreaBeenDetected;  }
			set { 
				_hasAreaBeenDetected = value;  
				if (_hasAreaBeenDetected)
				{
					MainController.Instance.ApplyOclusionNavigation();
					SystemEventController.Instance.DispatchSystemEvent(ARMaxSTController.EventARMaxSTControllerAreaRecognized);
				}
				else
				{
					SystemEventController.Instance.DispatchSystemEvent(ARMaxSTController.EventARMaxSTControllerAreaLost);
				}
			}
        }
		public Camera ARNianticCamera
        {
			get { return arNianticCamera;  }
			set { arNianticCamera = value; }
        }
		public string LevelAnchorPayload
		{
			get { return _levelAnchorPayload;  }
			set { _levelAnchorPayload = value; }
		}
#if ENABLE_NIANTIC		
		public ARVps2Anchor Anchor
		{
			get { return _anchor;  }
		}
#endif	
		void Start()
		{
			SystemEventController.Instance.Event += OnSystemEvent;
		}

        void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		public void Stop()
		{
			_enableDetection = false;
			_hasAreaBeenDetected = false;
			_isAnchorSet = false;
			if (_coarseMarker != null)
			{
				GameObject.Destroy(_coarseMarker);
				_coarseMarker = null;
			}
		}

		public bool CheckVisiblePoint(Vector3 position)
		{
			return cameraVision.bounds.Contains(position);
		}

#if ENABLE_NIANTIC
		private System.Collections.IEnumerator AutoLoadWhenReady(string payload)
		{
			while (!_isAnchorSet)
			{
				TrackAnchorFromPayload(payload);
				if (_isAnchorSet) yield break;
				yield return new WaitForSeconds(0.5f);
			}
		}

		private void TrackAnchorFromPayload(string payload)
		{
			// Check arguments
			if (string.IsNullOrEmpty(payload))
			{
				Debug.LogError("Niantic: The selected location does not have a default anchor");
				return;
			}

			// Create the anchor
			_isAnchorSet = arVps2Manager.TryTrackAnchor(
				anchorPayload: payload,
				anchorOut: out _anchor);

			if (!_isAnchorSet)
			{
				Debug.LogError("Niantic: Failed to track anchor");
				return;
			}			

			// Instantiate the debug anchor visualization
			if (anchorMarkerPrefab != null)
			{
				_coarseMarker = Instantiate(anchorMarkerPrefab, _anchor.transform, true);
				_coarseMarker.transform.localPosition = Vector3.zero;
				_coarseMarker.transform.localRotation = Quaternion.identity;
				_coarseMarker.transform.localScale = Vector3.one;
			}

			HasAreaBeenDetected = true;
			_enableDetection = true;
		}
#endif

		public Vector3 DesignToWorldPoint(Vector3 designPoint)
		{
#if ENABLE_NIANTIC
			if (_isAnchorSet && _anchor != null)
				return _anchor.transform.TransformPoint(designPoint);
#endif
			return designPoint;
		}

		public Vector3 WorldToDesignPoint(Vector3 worldPoint)
		{
#if ENABLE_NIANTIC
			if (_isAnchorSet && _anchor != null)
				return _anchor.transform.InverseTransformPoint(worldPoint);
#endif
			return worldPoint;
		}

		public Vector3 DesignToWorldDirection(Vector3 designDir)
		{
#if ENABLE_NIANTIC
			if (_isAnchorSet && _anchor != null)
				return _anchor.transform.TransformDirection(designDir);
#endif
			return designDir;
		}

		public Vector3 WorldToDesignDirection(Vector3 worldDir)
		{
#if ENABLE_NIANTIC
			if (_isAnchorSet && _anchor != null)
				return _anchor.transform.InverseTransformDirection(worldDir);
#endif
			return worldDir;
		}

		public Quaternion DesignToWorldRotation(Quaternion designRot)
		{
#if ENABLE_NIANTIC
			if (_isAnchorSet && _anchor != null)
				return _anchor.transform.rotation * designRot;
#endif
			return designRot;
		}

		public Quaternion WorldToDesignRotation(Quaternion worldRot)
		{
#if ENABLE_NIANTIC
			if (_isAnchorSet && _anchor != null)
				return Quaternion.Inverse(_anchor.transform.rotation) * worldRot;
#endif
			return worldRot;
		}

		// Convenience: place a transform directly from design-space coordinates
		public void PlaceFromDesign(Transform t, Vector3 designPos, Quaternion designRot)
		{
			t.position = DesignToWorldPoint(designPos);
			t.rotation = DesignToWorldRotation(designRot);
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (!this.gameObject.activeSelf) return;

			if (nameEvent.Equals(LevelView.EventLevelViewStarted))
			{
				if (MainController.Instance.EnableEditionPOIs)
				{
					SystemEventController.Instance.DispatchSystemEvent(LevelView.EventLevelViewForcePOIsVisible);
				}
				Debug.LogError($"Niantic: Auto-loading anchor with payload: {_levelAnchorPayload}");
				
#if UNITY_EDITOR
				_hasAreaBeenDetected = true;
#elif ENABLE_NIANTIC
				_levelAnchorPayload = (string)parameters[1];
				StartCoroutine(AutoLoadWhenReady(_levelAnchorPayload));
#endif
			}
			if (nameEvent.Equals(MainController.EventMainControllerReleaseGameResources))
			{
				Stop();
			}
        }
		
		private void Update()
    	{
			if (_enableDetection)
			{
#if ENABLE_NIANTIC				
				if (!arVps2Manager.TryGetLatestLocalization(out var localization))
				{
					if (HasAreaBeenDetected)
					{
						HasAreaBeenDetected = false;
					}
					return;
				}
				if (!HasAreaBeenDetected)
				{
					HasAreaBeenDetected = true;
				}
#endif				
			}
		}
    }
}
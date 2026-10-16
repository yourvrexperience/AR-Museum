using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;
using yourvrexperience.Narration;
using System;
#if ENABLE_NIANTIC || ENABLE_NIANTICXR
using System.Threading.Tasks;
using NianticSpatial.NSDK.AR;
using NianticSpatial.NSDK.AR.VPS2;
using NianticSpatial.NSDK.AR.Subsystems;
using NianticSpatial.NSDK.AR.XRSubsystems;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;
using System.Globalization;
#endif

namespace yourvrexperience.template6dof
{
    public class NianticController : MonoBehaviour
	{
#if ENABLE_NIANTIC || ENABLE_NIANTICXR
		public static class GeolocationSpaceConverter
		{
			private static Transform Area => NavMeshController.Instance.AreaMaxST.transform;

			private static Transform _anchor;
			private static Vector3 _poiLocal;

			public static Transform Anchor => _anchor;

			// Call ONCE when the interaction with a POI starts
			public static Transform CaptureAnchor(Transform poiRoot, Transform userCamera)
			{
				if (_anchor == null) _anchor = new GameObject("GeoPOIAnchor").transform;
				_anchor.SetParent(Area, false);   // lives in area space, so area scale/rotation are handled for free

				_poiLocal = Area.InverseTransformPoint(poiRoot.position);

				// Direction the user is facing toward the POI, flattened in area space
				Vector3 dirLocal = Area.InverseTransformDirection(poiRoot.position - userCamera.position);
				dirLocal.y = 0;
				if (dirLocal.sqrMagnitude < 1e-4f)   // user standing on the POI: fall back to the view direction
				{
					dirLocal = Area.InverseTransformDirection(userCamera.forward);
					dirLocal.y = 0;
				}
				if (dirLocal.sqrMagnitude < 1e-6f) dirLocal = Vector3.forward;

				_anchor.localPosition = _poiLocal;
				_anchor.localRotation = Quaternion.LookRotation(dirLocal.normalized, Vector3.up);
				_anchor.localScale = Vector3.one;
				return _anchor;
			}

			// Stored data (area coords, canonical POI frame) -> local pose under the anchor
			public static void StoredToAnchor(Vector3 posArea, Quaternion rotArea, out Vector3 posLocal, out Quaternion rotLocal)
			{
				posLocal = posArea - _poiLocal;
				rotLocal = rotArea;
			}

			// Object placed in the world while editing -> stored data
			public static void WorldToStored(Transform obj, out Vector3 posArea, out Quaternion rotArea)
			{
				Vector3 posLocal = _anchor.InverseTransformPoint(obj.position);
				Quaternion rotLocal = Quaternion.Inverse(_anchor.rotation) * obj.rotation;
				posArea = posLocal + _poiLocal;
				rotArea = rotLocal;
			}
		}
#endif
		private const double EarthRadiusMeters = 6378137.0; // WGS84 equatorial radius

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

		[Header("Niantic")]
		 		
#if ENABLE_NIANTIC || ENABLE_NIANTICXR		 		
		[SerializeField] private ARVps2Manager arVps2Manager;
#endif		
		[SerializeField] private GameObject anchorMarkerPrefab;
		[SerializeField] private Camera arNianticCamera;
		[SerializeField] private BoxCollider cameraVision;

        [Header("UI")] 
        [SerializeField] 
        private bool _enableInformation;

        [SerializeField] 
        private GameObject _panelInformation;

#if ENABLE_NIANTIC || ENABLE_NIANTICXR
        [SerializeField] 
        private Text _localizationStatusText;
        
        [SerializeField]
        private Text _anchorStatusText;

        [SerializeField]
        private Text _geolocationText;

        [SerializeField]
        private Text _cameraPositionText;

       [SerializeField]
        private Text _extraInfoText;
#endif		

		private bool _enableDetection = false;
		private bool _hasAreaBeenDetected = false;

		private bool _usePayload = true;
		private string _levelAnchorPayload;
#if ENABLE_NIANTIC || ENABLE_NIANTICXR
		private ARVps2Anchor _anchor;		
		private XRVps2Localization _currentLocalization;
#endif		
		private GameObject _coarseMarker;
		private bool _isAnchorSet;
		private GeoLocation _currentGeoLocation;
		private Coroutine _autoLoadCoroutine;		
		private Vector3 _originUnityPos;
		private GeoLocation _originGeo;
		private Quaternion _geoToWorld;
		private bool _isCalibrated = false;

		public bool HasAreaBeenDetected
        {
			get { return _hasAreaBeenDetected;  }
			set { 
				_hasAreaBeenDetected = value;  
				if (_hasAreaBeenDetected)
				{
					MainController.Instance.ApplyOclusionNavigation();
					SystemEventController.Instance.DispatchSystemEvent(ARMaxSTController.EventARMaxSTControllerAreaRecognized);
					SystemEventController.Instance.DelaySystemEvent(LevelView.EventLevelViewRunGeoCalibration, 4);
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
		public bool UsePayload
		{
			get { return _usePayload;  }
			set { _usePayload = value; }
		}
		public GeoLocation CurrentGeoLocation
		{
			get { return _currentGeoLocation;  }
			set { _currentGeoLocation = value; }
		}
		public bool IsCalibrated
		{
			get { return _isCalibrated; }
		}
#if ENABLE_NIANTIC || ENABLE_NIANTICXR		
		public Transform Anchor
		{
			get { 
				if (_anchor != null)
				{
					return _anchor.transform;  
				}
				else
				{
					return null;
				}				
			}
		}
#endif	
		void Start()
		{
			_panelInformation.SetActive(_enableInformation);
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
			if (_autoLoadCoroutine != null)
			{
				StopCoroutine(_autoLoadCoroutine);
				_autoLoadCoroutine = null;
			}
#if ENABLE_NIANTIC || ENABLE_NIANTICXR					
			if (_anchor != null)
			{
				_anchor = null;
			}
#endif			
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

		private bool UpdateGeolocation()
		{
#if ENABLE_NIANTIC || ENABLE_NIANTICXR			
			// UPDATE THE CURRENT GEOLOCATION
			if (arVps2Manager.TryGetDeviceGeolocation(out XRVps2Geolocation deviceGeo, HeadingMode.CameraDirection))
			{
				_currentGeoLocation.Latitude  = deviceGeo.Geolocation.Latitude;
				_currentGeoLocation.Longitude = deviceGeo.Geolocation.Longitude;
				_currentGeoLocation.Altitude  = deviceGeo.Geolocation.Altitude;
				_currentGeoLocation.Heading  = deviceGeo.Geolocation.Heading;
				return true;
			}				
			else
			{
				return false;
			}	
#else
			return false;
#endif			
		}

		public System.Collections.IEnumerator CalibrationGeoOrigin()
		{
			while (!UpdateGeolocation())
				yield return new WaitForSeconds(0.5f);

			CalibrateGeoOrigin(); // only with a valid, fresh sample
		}

		// Call once, after GPS and compass have stabilized
		private void CalibrateGeoOrigin()
		{
			_isCalibrated = true;
			_originGeo = CurrentGeoLocation;
			_originUnityPos = Camera.main.transform.position;

			float cameraYaw = Camera.main.transform.eulerAngles.y;
			float heading   = (float)_originGeo.Heading;   // must be sampled at the same moment
			_geoToWorld = Quaternion.Euler(0f, cameraYaw - heading, 0f);
		}

		public Vector3 GeoToWorld(GeoLocation poi)
		{
			Vector3 enu = GetPOIPositionFromGeolocation(poi, _originGeo);
			Vector3 pos = _originUnityPos + _geoToWorld * enu;
			pos.y = _originUnityPos.y; // or your ground height, since altitude is ignored
			return pos;
		}

		public Vector3 GetPOIPositionFromGeolocation(GeoLocation poi, GeoLocation origin)
		{
			double latRad = origin.Latitude * Math.PI / 180.0;

			double dLat = (poi.Latitude - origin.Latitude) * Math.PI / 180.0;
			double dLon = (poi.Longitude - origin.Longitude) * Math.PI / 180.0;

			double north = dLat * EarthRadiusMeters;
			double east  = dLon * EarthRadiusMeters * Math.Cos(latRad);

			return new Vector3((float)east, 0f, (float)north); // pure offset, no rotation, no camera
		}

#if ENABLE_NIANTIC || ENABLE_NIANTICXR
		private System.Collections.IEnumerator AutoLoadWhenReady(string payload)
		{
			while (!_isAnchorSet)
			{
				Debug.Log($"Auto-loading anchor with payload: {payload}");
                if (_cameraPositionText != null) _cameraPositionText.text = payload;				
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

			// _anchor = null;  // FOR TESTING: PLEASE REMOVE
			if (_anchor != null)
			{
				// Instantiate the debug anchor visualization
				if (anchorMarkerPrefab != null)
				{
					_coarseMarker = Instantiate(anchorMarkerPrefab, _anchor.transform, true);
					_coarseMarker.transform.localPosition = Vector3.zero;
					_coarseMarker.transform.localRotation = Quaternion.identity;
					_coarseMarker.transform.localScale = Vector3.one;
				}
			}

			HasAreaBeenDetected = true;
			_enableDetection = true;

			if (_enableInformation)
			{
				if (_cameraPositionText != null) _cameraPositionText.text =  "AREA RECOGNISED";
				if (_localizationStatusText != null) _localizationStatusText.text =  "AREA RECOGNISED";
				if (_anchorStatusText != null) _anchorStatusText.text =  "AREA RECOGNISED";
				if (_geolocationText != null) _geolocationText.text =  "AREA RECOGNISED";
			}
		}

		private System.Collections.IEnumerator AutoLoadGeolocationWhenReady()
		{
			while (!_isAnchorSet)
			{
				TrackAnchorFromGeolocation();
				if (_isAnchorSet) yield break;
				yield return new WaitForSeconds(0.5f);
			}
		}

		private void TrackAnchorFromGeolocation()
		{
			if (arVps2Manager.TryGetLatestLocalization(out var localization) &&
            	localization.TrackingState != Vps2TrackingState.Unavailable)
        	{
				_isAnchorSet = true;				
				_enableDetection = true;
				HasAreaBeenDetected = true;
				Debug.Log("GeoPositionedObjectHelper: applied latest pose to GameObject.");
			}
		}
#endif

		public Vector3 DesignToWorldPoint(Vector3 designPoint)
		{
#if ENABLE_NIANTIC || ENABLE_NIANTICXR
			if (_isAnchorSet && Anchor != null)
				return Anchor.TransformPoint(designPoint);
#endif
			return designPoint;
		}

		public Vector3 WorldToDesignPoint(Vector3 worldPoint)
		{
#if ENABLE_NIANTIC || ENABLE_NIANTICXR
			if (_isAnchorSet && Anchor != null)
				return Anchor.InverseTransformPoint(worldPoint);
#endif
			return worldPoint;
		}

		public Vector3 DesignToWorldDirection(Vector3 designDir)
		{
#if ENABLE_NIANTIC || ENABLE_NIANTICXR
			if (_isAnchorSet && Anchor != null)
				return Anchor.TransformDirection(designDir);
#endif
			return designDir;
		}

		public Vector3 WorldToDesignDirection(Vector3 worldDir)
		{
#if ENABLE_NIANTIC || ENABLE_NIANTICXR
			if (_isAnchorSet && Anchor != null)
				return Anchor.InverseTransformDirection(worldDir);
#endif
			return worldDir;
		}

		public Quaternion DesignToWorldRotation(Quaternion designRot)
		{
#if ENABLE_NIANTIC || ENABLE_NIANTICXR
			if (_isAnchorSet && Anchor != null)
				return Anchor.rotation * designRot;
#endif
			return designRot;
		}

		public Quaternion WorldToDesignRotation(Quaternion worldRot)
		{
#if ENABLE_NIANTIC || ENABLE_NIANTICXR
			if (_isAnchorSet && Anchor != null)
				return Quaternion.Inverse(Anchor.rotation) * worldRot;
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

				if (_enableInformation)
				{
#if ENABLE_NIANTIC || ENABLE_NIANTICXR					
					if (_localizationStatusText != null) _localizationStatusText.text = "VPS2 Tracking State: UNAVAILABLE";
					if (_anchorStatusText != null) _anchorStatusText.text = "Anchor Tracking State: NOT TRACKED";
					if (_geolocationText != null) _geolocationText.text = "Anchor Geolocation: N/A";
#endif					
				}

#if UNITY_EDITOR
				_hasAreaBeenDetected = true;
#elif ENABLE_NIANTIC || ENABLE_NIANTICXR
				_levelAnchorPayload = (string)parameters[1];
				// FOR TESTING: PLEASE REMOVE
				// _usePayload = false;
				// _autoLoadCoroutine = StartCoroutine(AutoLoadWhenReady("ChUIsuHshKG9qKfgARCT38WHrYuJ+iYYrLvy4PwzKicKFQiR37nq9sPGp30QvJ+rpfjAhfSXARIJCgASBSUAAIA/HQAAgD8yEhFbiqa2vb5EQBkVEveTbi4BQA=="));					
				if (_levelAnchorPayload == null || _levelAnchorPayload.Length == 0)
				{
					_usePayload = false;
					Debug.LogError($"Niantic: Auto-loading anchor with geolocation");
					_autoLoadCoroutine = StartCoroutine(AutoLoadGeolocationWhenReady());						
				}
				else
				{
					_usePayload = true;
					Debug.LogError($"Niantic: Auto-loading anchor with payload: {_levelAnchorPayload}");
					_autoLoadCoroutine = StartCoroutine(AutoLoadWhenReady(_levelAnchorPayload));					
				}
#endif
			}
			if (nameEvent.Equals(MainController.EventMainControllerReleaseGameResources))
			{
				Stop();
			}
        }

#if ENABLE_NIANTIC || ENABLE_NIANTICXR			
		private void DisplayDebugInformation(XRVps2Localization localization)
		{
			if (!_enableInformation) return;
			if ((_cameraPositionText == null) || (_localizationStatusText == null) || (_anchorStatusText == null) || (_geolocationText == null))
			{
				return;
			}
			_cameraPositionText.text =  $"Camera Position: {Camera.main.transform.position}";

            // Update the localization status label
            _localizationStatusText.text = localization.TrackingState switch
            {
                Vps2TrackingState.Unavailable => "VPS2 Tracking State: NOT TRACKING",
                Vps2TrackingState.Coarse => "VPS2 Tracking State: COARSE",
                Vps2TrackingState.Precise => "VPS2 Tracking State: PRECISE",
                _ => throw new ArgumentOutOfRangeException()
            };

            if (_isAnchorSet)
            {
				if (_usePayload)
				{
					if (_anchor != null)
					{
						_anchorStatusText.text = _anchor.trackingState switch
						{
							TrackingState.None => $"Anchor Tracking State: NONE\n(Reason: {_anchor.trackingStateReason})",
							TrackingState.Limited => $"Anchor Tracking State: LIMITED\n(Reason: {_anchor.trackingStateReason})",
							TrackingState.Tracking => "Anchor Tracking State: TRACKING",
							_ => throw new ArgumentOutOfRangeException()
						};
						var geo = _anchor.geolocation;
						if (geo.HasValue && _anchor.trackingState == TrackingState.Tracking)
						{
							_geolocationText.text = $"Anchor geolocation: {geo.Value.Latitude:F5}, {geo.Value.Longitude:F5}, {geo.Value.Altitude:F5}, {geo.Value.Heading:F5}";
						}
						else
						{
							_geolocationText.text = "Anchor geolocation: N/A";
						}
					}
				}
				else
				{
					_anchorStatusText.text = "Anchor Tracking State: GEOLOCATION";
					_geolocationText.text = $"Anchor geolocation: {_currentGeoLocation.Latitude:F5}, {_currentGeoLocation.Longitude :F5}, {_currentGeoLocation.Heading:F5}";
				}
            }			
		}
#endif		

		public void SetExtraInfo(string info)
		{			
			if (!_enableInformation) return;
#if ENABLE_NIANTIC || ENABLE_NIANTICXR			
			if (_extraInfoText != null)
			{
				_extraInfoText.text = info;
			}
#endif			
		}

		private void Update()
    	{
			if (_enableDetection)
			{
#if ENABLE_NIANTIC || ENABLE_NIANTICXR			
				if (!arVps2Manager.TryGetLatestLocalization(out var _currentLocalization) || 
					_currentLocalization.TrackingState == Vps2TrackingState.Unavailable)
				{
					if (HasAreaBeenDetected) HasAreaBeenDetected = false;
					return;
				}
				if (!HasAreaBeenDetected)
				{
					HasAreaBeenDetected = true;
				}
								
				UpdateGeolocation();
				DisplayDebugInformation(_currentLocalization);
#endif				
			}
		}
    }
}
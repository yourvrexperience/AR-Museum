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
		private double _levelAnchorLatitude;
		private double _levelAnchorLongitude;
		private double _levelAnchorAltitude;
#if ENABLE_NIANTIC || ENABLE_NIANTICXR
		private ARVps2Anchor _anchor;		
		private XRVps2Localization _currentLocalization;
#endif		
		private GameObject _anchorGeolocation;
		private GameObject _coarseMarker;
		private bool _isAnchorSet;
		private GeoLocation _currentGeoLocation;
		private Coroutine _autoLoadCoroutine;		

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
					if (_anchorGeolocation == null)
					{
						return null;
					}
					else
					{
						return _anchorGeolocation.transform;
					}					
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
			if (_anchorGeolocation != null)
			{
				GameObject.Destroy(_anchorGeolocation);
				_anchorGeolocation = null;
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
				if (_geolocationText != null) _geolocationText.text =  _levelAnchorLatitude + ", " + _levelAnchorLongitude + ", " + _levelAnchorAltitude;
				TrackAnchorFromGeolocation();
				if (_isAnchorSet) yield break;
				yield return new WaitForSeconds(0.5f);
			}
		}

		private void TrackAnchorFromGeolocation()
		{
			if (arVps2Manager.TryGetLatestLocalization(out var localization) &&
            	localization.TrackingState != Vps2TrackingState.Unavailable &&
            	arVps2Manager.TryGetPose(localization,
                                     _levelAnchorLatitude,
                                     _levelAnchorLongitude,
                                     _levelAnchorAltitude,
                                     Quaternion.identity,
                                     out var pose))
        	{
				_isAnchorSet = true;
				HasAreaBeenDetected = true;
				_enableDetection = true;
				_anchorGeolocation = GameObject.CreatePrimitive(PrimitiveType.Cube);
				_anchorGeolocation.transform.SetPositionAndRotation(pose.Pose.position, pose.Pose.rotation);
				_anchorGeolocation.GetComponent<Collider>().enabled = false;
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
				if (_levelAnchorPayload == null || _levelAnchorPayload.Length == 0)
				{
					_usePayload = false;
					var ci = CultureInfo.InvariantCulture;
					if (!double.TryParse((string)parameters[2], NumberStyles.Float, ci, out _levelAnchorLatitude) ||
						!double.TryParse((string)parameters[3], NumberStyles.Float, ci, out _levelAnchorLongitude) ||
						!double.TryParse((string)parameters[4], NumberStyles.Float, ci, out _levelAnchorAltitude))
					{
						Debug.LogError("Niantic: geolocation anchor not parseable");
						return;
					}
					Debug.LogError($"Niantic: Auto-loading anchor with geolocation: {_levelAnchorLatitude}, {_levelAnchorLongitude}, {_levelAnchorAltitude}");
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
						// _geolocationText.text = $"Anchor geolocation: {geo.Value.Latitude:F5}, {geo.Value.Longitude:F5}, {geo.Value.Altitude:F5}";
						_geolocationText.text = $"Anchor GEOLOCATION: {_currentGeoLocation.Latitude}, {_currentGeoLocation.Longitude}, {_currentGeoLocation.Altitude}";
					}
					else
					{
						_geolocationText.text = "Anchor geolocation: N/A";
					}
				}
				else
				{
					_anchorStatusText.text = "Anchor Tracking State: GEOLOCATION";
					_geolocationText.text = $"Anchor geolocation: {_currentGeoLocation.Latitude:F5}, {_currentGeoLocation.Longitude :F5}, {_currentGeoLocation.Altitude:F5}";
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

#if ENABLE_NIANTIC || ENABLE_NIANTICXR
		public void RefreshGeolocationGO(GameObject go, GeoLocation geoLocation)
		{
			if (_currentLocalization.TrackingState == Vps2TrackingState.Unavailable)
			{
				return;
			}
			if (arVps2Manager.TryGetPose(_currentLocalization, 
				geoLocation.Latitude,
				geoLocation.Longitude,
				geoLocation.Altitude, 
				Quaternion.identity, 
				out var pose))
			{
				go.transform.SetPositionAndRotation(pose.Pose.position, pose.Pose.rotation);
			}
		}				
#endif			

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
								
				if (!_usePayload)
				{
					// REFRESH THE ANCHOR
					if (_anchorGeolocation == null)
					{
						_anchorGeolocation = GameObject.CreatePrimitive(PrimitiveType.Cube);
						_anchorGeolocation.GetComponent<Collider>().enabled = false;
					}
					else
					{
						GeoLocation geoLocationAnchor = new GeoLocation();
						geoLocationAnchor.Latitude = _levelAnchorLatitude;
						geoLocationAnchor.Longitude = _levelAnchorLongitude;
						geoLocationAnchor.Altitude = _levelAnchorAltitude;
						geoLocationAnchor.Heading = 0;
						RefreshGeolocationGO(_anchorGeolocation, geoLocationAnchor);
					}
				}

				// UPDATE THE CURRENT GEOLOCATION
				if (arVps2Manager.TryGetDeviceGeolocation(out XRVps2Geolocation deviceGeo, HeadingMode.CameraDirection))
				{
					_currentGeoLocation.Latitude  = deviceGeo.Geolocation.Latitude;
					_currentGeoLocation.Longitude = deviceGeo.Geolocation.Longitude;
					_currentGeoLocation.Altitude  = deviceGeo.Geolocation.Altitude;
					_currentGeoLocation.Heading  = deviceGeo.Geolocation.Heading;
				}					

				DisplayDebugInformation(_currentLocalization);
#endif				
			}
		}
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if !UNITY_WEBGL && ENABLE_MAXST
using maxstAR;
#endif
using yourvrexperience.Utils;

namespace yourvrexperience.template6dof
{
    public class ARMaxSTController : 
#if !UNITY_WEBGL && ENABLE_MAXST	
	ARBehaviour
#else
	MonoBehaviour
#endif
	{
		public const string EventARMaxSTControllerAreaRecognized = "EventARMaxSTControllerAreaRecognized";
		public const string EventARMaxSTControllerAreaLost = "EventARMaxSTControllerAreaLost";		

		private static ARMaxSTController _instance;

        public static ARMaxSTController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(ARMaxSTController)) as ARMaxSTController;
                }
                return _instance;
            }
        }

		[SerializeField] private Camera arMaxSTCamera;
		[SerializeField] private Material occlusionMaterial;
		[SerializeField] private List<GameObject> occlusionObjects = new List<GameObject>();
		[SerializeField] private bool isOcclusion = true;
		[SerializeField] private BoxCollider cameraVision;

#if !UNITY_WEBGL && ENABLE_MAXST
		private Dictionary<string, SpaceTrackableBehaviour> _spaceTrackablesMap = new Dictionary<string, SpaceTrackableBehaviour>();
		private CameraBackgroundBehaviour _cameraBackgroundBehaviour = null;
#endif
		private bool _hasAreaBeenDetected = false;
		private string _filePackageName = "";

#if ENABLE_NREAL
    	private NRCollectYUV _nrCollectYUV = null;
    	private AndroidEngine _androidEngine;
#endif
		public bool HasAreaBeenDetected
        {
			get { return _hasAreaBeenDetected;  }
        }
		public Camera ARMaxSTCamera
        {
			get { return arMaxSTCamera;  }
        }

		void Awake()
		{
#if ENABLE_NREAL			
			_androidEngine = new AndroidEngine();
#endif			
			

#if !UNITY_WEBGL && ENABLE_MAXST
			Init();

			AndroidRuntimePermissions.Permission[] result = AndroidRuntimePermissions.RequestPermissions("android.permission.WRITE_EXTERNAL_STORAGE", "android.permission.CAMERA");
			if (result[0] == AndroidRuntimePermissions.Permission.Granted && result[1] == AndroidRuntimePermissions.Permission.Granted)
				Debug.Log("We have all the permissions!");
			else
				Debug.Log("Some permission(s) are not granted...");


#if !ENABLE_NREAL
			_cameraBackgroundBehaviour = FindObjectOfType<CameraBackgroundBehaviour>();
			if (_cameraBackgroundBehaviour == null)
			{
				Debug.LogError("Can't find CameraBackgroundBehaviour.");
				return;
			}
#endif			
#endif
		}

		void Start()
        {
			if (MainController.Instance.IsARMode)
			{
				QualitySettings.vSyncCount = 0;
#if !ENABLE_NREAL && ENABLE_MAXST
				Application.targetFrameRate = 60;
				StartARMobile();
#endif
			}
		}

		public bool CheckVisiblePoint(Vector3 position)
		{
			return cameraVision.bounds.Contains(position);
		}

		private void StartARMobile()
		{
#if !UNITY_WEBGL && ENABLE_MAXST		
			if ((Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.WindowsEditor)
				&& SimulationController.Instance.SimulationMode)
			{
				TrackerManager.GetInstance().StartTracker(TrackerManager.TRACKER_TYPE_SPACE);
			}
			else
			{
				if (TrackerManager.GetInstance().IsFusionSupported())
				{
					CameraDevice.GetInstance().SetARCoreTexture();
					CameraDevice.GetInstance().SetFusionEnable();
					CameraDevice.GetInstance().Start();
					TrackerManager.GetInstance().StartTracker(TrackerManager.TRACKER_TYPE_SPACE);
				}
				else
				{
					TrackerManager.GetInstance().RequestARCoreApk();
				}
			}

			// For see through smart glass setting
			if (ConfigurationScriptableObject.GetInstance().WearableType == WearableCalibration.WearableType.OpticalSeeThrough)
			{
				WearableManager.GetInstance().GetDeviceController().SetStereoMode(true);

				CameraBackgroundBehaviour cameraBackground = FindObjectOfType<CameraBackgroundBehaviour>();
				cameraBackground.gameObject.SetActive(false);

				WearableManager.GetInstance().GetCalibration().CreateWearableEye(Camera.main.transform);

				// BT-300 screen is splited in half size, but R-7 screen is doubled.
				if (WearableManager.GetInstance().GetDeviceController().IsSideBySideType() == true)
				{
					// Do something here. For example resize gui to fit ratio
				}
			}
#endif
		}

#if !UNITY_WEBGL && ENABLE_MAXST
		public void AddTrackerData(string filePackageName = "")
		{
			_hasAreaBeenDetected = false;
			if (MainController.Instance.IsARMode)
			{
#if AREAMAX_COMPLETE
				_spaceTrackablesMap.Clear();
				SpaceTrackableBehaviour[] RoomTrackables = FindObjectsOfType<SpaceTrackableBehaviour>();
				foreach (var trackable in RoomTrackables)
				{
					Transform childTransform = trackable.gameObject.transform.GetChild(0);
					trackable.TrackableName = childTransform.name;
					_spaceTrackablesMap.Add(trackable.TrackableName, trackable);
				}

#if !ENABLE_NREAL
				StartCoroutine(LoadTrackerData(filePackageName));
#else
				StartNRealEngine(filePackageName);
#endif
#else
				_spaceTrackablesMap.Clear();
				SpaceTrackableBehaviour[] RoomTrackables = FindObjectsOfType<SpaceTrackableBehaviour>();
				foreach (var trackable in RoomTrackables)
				{
					_spaceTrackablesMap.Add(trackable.TrackableName, trackable);
				}
#if !ENABLE_NREAL
				StartCoroutine(LoadTrackerData(filePackageName));
#else
				StartNRealEngine(filePackageName);
#endif
#endif			
			}
		}
#endif


#if ENABLE_NREAL
		private void StartNRealEngine(string filePackageName)
		{
			_filePackageName = filePackageName;
			if (_nrCollectYUV == null)
			{
				_nrCollectYUV = new NRCollectYUV();
			}
			_nrCollectYUV.PlayNReal();
			StartCoroutine(LoadTrackerData(_filePackageName));
		}
#endif		

#if !UNITY_WEBGL && ENABLE_MAXST
		private IEnumerator LoadTrackerData(string filePackageName)
		{
#if ENABLE_NREAL
			while (!_nrCollectYUV.isReady)
			{
				yield return new WaitForEndOfFrame();
			}

			TrackerManager.GetInstance().StartTracker(TrackerManager.TRACKER_TYPE_SPACE);
#else			
			yield return new WaitForEndOfFrame();
#endif			

			if (MainController.Instance.IsARMode)
			{
#if AREAMAX_COMPLETE
				foreach (var trackable in _spaceTrackablesMap)
				{
					if (trackable.Value.StorageType == StorageType.AbsolutePath)
					{
						TrackerManager.GetInstance().AddTrackerData(trackable.Value.TrackerDataFileName);
					}
					else if (trackable.Value.StorageType == StorageType.StreamingAssets)
					{
						if (Application.platform == RuntimePlatform.Android)
						{
							List<string> fileList = new List<string>();
							yield return StartCoroutine(MaxstARUtil.ExtractAssets(filePackageName, fileList));
							TrackerManager.GetInstance().AddTrackerData(fileList[0], false);
						}
						else
						{
							Debug.Log(Application.streamingAssetsPath + "/" + trackable.Value.TrackerDataFileName);
							TrackerManager.GetInstance().AddTrackerData(Application.streamingAssetsPath + "/" + trackable.Value.TrackerDataFileName);
						}
					}
				}
				TrackerManager.GetInstance().LoadTrackerData();
#else
#if !UNITY_WEBGL
				foreach (var trackable in _spaceTrackablesMap)
				{
					if (trackable.Value.TrackerDataFileName.Length == 0)
					{
						continue;
					}

					if (trackable.Value.StorageType == StorageType.AbsolutePath)
					{
						TrackerManager.GetInstance().AddTrackerData(trackable.Value.TrackerDataFileName);
#if ENABLE_NREAL
						TrackerManager.GetInstance().LoadTrackerData();
#endif
					}
					else if (trackable.Value.StorageType == StorageType.StreamingAssets)
					{
						if (Application.platform == RuntimePlatform.Android)
						{
							yield return StartCoroutine(MaxstARUtil.ExtractAssets(trackable.Value.TrackerDataFileName, (filePath) =>
							{
								TrackerManager.GetInstance().AddTrackerData(filePath, false);
#if ENABLE_NREAL
								TrackerManager.GetInstance().LoadTrackerData();
#endif
							}));
						}
						else
						{
							Debug.Log(Application.streamingAssetsPath + "/" + trackable.Value.TrackerDataFileName);
							TrackerManager.GetInstance().AddTrackerData(Application.streamingAssetsPath + "/" + trackable.Value.TrackerDataFileName);						
#if ENABLE_NREAL
							TrackerManager.GetInstance().LoadTrackerData();
#endif
						}
					}
				}
#endif				
#if !ENABLE_NREAL				
				TrackerManager.GetInstance().LoadTrackerData();
#endif				
#endif			
			}
			MainController.Instance.ApplyOclusionNavigation();
		}
#endif

		private void DisableAllTrackables()
		{
#if !UNITY_WEBGL && ENABLE_MAXST	
			foreach (var trackable in _spaceTrackablesMap)
			{
				trackable.Value.OnTrackFail();
			}
#endif			
		}

		private void UpdateARMaxST()
		{
			DisableAllTrackables();

#if !UNITY_WEBGL && ENABLE_MAXST
			TrackingState state;
#if !ENABLE_NREAL	
			state = TrackerManager.GetInstance().UpdateTrackingState();
#else
			if (_nrCollectYUV == null)
			{
				return;
			}
        	_nrCollectYUV.UpdateFrame();
        	state = TrackerManager.GetInstance().UpdateTrackingState(1);
#endif			

			if (state == null)
			{
				return;
			}

#if !ENABLE_NREAL	
			if (_cameraBackgroundBehaviour != null) _cameraBackgroundBehaviour.UpdateCameraBackgroundImage(state);
#endif			

#if !ENABLE_NREAL	
			if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.WindowsEditor)
			{
				// guideView.SetActive(false);
			}
			else
			{
				int fusionState = TrackerManager.GetInstance().GetFusionTrackingState();
				if (fusionState == -1)
				{
					return;
				}
			}
#endif
			TrackingResult trackingResult = state.GetTrackingResult();

			bool cycleAreaDetection = false;
			for (int i = 0; i < trackingResult.GetCount(); i++)
			{
				Trackable trackable = trackingResult.GetTrackable(i);

				if (!_spaceTrackablesMap.ContainsKey(trackable.GetName()))
				{
					return;
				}

				if (!_hasAreaBeenDetected)
                {
					_hasAreaBeenDetected = true;
					SystemEventController.Instance.DispatchSystemEvent(EventARMaxSTControllerAreaRecognized);
				}
				cycleAreaDetection = true;
#if !ENABLE_NREAL				
				_spaceTrackablesMap[trackable.GetName()].OnTrackSuccess(trackable.GetId(), trackable.GetName(), trackable.GetPose(arMaxSTCamera.gameObject));
#else
            	_spaceTrackablesMap[trackable.GetName()].OnTrackSuccess(trackable.GetId(), trackable.GetName(), trackable.GetSpaceNRealPose());
#endif				
			}
			if (!cycleAreaDetection)
            {
				if (_hasAreaBeenDetected)
				{
					_hasAreaBeenDetected = false;					
					SystemEventController.Instance.DispatchSystemEvent(EventARMaxSTControllerAreaLost);
				}
			}
#endif			
		}

	void OnApplicationPause(bool pause)
    {
#if !UNITY_WEBGL && ENABLE_MAXST
        if (pause)
        {
			TrackerManager.GetInstance().StopTracker();
#if !ENABLE_NREAL			
            CameraDevice.GetInstance().Stop();
#else
            _nrCollectYUV.StopNReal();
#endif
        }
        else
        {
#if !ENABLE_NREAL						
			StartARMobile();
#else
			_nrCollectYUV.PlayNReal();
			AddTrackerData(_filePackageName);
#endif            
        }
#endif		
    }

		void OnDestroy()
		{
#if !UNITY_WEBGL && ENABLE_MAXST	
			_spaceTrackablesMap.Clear();
#if !ENABLE_NREAL			
			CameraDevice.GetInstance().Stop();
#endif			
			TrackerManager.GetInstance().StopTracker();
			TrackerManager.GetInstance().DestroyTracker();
#endif			
		}

		void Update()
        {
			if (MainController.Instance.IsARMode)
			{
				UpdateARMaxST();
			}
		}
    }
}
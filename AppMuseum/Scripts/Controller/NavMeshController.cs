using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
#if ENABLE_VUFORIA	
using Vuforia;
#endif
using yourvrexperience.Utils;

namespace yourvrexperience.template6dof
{
    public class NavMeshController : MonoBehaviour
    {
        public const float SHIFT_FROM_FLOOR = 0.45f;

        public const string EventNavMeshControllerStarted = "EventNavMeshControllerStarted";
        public const string EventNavMeshControllerInstantiateNavAgents = "EventNavMeshControllerInstantiateNavAgents";
        public const string EventNavMeshControllerUpdateTarget = "EventNavMeshControllerUpdateTarget";
        public const string EventNavMeshControllerReleaseResources = "EventNavMeshControllerReleaseResources";

        private static NavMeshController _instance;

        public static NavMeshController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(NavMeshController)) as NavMeshController;
                }
                return _instance;
            }
        }

        [Header("Area Target Position")]
        public Transform AreaTargetTransform;

        [Header("Area MaxST")]
        public Transform AreaMaxST;

        [Header("Container Area MaxST")]
        public Transform ContainerAreaMaxST;

        [Header("Navigation Agent for Area Target")]
        public GameObject NavigationAgentPrefab;

        [Header("Navigation Agent for Guid")]
        public GameObject NavigationGOAgentPrefab;

        [Header("Navigation Agent for Area Target")]
        public Transform TargetNavigation;

        [Header("Container of the waypoints")]
        public Transform WaypointsContainter;

        [Header("Arrow path to target")]
        public GameObject ArrowPathToTarget;

        private Transform _arCameraTransform;
        private NavigationAgentView _navAgentPlayerView;
        private NavigationAgentView _navAgentProviderView;
        private NavigationAgentView _navAgentGuideView;

        private Vector3 _areaTargetOriginalPosition;
        private Vector3 _currentDestination;

        private GameObject _refNavigationHelper;
        private GameObject _refAreaMaxSTHelper;

        private LineRenderer _arrowPathLine;

        private bool _hasFoundAreaTarget = false;

        const float DISTANCE_THRESHOLD = 1.5f;

        public Transform ArCameraTransform
        {
            get { 
                if (_arCameraTransform == null)
                {
                    _arCameraTransform = MainController.Instance.GameInputController.Camera.transform;
                }
                return _arCameraTransform; 
            }
        }
        public NavigationAgentView NavigationAgentProviderView
        {
            get { return _navAgentProviderView;  }
        }
        public NavigationAgentView NavigationAgentGuideView
        {
            get { return _navAgentGuideView; }
        }
        public GameObject RefNavigationHelper
        {
            get { return _refNavigationHelper; }
        }
        public GameObject RefAreaMaxSTHelper
        {
            get { return _refAreaMaxSTHelper; }
        }
        public void CreateNavigationAgentGuide(Vector3 startingPosition)
        {
            if (_navAgentGuideView != null)
            {
                _navAgentGuideView.Destroy();
                Destroy(_navAgentGuideView.gameObject);
                _navAgentGuideView = null;
            }
            _navAgentGuideView = Instantiate(NavigationGOAgentPrefab, startingPosition, Quaternion.identity).GetComponent<NavigationAgentView>();
            _navAgentGuideView.gameObject.name = "NavAgentAreaGuide";
        }

        public void CreateNavigationAgentProvider(Vector3 startingPosition)
        {
            if (_navAgentProviderView != null)
            {
                _navAgentProviderView.Destroy();
                Destroy(_navAgentProviderView.gameObject);
                _navAgentProviderView = null;
            }
            _navAgentProviderView = Instantiate(NavigationAgentPrefab, startingPosition, Quaternion.identity).GetComponent<NavigationAgentView>();
            _navAgentProviderView.gameObject.name = "NavAgentAreaProvider";
        }

        public void CreateNavigationAgentPlayer(Vector3 startingPosition)
        {
            if (_navAgentPlayerView != null)
            {
                _navAgentPlayerView.Destroy();
                Destroy(_navAgentPlayerView.gameObject);
                _navAgentPlayerView = null;
            }
            _navAgentPlayerView = Instantiate(NavigationAgentPrefab, startingPosition, Quaternion.identity).GetComponent<NavigationAgentView>();
            _navAgentPlayerView.gameObject.name = "NavAgentAreaPlayer";
        }

        void Awake()
        {
            _areaTargetOriginalPosition = AreaTargetTransform.transform.position;
        }

        void Start()
        {
            _refNavigationHelper = new GameObject();
            _refNavigationHelper.transform.parent = NavMeshController.Instance.AreaTargetTransform.transform;

            _refAreaMaxSTHelper = new GameObject();
            _refAreaMaxSTHelper.transform.parent = NavMeshController.Instance.AreaMaxST.transform;

            SystemEventController.Instance.Event += OnSystemEvent;
            SystemEventController.Instance.DelaySystemEvent(EventNavMeshControllerStarted, 0.1f);

#if ENABLE_VUFORIA		
            AreaTargetBehaviour areaTargetBehaviour = GameObject.FindObjectOfType<AreaTargetBehaviour>();
            VuforiaController.Instance.SetWorldCenter(areaTargetBehaviour);
#endif
        }

        void OnDestroy()
        {
            DestroyArrowPath();
            if (_refNavigationHelper != null)
            {
                GameObject.Destroy(_refNavigationHelper);
                _refNavigationHelper = null;
            }
            if (_refAreaMaxSTHelper != null)
            {
                GameObject.Destroy(_refAreaMaxSTHelper);
                _refAreaMaxSTHelper = null;
            }
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
        }

        public void UnParent()
        {
            this.transform.parent = null;
        }

        void Update()
        {
            if (MainController.Instance.IsARMode)
            {
                if (_navAgentPlayerView != null)
                {
                    UpdateNavigationAgentPosition();
                }
            }
        }

        private void UpdateNavigationAgentPosition()
        {
#if ENABLE_NIANTIC && !UNITY_EDITOR	            
            Vector3 agentPosition = NianticController.Instance.WorldToDesignPoint(ArCameraTransform.position);
            _navAgentPlayerView.SetLocalPosition(agentPosition);
#else
            var arCamPositionInAreaTarget = AreaTargetTransform.InverseTransformPoint(ArCameraTransform.position);
            Vector3 agentPosition = arCamPositionInAreaTarget + _areaTargetOriginalPosition;
            _navAgentPlayerView.SetLocalPosition(agentPosition);
#endif            
        }

        public void NavigateTo(Transform destinationTransform)
        {
            if (_navAgentPlayerView != null)
            {
#if ENABLE_NIANTIC && !UNITY_EDITOR	            
                _currentDestination = NianticController.Instance.WorldToDesignPoint(destinationTransform.position);
                _navAgentPlayerView.SetLocalPosition(_currentDestination);
#else
                var localPositionInAreaTarget = AreaTargetTransform.InverseTransformPoint(destinationTransform.position);
                _currentDestination = localPositionInAreaTarget + _areaTargetOriginalPosition;
                _navAgentPlayerView.SetDestination(_currentDestination);
#endif                 
            }
        }

        public Vector3 ConvertStandardARToNavigation(Vector3 maxSTLocalPosition, bool debugShape = false)
        {
            Vector3 localPositionCorrected = maxSTLocalPosition;
            Vector3 posNavigation = Vector3.zero;
#if ENABLE_NIANTIC && !UNITY_EDITOR					
			posNavigation = NianticController.Instance.WorldToDesignPoint(localPositionCorrected);
            return posNavigation;
#else            
            posNavigation = yourvrexperience.Utils.Utilities.ProjectPointOntoPlane(NavMeshController.Instance.AreaMaxST, NavMeshController.Instance.AreaTargetTransform, localPositionCorrected);
#endif

            GameObject refNavigation = new GameObject();
            refNavigation.transform.parent = NavMeshController.Instance.AreaTargetTransform.transform;
            refNavigation.transform.localPosition = posNavigation;

            Vector3 output = refNavigation.transform.position + Vector3.zero;

            if (debugShape)
            {
                GameObject refShape = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                refShape.transform.position = output;
                refShape.GetComponent<Collider>().enabled = false;
            }

            GameObject.Destroy(refNavigation);

            return output;
        }

        public Vector3 ConvertARWorldToNavigation(Vector3 maxSTLocalPosition, bool debugShape = false)
        {
            Vector3 localPositionCorrected = maxSTLocalPosition;
            Vector3 posNavigation = Vector3.zero;
#if ENABLE_NIANTIC && !UNITY_EDITOR					
			posNavigation = NianticController.Instance.WorldToDesignPoint(localPositionCorrected);
            return posNavigation;
#else            
            if (!MainController.Instance.IsNormalAxis)
            {
                localPositionCorrected = new Vector3(maxSTLocalPosition.x, maxSTLocalPosition.z, maxSTLocalPosition.y);
            }
            posNavigation = yourvrexperience.Utils.Utilities.ProjectPointOntoPlane(NavMeshController.Instance.AreaMaxST, NavMeshController.Instance.AreaTargetTransform, localPositionCorrected);
#endif   

            GameObject refNavigation = new GameObject();
            refNavigation.transform.parent = NavMeshController.Instance.AreaTargetTransform.transform;
            refNavigation.transform.localPosition = posNavigation;

            Vector3 output = refNavigation.transform.position + Vector3.zero;
            if (!MainController.Instance.IsNormalAxis)
            {
                output = new Vector3(output.x, output.z, output.y);
            }

            if (debugShape)
            {
                GameObject refShape = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                refShape.transform.position = output;
                refShape.GetComponent<Collider>().enabled = false;
            }

            GameObject.Destroy(refNavigation);

            return output;
        }

        public Vector3 ConvertNavigationToStandardAR(Vector3 navigationLocalPosition, bool debugShape = false)
        {
            Vector3 posNavigation = Vector3.zero;
#if ENABLE_NIANTIC && !UNITY_EDITOR					
			posNavigation = NianticController.Instance.DesignToWorldPoint(navigationLocalPosition);
            return posNavigation;
#else            
            posNavigation = yourvrexperience.Utils.Utilities.ProjectPointOntoPlane(NavMeshController.Instance.AreaTargetTransform, NavMeshController.Instance.AreaMaxST, navigationLocalPosition);
#endif            

            GameObject refMaxST = new GameObject();
            refMaxST.transform.parent = NavMeshController.Instance.AreaMaxST.transform;
            refMaxST.transform.localPosition = posNavigation;

            Vector3 output = refMaxST.transform.position + Vector3.zero;
            output.y -= SHIFT_FROM_FLOOR;

            if (debugShape)
            {
                GameObject refShape = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                if (!MainController.Instance.IsNormalAxis)
                {
                    refShape.transform.position = new Vector3(output.x, output.z, output.y); 
                }
                else
                {
                    refShape.transform.position = output;
                }
                refShape.GetComponent<Collider>().enabled = false;
            }

            GameObject.Destroy(refMaxST);
            return output;
        }

        public Vector3 ConvertNavigationToARWorld(Vector3 navigationLocalPosition, bool debugShape = false)
        {
            Vector3 posNavigation = Vector3.zero;
#if ENABLE_NIANTIC && !UNITY_EDITOR					
			posNavigation = NianticController.Instance.DesignToWorldPoint(navigationLocalPosition);
            return posNavigation;
#else                
            if (!MainController.Instance.IsNormalAxis)
            {
                posNavigation = yourvrexperience.Utils.Utilities.ProjectPointOntoPlaneSwap(NavMeshController.Instance.AreaTargetTransform, NavMeshController.Instance.AreaMaxST, navigationLocalPosition);
            }
            else
            {
                posNavigation = yourvrexperience.Utils.Utilities.ProjectPointOntoPlane(NavMeshController.Instance.AreaTargetTransform, NavMeshController.Instance.AreaMaxST, navigationLocalPosition);
            }
#endif            

            GameObject refMaxST = new GameObject();
            refMaxST.transform.parent = NavMeshController.Instance.AreaMaxST.transform;
            refMaxST.transform.localPosition = posNavigation;

            Vector3 output = refMaxST.transform.position + Vector3.zero;
            if (!MainController.Instance.IsNormalAxis)
            {
                output.z += SHIFT_FROM_FLOOR;
            }
            else
            {
                output.y -= SHIFT_FROM_FLOOR;
            }

            if (debugShape)
            {
                GameObject refShape = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                if (!MainController.Instance.IsNormalAxis)
                {
                    refShape.transform.position = new Vector3(output.x, output.z, output.y); 
                }
                else
                {
                    refShape.transform.position = output;
                }
                refShape.GetComponent<Collider>().enabled = false;
            }

            GameObject.Destroy(refMaxST);

            return output;
        }

        public void OnAreaTargetFound()
        {
            if (!_hasFoundAreaTarget)
            {
                _hasFoundAreaTarget = true; 
#if ENABLE_VUFORIA     
                VuforiaController.Instance.HasAreaBeenDetected = true;
#endif                
            }
        }

        public void OnAreaTargetLost()
        {
            if (_hasFoundAreaTarget)
            {
                _hasFoundAreaTarget = false;
#if ENABLE_VUFORIA                
                VuforiaController.Instance.HasAreaBeenDetected = false;
#endif                
            }
        }

        public void DestroyArrowPath()
        {
            if (_arrowPathLine != null)
            {
                GameObject.Destroy(_arrowPathLine);
                _arrowPathLine = null;
            }
        }

        public void UpdateArrowPath(Vector3 origin, Vector3 target)
        {
            if (_arrowPathLine == null)
            {
                _arrowPathLine = Instantiate(ArrowPathToTarget).GetComponent<LineRenderer>();
				if (MainController.Instance.IsNormalAxis)
				{
					_arrowPathLine.transform.Rotate(new Vector3(90, 0, 0));
				}
            }
            float arrowSegment = 1;
            CreateNavigationAgentProvider(origin);
            List<Vector3> navigationPoints = MainController.Instance.GetPathToTarget(origin, target, arrowSegment);
            Vector3[] finalPoints = new Vector3[navigationPoints.Count];
            for (int i = 0; i < navigationPoints.Count; i++)
            {
                RefNavigationHelper.transform.position = navigationPoints[i];
                Vector3 waypointNavigation = ConvertNavigationToARWorld(RefNavigationHelper.transform.localPosition, false);
                if (!MainController.Instance.IsNormalAxis)
                {
                    finalPoints[i] = new Vector3(waypointNavigation.x, waypointNavigation.y, waypointNavigation.z + 1);
                }
                else
                {
                    finalPoints[i] = new Vector3(waypointNavigation.x, waypointNavigation.y - 0.6f, waypointNavigation.z);
                }                
            }
            _arrowPathLine.positionCount = navigationPoints.Count;
            _arrowPathLine.SetPositions(finalPoints);
        }

        public void ToStorage(bool isNormalAxis, Vector3 livePos, Quaternion liveRot, out Vector3 storedPos, out Quaternion storedRot)
        {
            bool swap = !isNormalAxis;
            storedPos = AxisConverter.ConvertVectorIfNeeded(livePos, swap);
            storedRot = AxisConverter.ConvertRotationIfNeeded(liveRot, swap);
        }

        public void FromStorage(bool isNormalAxis, Vector3 storedPos, Quaternion storedRot, out Vector3 livePos, out Quaternion liveRot)
        {
            bool swap = !isNormalAxis;
            livePos = AxisConverter.ConvertVectorIfNeeded(storedPos, swap);
            liveRot = AxisConverter.ConvertRotationIfNeeded(storedRot, swap);
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(MainController.EventMainControllerReleaseGameResources)
                || nameEvent.Equals(EventNavMeshControllerReleaseResources))
			{
				if (_instance != null)
				{
					_instance = null;
                    _navAgentPlayerView = null;
                    _navAgentProviderView = null;
                    _navAgentGuideView = null;
 					GameObject.Destroy(this.gameObject);
				}
			}
            if (nameEvent.Equals(EventNavMeshControllerUpdateTarget))
            {
                if (_navAgentPlayerView != null)
                {
                    NavigateTo((Transform)parameters[0]);
                }
            }
        }
    }
}
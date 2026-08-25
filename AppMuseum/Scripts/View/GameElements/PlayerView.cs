using yourvrexperience.Utils;
using yourvrexperience.VR;
using UnityEngine;
using System;
using yourvrexperience.Networking;
using System.Collections.Generic;
using Unity.VisualScripting;
using yourvrexperience.Narration;

namespace yourvrexperience.template6dof
{
	[RequireComponent(typeof(Collider))]
	[RequireComponent(typeof(Rigidbody))]	
	public class PlayerView : MonoBehaviour, ICameraPlayer, INetworkObject
	{
		public const string EventPlayerAppHasStarted = "EventPlayerAppHasStarted";
		public const string EventPlayerAppEnableMovement = "EventPlayerAppEnableMovement";
		public const string EventPlayerViewPositionUpdated = "EventPlayerViewPositionUpdated";
		public const string EventPlayerViewMovePlayerForward = "EventPlayerViewMovePlayerForward";
		public const string EventPlayerViewInitBody = "EventPlayerViewInitBody";
		public const string EventPlayerViewRequestBody = "EventPlayerViewRequestBody";
		public const string EventPlayerViewReleaseGameResources = "EventPlayerViewReleaseGameResources";
		public const string EventPlayerViewWaypointConsumed = "EventPlayerViewWaypointConsumed";
		public const string EventDestroyArrowPath = "EventDestroyArrowPath";		
		public const string EventShowArrowPath = "EventShowArrowPath";		
		public const string EventPlayerViewUpdateTarget = "EventPlayerViewUpdateTarget";
		public const string EventPlayerDisconnectParent = "EventPlayerDisconnectParent";
		public const string EventPlayerViewEnableBody = "EventPlayerViewEnableBody";
		public const string EventPlayerViewDisableBody = "EventPlayerViewDisableBody";
		public const string EventPlayerViewEnableMovement = "EventPlayerViewEnableMovement";

		public const float DistanceBetweenWaypoints = 0.5f;
		public const float DistanceToUpdateWaypoint = 0.25f;
		public const float DistanceToReachGoal = 2f;
		public const float TotalTimeToUpdateArrowPath = 0.5f;

		[SerializeField] private GameObject Body;
		[SerializeField] private string NameAssetBody;
		[SerializeField] private float ScaleBody = 1;

		private GameObject _bodyAsset = null;
		private GameObject _bodyContainerNetwork = null;
		private GameObject _bodyNetwork = null;
		private float _rotationY = 0F;
		private Vector3 _forwardCamera = Vector3.zero;
		private bool _enableMovement = true;
		private Camera _camera;
		private Collider _collider;
		private Rigidbody _rigidBody;
		private bool _moveForwardActivated = false;
		private bool _isOnFloor = true;
		private int _layerFloor = -1;
		private bool _hasBeenInited = false;
		private GameObject _refNavigationHelper;
		private GameObject _refAreaMaxSTHelper;
		private Vector3 _targetToGo;
		private List<Vector3> _waypointsToCheck;
		private int _nextIndexWaypointToCheck;
		private float _timeUpdatePath = 0;
		private bool _isVRClient = false;


#if (UNITY_ANDROID && !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)) && !UNITY_EDITOR
    	private Gyroscope _gyro;
		private GameObject _cameraContainer;
		private Quaternion _rotationInitial;
#endif

		private NetworkObjectID _networkGameID;
		public NetworkObjectID NetworkGameIDView
		{
			get
			{
				if (_networkGameID == null)
				{
					if (this != null)
					{
						_networkGameID = GetComponent<NetworkObjectID>();
					}
				}
				return _networkGameID;
			}
		}

		public string NameNetworkPrefab 
		{
			get { return null; }
		}
		public string NameNetworkPath 
		{
			get { return null; }
		}
		public bool LinkedToCurrentLevel
		{
			get { return false; }
		}

        public void SetInitData(string initializationData)
        {
        }

        public void OnInitDataEvent(string initializationData)
        {
        }		

        public GameObject GetGameObject()
		{
			return this.gameObject;
		}

		public Vector3 PositionCamera 
		{ 
			get { return _camera.transform.position; } 
			set { _camera.transform.position = value; } 
		}
		public Vector3 ForwardCamera 
		{
			get { return _camera.transform.forward; } 
			set { _camera.transform.forward = value; } 
		}
		public Vector3 PositionBase
		{ 
			get {  return this.transform.position + new Vector3(0, transform.localScale.y, 0); } 
		}
		public GameObject RefNavigationHelper
		{
			get { return _refNavigationHelper; }
		}
		public GameObject RefAreaMaxSTHelper
		{
			get { return _refAreaMaxSTHelper; }
		}		
		private Vector3 TargetToGo
        {
			get { return _targetToGo; }
			set { _targetToGo = value; }
        }

        public bool IsOwner()
        {
            return true;
        }

		void Awake()
		{
			_collider = this.GetComponent<Collider>();
			_rigidBody = this.GetComponent<Rigidbody>();

			_collider.isTrigger = true;
			_rigidBody.useGravity = false;
			_rigidBody.isKinematic = true;
		}

		void Start()
		{
			SystemEventController.Instance.DispatchSystemEvent(EventPlayerAppHasStarted, this);
		}

		public void Initialize()
		{
			_camera = Camera.main;
			_layerFloor = LayerMask.NameToLayer("Floor");

			SystemEventController.Instance.Event += OnSystemEvent;
			NetworkController.Instance.NetworkEvent += OnNetworkEvent;

			bool shouldRun = true;
			if (MainController.Instance.IsMultiplayer)
			{
				NetworkGameIDView.InitedEvent += OnInitDataEvent;
#if ENABLE_MIRROR			
				NetworkGameIDView.RefreshAuthority();
#endif			
			}

			NameAssetBody = "BodyDesktopBlue";
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
			NameAssetBody = "BodyVRBlue";
#endif

			if (!MainController.Instance.IsMultiplayer)
			{
				bool isVRMode = false;
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
				isVRMode = true;
#endif

#if UNITY_EDITOR
				isVRMode = true;
#endif
				Body.SetActive(false);				
				SystemEventController.Instance.DispatchSystemEvent(CameraXRController.EventCameraPlayerReadyForCamera, this);

				_bodyAsset = new GameObject();
			}
			else 
			{
				if (NetworkGameIDView.AmOwner())
				{
					bool isVRMode = false;
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
					isVRMode = true;
#endif

#if UNITY_EDITOR
					isVRMode = true;
#endif
					Body.SetActive(false);
					SystemEventController.Instance.DispatchSystemEvent(CameraXRController.EventCameraPlayerReadyForCamera, this);
					NetworkController.Instance.DelayNetworkEvent(EventPlayerViewInitBody, 0.1f, -1, -1, NetworkController.Instance.UniqueNetworkID, NetworkGameIDView.GetViewID(), NameAssetBody, isVRMode);
				}
				else
				{
					yourvrexperience.Utils.Utilities.EnableRenderers(Body.transform, false);
					NetworkController.Instance.DelayNetworkEvent(EventPlayerViewRequestBody, 1f, -1, -1, NetworkController.Instance.UniqueNetworkID, NetworkGameIDView.GetViewID());				
					shouldRun = false;
				}
			}
		}

		void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;			
			if (NetworkController.Instance != null)	NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
			if (NetworkGameIDView != null) NetworkGameIDView.InitedEvent -= OnInitDataEvent;
			if (_refNavigationHelper != null) GameObject.Destroy(_refNavigationHelper);
			if (_refAreaMaxSTHelper != null) GameObject.Destroy(_refAreaMaxSTHelper);
			if (_bodyAsset != null) GameObject.Destroy(_bodyAsset);
			SystemEventController.Instance.DispatchSystemEvent(PlayerHandView.EventPlayerViewHandDestroyedAvatar, this);
		}

		private void InitHelpers()
		{
			if (MainController.Instance.IsARMode)
			{
				if (_refNavigationHelper == null)
				{
					_refNavigationHelper = new GameObject();
					_refNavigationHelper.transform.parent = NavMeshController.Instance.AreaTargetTransform.transform;
				}

				if (_refAreaMaxSTHelper == null)
				{
					_refAreaMaxSTHelper = new GameObject();
					_refAreaMaxSTHelper.transform.parent = NavMeshController.Instance.AreaMaxST.transform;
				}
			}
		}

		public void ActivatePhysics(bool activation, bool force = false)
		{
			_collider.isTrigger = !activation;
			_rigidBody.useGravity = activation;
			_rigidBody.isKinematic = !activation;
		}

		private void Move()
        {
			float axisVertical = Input.GetAxis("Vertical");
			float axisHorizontal = Input.GetAxis("Horizontal");
			if (_moveForwardActivated)
			{
				axisVertical = 1;
				axisHorizontal = 0;
			}
			float finalSpeed = GameLevelData.Instance.PlayersDesktopSpeed;
#if UNITY_EDITOR
			finalSpeed = 50;
#endif
#if (UNITY_ANDROID && !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)) && !UNITY_EDITOR
			finalSpeed = 10;
#endif
			Vector3 forward = axisVertical * _camera.transform.forward * finalSpeed * Time.deltaTime;
			Vector3 lateral = axisHorizontal * _camera.transform.right * finalSpeed * Time.deltaTime;
			Vector3 increment = forward + lateral;
			increment.y = 0;
			transform.GetComponent<Rigidbody>().MovePosition(transform.position + increment);
#if (UNITY_ANDROID && !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)) && !UNITY_EDITOR
			if (_cameraContainer != null) _cameraContainer.transform.position = this.transform.position;
#else			
			_camera.transform.position = this.transform.position + new Vector3(0,0.7f,0);
#endif
        }

        public void RotateCamera()
        {
#if (UNITY_ANDROID && !ENABLE_NIANTIC && !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)) && !UNITY_EDITOR
			if (_gyro == null)
			{
				_gyro = Input.gyro;
            	_gyro.enabled = true;

				_cameraContainer = new GameObject("Camera Container");
				_camera.transform.SetParent(_cameraContainer.transform);
            	_cameraContainer.transform.rotation = Quaternion.Euler(90f, 90f, 0f);
            	_rotationInitial = new Quaternion(0, 0, 1, 0);
			}
			if (_gyro != null)
			{
				_camera.transform.localRotation = _gyro.attitude * _rotationInitial;
				_forwardCamera = _camera.transform.forward;
				this.transform.forward = new Vector3(_forwardCamera.x, 0, _forwardCamera.z);
			}
#else		

			if (MainController.Instance.IsARMode) return;			

			if (!MainController.Instance.BlockCameraMovement)
			{
				if (Input.GetKey(KeyCode.LeftShift))
				{	
					float finalSensitivity = GameLevelData.Instance.SensitivityCamera;
#if UNITY_EDITOR
					finalSensitivity = 8;
#endif				
					float rotationX = _camera.transform.transform.localEulerAngles.y + Input.GetAxis("Mouse X") * finalSensitivity;
					_rotationY = _rotationY + Input.GetAxis("Mouse Y") * finalSensitivity;
					_rotationY = Mathf.Clamp(_rotationY, -60, 60);
					Quaternion rotation = Quaternion.Euler(-_rotationY, rotationX, 0);
					_forwardCamera = rotation * Vector3.forward;
					this.transform.forward = new Vector3(_forwardCamera.x, 0, _forwardCamera.z);
				}
				_camera.transform.forward = _forwardCamera;		
			}
#endif			
        }

		public void Jump()
		{
			if (_isOnFloor)
			{
				_isOnFloor = false;
				transform.GetComponent<Rigidbody>().AddForce(Vector3.up * 20, ForceMode.Impulse);
			}
		}

 		void OnCollisionEnter(Collision collision)
        {
			if (!_isOnFloor)
			{
				if (collision.gameObject.layer == _layerFloor)
				{
					_isOnFloor = true;
				}
			}			
        }

        private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
        {
			if (nameEvent.Equals(EventPlayerViewRequestBody))
			{
				int netID = (int)parameters[0];
				int playerNetID = (int)parameters[1];
				if (NetworkGameIDView.GetViewID() == playerNetID)
				{
					if (NetworkGameIDView.AmOwner())
					{
						bool isVRMode = false;
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
						isVRMode = true;
#endif

#if UNITY_EDITOR
						isVRMode = true;
#endif
						NetworkController.Instance.DelayNetworkEvent(EventPlayerViewInitBody, 0.1f, -1, -1, NetworkController.Instance.UniqueNetworkID, NetworkGameIDView.GetViewID(), NameAssetBody, isVRMode);
					}
				}
			}
            if (nameEvent.Equals(EventPlayerViewInitBody))
			{
				int netID = (int)parameters[0];
				int playerNetID = (int)parameters[1];
				string bodyPrefab = (string)parameters[2];
				if (NetworkGameIDView.GetViewID() == playerNetID)
				{
					_isVRClient = (bool)parameters[3];
					if (!NetworkGameIDView.AmOwner())
					{
						if (_bodyAsset == null)
						{
							_bodyAsset = AssetBundleController.Instance.CreateGameObject(bodyPrefab) as GameObject;
							_bodyAsset.transform.localScale = new Vector3(ScaleBody, ScaleBody, ScaleBody);							
							if (MainController.Instance.IsARMode && _isVRClient)
							{
								_bodyContainerNetwork = new GameObject();

								_bodyNetwork = AssetBundleController.Instance.CreateGameObject(bodyPrefab) as GameObject;
								_bodyNetwork.transform.localScale = new Vector3(ScaleBody - 0.1f, ScaleBody- 0.1f, ScaleBody- 0.1f);

								_bodyNetwork.transform.parent = _bodyContainerNetwork.transform;
								_bodyNetwork.transform.localPosition = Vector3.zero;
								_bodyContainerNetwork.transform.rotation = Quaternion.Euler(-90, 0, 0);
							}							
#if UNITY_EDITOR						
							yourvrexperience.Utils.Utilities.ResetMaterials(_bodyAsset);
#endif						
							_bodyAsset.transform.parent = Body.transform;
							_bodyAsset.transform.localPosition = Vector3.zero;
							_bodyAsset.transform.rotation = Body.transform.rotation;							
						}
					}
				}
			}
        }

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(EventPlayerViewEnableBody))
			{
				if (NetworkGameIDView.AmOwner())
				{
					bool activate = (bool)parameters[0];
					Body.SetActive(activate);
				}
			}
			if (nameEvent.Equals(EventPlayerViewDisableBody))
			{
				if (NetworkGameIDView.GetViewID() == (int)parameters[0])
				{
					Body.SetActive(false);
					if (_bodyAsset != null) GameObject.Destroy(_bodyAsset);
				}
			}
			if (nameEvent.Equals(LevelView.EventLevelViewDestroy))
			{
				ActivatePhysics(false);
			}
			if (nameEvent.Equals(EventPlayerViewMovePlayerForward))
			{
				_moveForwardActivated = (bool)parameters[0];
			}
			if (nameEvent.Equals(CameraXRController.EventCameraResponseToPlayer))
			{
				_camera = (Camera)parameters[0];
			}
			if (nameEvent.Equals(EventPlayerAppEnableMovement))
			{				
				_enableMovement = (bool)parameters[0];
			}
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))	
			{
				DontDestroyOnLoad(this.gameObject);
			}
			if (nameEvent.Equals(EventPlayerViewReleaseGameResources))
			{
				GameObject.Destroy(this.gameObject);
			}
			if (nameEvent.Equals(LevelView.EventLevelViewStarted))
			{
				if (!_hasBeenInited)
				{
					_hasBeenInited = true;

					if (MainController.Instance.IsARMode)
					{
						InitHelpers();
					}

					LevelView levelView = (LevelView)parameters[0];
					transform.position = levelView.InitialPosition.transform.position;
					transform.rotation = levelView.InitialPosition.transform.rotation;
					this.transform.parent = NavMeshController.Instance.AreaMaxST;
	#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
					VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerResetToInitial, transform.position, transform.rotation);
	#endif
					SystemEventController.Instance.DispatchSystemEvent(EventPlayerViewPositionUpdated);

					if (!MainController.Instance.IsMultiplayer)
					{
						ActivatePhysics(true);
					}					
				}
				else
				{
					this.transform.parent = NavMeshController.Instance.AreaMaxST;
					ActivatePhysics(true);
				}
			}
			if (nameEvent.Equals(EventPlayerDisconnectParent))
			{
				_hasBeenInited = true;
				this.transform.parent = null;
			}
			if (nameEvent.Equals(EventPlayerViewUpdateTarget))
			{				
				TargetToGo = (Vector3)parameters[0];
			}
			if (nameEvent.Equals(EventShowArrowPath))
			{
				GameObject nextTarget = (GameObject)parameters[0];
				if (nextTarget.GetComponent<WaypointToNextTarget>() != null)
                {
					nextTarget.GetComponent<WaypointToNextTarget>().GoToWaypoint();
				}
				TargetToGo = nextTarget.transform.position;
			}
			if (nameEvent.Equals(EventDestroyArrowPath))
            {
				TargetToGo = Vector3.zero;
				NavMeshController.Instance.DestroyArrowPath();
            }
		}

		private void UpdateWaypoints()
		{
			if (TargetToGo != Vector3.zero)
            {
				Vector3 posPlayer = new Vector3(this.transform.position.x, 0, this.transform.position.z);

				if ((_waypointsToCheck != null) && (_waypointsToCheck.Count > 0))
				{
					Vector3 nextWaypoint = _waypointsToCheck[0];
					Vector3 posWay = new Vector2(nextWaypoint.x, nextWaypoint.z);
					float distanceToNextWaypoint = Vector2.Distance(posPlayer, posWay);
					if (distanceToNextWaypoint < DistanceToUpdateWaypoint)
					{
						SystemEventController.Instance.DispatchSystemEvent(EventPlayerViewWaypointConsumed, _waypointsToCheck.Count - 1, _waypointsToCheck);
						_waypointsToCheck.RemoveAt(0);
						if (_waypointsToCheck.Count == 0)
						{
							TargetToGo = Vector3.zero;
						}
					}
					if (distanceToNextWaypoint > 2 * DistanceBetweenWaypoints)
					{
						_waypointsToCheck = new List<Vector3>();
						SystemEventController.Instance.DispatchSystemEvent(EventPlayerViewUpdateTarget, TargetToGo);
					}
				}
				else
                {
					float distanceToRealTarget = Vector3.Distance(TargetToGo, posPlayer);
					if (Vector3.Distance(TargetToGo, posPlayer) < DistanceToReachGoal)
                    {
						TargetToGo = Vector3.zero;
						_timeUpdatePath = 0;
						NavMeshController.Instance.DestroyArrowPath();
					}
					else
                    {
						_timeUpdatePath += Time.deltaTime;
						if (_timeUpdatePath > TotalTimeToUpdateArrowPath)
                        {
							_timeUpdatePath = 0;
							Vector3 posPlayerNavigation = this.transform.localPosition;
#if !(UNITY_EDITOR || (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || UNITY_WEBGL))
#if ENABLE_NIANTIC
							posPlayerNavigation = NavMeshController.Instance.ConvertARWorldToNavigation(this.transform.position, false);
#else
							posPlayerNavigation = NavMeshController.Instance.ConvertARWorldToNavigation(this.transform.localPosition, false);
#endif														
#endif							
							Vector3 posTargetNavigation = Vector3.zero;
#if !(UNITY_EDITOR || (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || UNITY_WEBGL))
#if ENABLE_NIANTIC
							posTargetNavigation = NavMeshController.Instance.ConvertARWorldToNavigation(TargetToGo, false);
#else
							NavMeshController.Instance.RefAreaMaxSTHelper.transform.position = TargetToGo;
							posTargetNavigation = NavMeshController.Instance.ConvertARWorldToNavigation(NavMeshController.Instance.RefAreaMaxSTHelper.transform.localPosition, false);
#endif														
#else
							NavMeshController.Instance.RefNavigationHelper.transform.position = TargetToGo;
							posTargetNavigation = NavMeshController.Instance.RefNavigationHelper.transform.localPosition;
#endif							
							NavMeshController.Instance.UpdateArrowPath(posPlayerNavigation, posTargetNavigation);
						}
                    }
				}
			}
		}

		public void Run()
		{
			if (!_enableMovement) return;

			bool runLogic = true;
			if (MainController.Instance.IsMultiplayer)
			{
				runLogic = NetworkGameIDView.AmOwner();
			}

#if !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR) || UNITY_EDITOR
			if (runLogic)
			{
				if (MainController.Instance.IsARMode)					
				{
					if (this.transform.parent != NavMeshController.Instance.AreaMaxST)
					{
						this.transform.parent = NavMeshController.Instance.AreaMaxST;
					}
#if ENABLE_NREAL
					this.transform.position = VRInputController.Instance.VRController.HeadController.transform.position;
					this.transform.forward = VRInputController.Instance.VRController.HeadController.transform.forward;
#else
					this.transform.position = MainController.Instance.GetARWorldCamera().transform.position;
					this.transform.forward = MainController.Instance.GetARWorldCamera().transform.forward;
#endif
				}
				else
				{
#if !ENABLE_VUFORIA					
					this.transform.forward = new Vector3(_camera.transform.position.x, 0, _camera.transform.position.z);
					Move();
					RotateCamera();
#else
					this.transform.forward =  new Vector3(_camera.transform.forward.x, 0, _camera.transform.forward.z);
					this.transform.position = new Vector3(_camera.transform.position.x, this.transform.position.y, _camera.transform.position.z);
					_camera.transform.position = new Vector3(_camera.transform.position.x, this.transform.position.y + 0.7f, _camera.transform.position.z);
#endif					
				}
			}
#endif			
			UpdateWaypoints();
		}

		void Update()
		{
			if ((NetworkGameIDView != null) && MainController.Instance.IsMultiplayer)
			{
				if (!NetworkGameIDView.AmOwner())
				{
					if (MainController.Instance.IsARMode)
					{
						if (_bodyAsset != null)
						{
							if (_isVRClient)
							{
								if (_bodyNetwork != null)
								{
									_bodyAsset.SetActive(false);
									InitHelpers();

									_refNavigationHelper.transform.position = this.transform.position;
									Vector3 positionFinal = NavMeshController.Instance.ConvertNavigationToARWorld(_refNavigationHelper.transform.localPosition, false);

									if (_bodyContainerNetwork.transform.parent != NavMeshController.Instance.AreaMaxST)
									{
										_bodyContainerNetwork.transform.parent = NavMeshController.Instance.AreaMaxST;
									}

									_bodyContainerNetwork.transform.position = positionFinal;
									_bodyNetwork.transform.localRotation = Quaternion.LookRotation(new Vector3(this.transform.forward.x, 0, this.transform.forward.z));
								}
							}
							else
							{
								_bodyAsset.SetActive(true);
							}
						}
					}
					else
					{
						Quaternion rotateFaceRigth = Quaternion.LookRotation(new Vector3(this.transform.forward.x, 0, this.transform.forward.z));
						Body.transform.localRotation = Quaternion.Inverse(this.transform.rotation);
						if (_bodyAsset != null) _bodyAsset.transform.localRotation = rotateFaceRigth;
					}
				}
			}
		}
    }
}

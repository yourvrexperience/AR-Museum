using yourvrexperience.Utils;
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using static yourvrexperience.Narration.NarrationController;
using yourvrexperience.Narration;
using static yourvrexperience.template6dof.LevelView;

namespace yourvrexperience.template6dof
{
	[RequireComponent(typeof(Rigidbody))]	
	public class TourGuideView : MonoBehaviour
	{
		public const string EventTourGuideViewReachedTarget = "EventTourGuideViewReachedTarget";
		public const string EventTourGuideViewReportPlayerClose = "EventTourGuideViewReportPlayerClose";
		public const string EventTourGuideViewEnableModel = "EventTourGuideViewEnableModel";
		public const string EventTourGuideViewSpeakActivation = "EventTourGuideViewSpeakActivation";
		public const string EventTourGuideViewBezierComplete = "EventTourGuideViewBezierComplete";

		public const string EventPOIGameOver = "EventPOIGameOver";
		public const string EventHallPOI1Outtro = "EventHallPOI1Outtro";

		public const string EventPOI0Animate01 = "EventPOI0Animate01";
		public const string EventPOI0Animate02 = "EventPOI0Animate02";
		public const string EventPOI0Animate03 = "EventPOI0Animate03";

		public const string EventPOI2Animate00 = "EventPOI2Animate00";
		public const string EventPOI2Animate01 = "EventPOI2Animate01";
		public const string EventPOI3Animate01 = "EventPOI3Animate01";
		public const string EventPOI3Animate02 = "EventPOI3Animate02";
		public const string EventPOI3Animate03 = "EventPOI3Animate03";
		public const string EventPOI3Animate04 = "EventPOI3Animate04";
		public const string EventPOI3Animate05 = "EventPOI3Animate05";

		public const string AnimationIdle = "Idle";
		public const string AnimationRun = "Run";
		public const string AnimationTalk = "Talk";
		
		enum TourGuideStates { Idle, WaitForPlayerNear, Navigation, Moving, Bezier, Talk };
		
		[SerializeField] private GameObject Model;
		[SerializeField] private float Speed = 1;
		[SerializeField] private float RotationSpeedFixed = 0.5f;
		[SerializeField] private float RotationSpeedMovement = 0.5f;
		[SerializeField] private WaypointFollower _waypointFollower;
		[SerializeField] private Animator ModelAnimator;
		[SerializeField] private GameObject screenVR;

		private NavigationAgentView _navigation;
		private bool _hasSynchronized = false;
		private TourGuideStates _state = TourGuideStates.Idle;
		private Vector3 _targetNavigation;
		private Vector3 _targetGO;
		private bool _hasBeenInited = false;
		private float _delayToSync = 0;
		private Vector3 _targetBezier;

		private GameObject _refNavigationHelper;
		private GameObject _refAreaMaxSTHelper;

		private bool _runUpdate = true;

		private bool _aligned = false;
		private bool _initialAlignmentCompleted = false;

		private AnimatorSystem _animatorSystem;

		private AudioSource _isNarrationPlaying = null;
		private GameObject _referencePlayer;

		private AudioSource SetNarrationAudioSource
		{
			set
			{ 
				_isNarrationPlaying = value;				
				if (_isNarrationPlaying != null)
				{
					ChangeGuideAnimation(AnimationTalk);
					SoundSpectrumAnalyzer.Instance.Play(_isNarrationPlaying);
				}
				else
				{
					ChangeGuideAnimation(AnimationIdle);
					SoundSpectrumAnalyzer.Instance.Stop();
				}
			}
		}

		public bool HasBeenInited
		{
			get { return _hasBeenInited; }
			set { _hasBeenInited = value; }
		}
		public bool RunUpdate
        {
			get { return _runUpdate; }
			set { _runUpdate = value; }
        }
		public bool IsNormalAxis 
		{
			get {  return MainController.Instance.IsNormalAxis; }
		}
		public Vector3 PositionPlayer
		{
			get {  
					return MainController.Instance.PlayerView.PositionCamera;
			}
		}
		public GameObject ScreenVR
		{
			get { return screenVR; }
		}
		public GameObject GetModel()
		{
			return Model;
		}

		public void Initialize()
		{
#if UNITY_EDITOR
			Model.transform.localScale = new Vector3(Model.transform.localScale.x, Model.transform.localScale.y, Model.transform.localScale.z);
#endif

			SystemEventController.Instance.Event += OnSystemEvent;
			UIEventController.Instance.Event += OnUIEvent;

			_refNavigationHelper = new GameObject();
			_refNavigationHelper.transform.parent = NavMeshController.Instance.AreaTargetTransform.transform;

			_refAreaMaxSTHelper = new GameObject();
			_refAreaMaxSTHelper.transform.parent = NavMeshController.Instance.AreaMaxST.transform;

			GameObject modelAge = null;
			switch (GameLevelData.Instance.Age)
			{
				case GameLevelData.GameAge.Kids:
					modelAge = AssetBundleController.Instance.CreateGameObject("ModelGuide0");
					break;

				case GameLevelData.GameAge.Adults:
					modelAge = AssetBundleController.Instance.CreateGameObject("ModelGuide1");
					break;					

				case GameLevelData.GameAge.Experts:
					modelAge = AssetBundleController.Instance.CreateGameObject("ModelGuide2");
					break;					
			}
			modelAge.transform.parent = Model.transform.parent;
			modelAge.transform.localPosition = Model.transform.localPosition;
			modelAge.transform.localRotation = Model.transform.localRotation;
			modelAge.transform.localScale = Model.transform.localScale;
			switch (GameLevelData.Instance.Age)
			{
				case GameLevelData.GameAge.Kids:
					modelAge.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
					modelAge.transform.localPosition += new Vector3(0, 0.3f, 0);
					break;

				case GameLevelData.GameAge.Adults:
					modelAge.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
					modelAge.transform.localPosition += new Vector3(0, 0.3f, 0);
					break;

				case GameLevelData.GameAge.Experts:
					modelAge.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
					modelAge.transform.localPosition += new Vector3(0, 0.3f, 0);
					break;
			}
			GameObject.Destroy(Model);
			Model = modelAge;
			ModelAnimator = Model.GetComponent<Animator>();

			screenVR.transform.parent = Model.transform;
			screenVR.transform.localPosition = new Vector3(0, 1.35f, 0) + Model.transform.forward * 0.32f;
			screenVR.SetActive(true);

			if (ModelAnimator != null)
			{
				_animatorSystem = ModelAnimator.gameObject.AddComponent<AnimatorSystem>();
				ChangeGuideAnimation(AnimationIdle);
			}

			Activate(false);
		}

		void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
			if (_navigation != null) _navigation.EventEnd -= OnNavigationEventEnd;
		}

		private void ChangeGuideAnimation(string animationName, bool force = false)
		{
			if (_animatorSystem != null) _animatorSystem.ChangeAnimation(animationName, force);
		}

        private void OnNavigationEventEnd(GameObject source)
        {
			ChangeState(TourGuideStates.Moving);			
        }

        void OnTriggerEnter(Collider collision)
        {
			
        }

		public void Activate(bool activation)
		{
			Model.SetActive(activation);
		}

		public bool IsActivated()
		{
			return Model.activeSelf;
		}

		public void SetPosition(Vector3 position)
		{
			this.transform.position = position;
			ChangeState(TourGuideStates.WaitForPlayerNear);
		}

		public void SetPositionOutsideNarration(Vector3 position)
		{
			StopNavigation();
			this.transform.position = position;
			ChangeState(TourGuideStates.Idle);
		}

		public void RunAnimationIdle()
		{
			ChangeGuideAnimation(AnimationIdle);
		}

		public void RunAnimationTalk()
		{
			ChangeGuideAnimation(AnimationTalk);			
		}

		private void StopNavigation()
		{
			if (_navigation != null)
			{
				if (_navigation.NavigationRunning)
				{
					_navigation.StopNavigation();
				}
				_navigation.EventEnd -= OnNavigationEventEnd;
				_navigation = null;
			}
		}

		public void WaitForPlayerToBeClose()
		{
			if (_state == TourGuideStates.Idle)
			{
				ChangeGuideAnimation(AnimationIdle);
				ChangeState(TourGuideStates.WaitForPlayerNear);
			}			
		}

		public void FacePosition(Vector3 position)
		{
			this.transform.forward = (position - this.transform.position).normalized;
		}			

		public bool NavigateTo(POIData targetPOI)			
		{
			Transform originalParentRoot = targetPOI.Root.transform.parent;
			Transform originalParentGOPosition = targetPOI.GOPosition.transform.parent;

			StopNavigation();

			targetPOI.Root.transform.parent = NavMeshController.Instance.ContainerAreaMaxST.transform;
			targetPOI.GOPosition.transform.parent = NavMeshController.Instance.ContainerAreaMaxST.transform;

#if ENABLE_NIANTIC
			Vector3 targetNavigation = targetPOI.Root.transform.position;
#else
			Vector3 targetNavigation = targetPOI.Root.transform.localPosition;
#endif			
			Vector3 targetGO = targetPOI.GOPosition.transform.position;

			targetPOI.Root.transform.parent = originalParentRoot;
			targetPOI.GOPosition.transform.parent = originalParentGOPosition;

#if ENABLE_NIANTIC
			Vector3 oriNavigation = NavMeshController.Instance.ConvertARWorldToNavigation(this.transform.position, false);
#else
			Vector3 oriNavigation = NavMeshController.Instance.ConvertARWorldToNavigation(this.transform.localPosition, false);
#endif			
			NavMeshController.Instance.CreateNavigationAgentGuide(oriNavigation);			

			_navigation = NavMeshController.Instance.NavigationAgentGuideView;
			_navigation.EventEnd += OnNavigationEventEnd;
			_targetNavigation = targetNavigation;
			_targetGO = targetGO;
			SetNarrationAudioSource = null;
			if (InitNavigationPath(false))
            {
				ChangeState(TourGuideStates.Navigation);
				return true;
			}
			else
            {
				ChangeState(TourGuideStates.Moving);
				return false;
            }			
		}

		private void DisplayDebugReachable()
		{
			DebugUIDisplayController.Instance?.DisplayMessage(" ++ REACHABLE POSITION");
		}

		private void DisplayDebugUnreachable()
		{
			DebugUIDisplayController.Instance?.DisplayMessage(" -- UNREACHABLE POSITION");
		}

		private bool InitNavigationPath(bool applyNavigation)
        {
#if ENABLE_NIANTIC			
			Vector3 oriNavigation = NavMeshController.Instance.ConvertARWorldToNavigation(this.transform.position, false);
#else			
			Vector3 oriNavigation = NavMeshController.Instance.ConvertARWorldToNavigation(this.transform.localPosition, false);
#endif
			Vector3 targetNavigation = NavMeshController.Instance.ConvertARWorldToNavigation(_targetNavigation, false);

			_refNavigationHelper.transform.position = oriNavigation;
			NavMeshController.Instance.ConvertNavigationToARWorld(_refNavigationHelper.transform.localPosition, false);
			_refNavigationHelper.transform.position = targetNavigation;
			NavMeshController.Instance.ConvertNavigationToARWorld(_refNavigationHelper.transform.localPosition, false);

			if (applyNavigation)
			{
				_navigation.SetGlobalPosition(oriNavigation);
				_navigation.SetDestination(targetNavigation);
			}
			else
            {
				List<Vector3> path = _navigation.GetPathToTarget(oriNavigation, targetNavigation);
				return path.Count > 0;
			}
			return true;
		}

		private void ApplyAnimation(List<Transform> waypoints)
        {
			_waypointFollower.Init(waypoints, 0.8f, 0.01f, 1, false);
			_targetBezier = waypoints[waypoints.Count - 1].position;
			SystemEventController.Instance.DelaySystemEvent(EventTourGuideViewBezierComplete, _waypointFollower.Duration);
			ChangeState(TourGuideStates.Bezier);
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(EventTourGuideViewBezierComplete))
			{
				_waypointFollower.Stop();
				ChangeState(TourGuideStates.Talk);
			}
			if (nameEvent.Equals(MainController.EventMainControllerReleaseGameResources))
			{
				GameObject.Destroy(this.gameObject);
			}
			if (nameEvent.Equals(EventTourGuideViewEnableModel))
			{
				Model.SetActive((bool)parameters[0]);
			}
			if (nameEvent.Equals(NarrationToken.EventNarrationTokenSetVisibilityTourGuide))
			{
				Model.SetActive((bool)parameters[0]);
			}
			if (nameEvent.Equals(NarrationToken.EventNarrationTokenStart))
			{
				bool mainNarration = (bool)parameters[0];
				int poiIndex = (int)parameters[1];
                float totalTimeNarration = (float)parameters[2];
                POIData currPOI = MainController.Instance.LevelView.GetPOIS()[poiIndex];
                string startEventNarration = (string)parameters[3];
				string narrationWaypoints = (string)parameters[4];
				if (mainNarration)
				{
					SetNarrationAudioSource = SoundsController.Instance.GetChannelAudioSource(mainNarration?SoundsController.ChannelsAudio.FX1:SoundsController.ChannelsAudio.FX2);
				}                
				if (narrationWaypoints.Length > 0)
				{
					SerializedNarrationObjects serializedWaypoints = JsonUtility.FromJson<SerializedNarrationObjects>(narrationWaypoints);
					List<Transform> waypointsAnimation = new List<Transform>();
					string assetName = "";
					Vector3 firstWaypoint = Vector3.zero;
					foreach (NarrationObject waypoint in serializedWaypoints.NarrationObjects)
					{
						if (firstWaypoint == Vector3.zero)
						{
							assetName = waypoint.AssetName;
							firstWaypoint = waypoint.Position;
						}						
						GameObject waypointGO = MainController.Instance.CreateWaypoint(waypoint.AssetName, NavMeshController.Instance.AreaMaxST.transform, waypoint.Position, waypoint.Rotation, waypoint.Scale, false);
						waypointsAnimation.Add(waypointGO.transform);
					}
					// UP TO 4 POINTS TO CREATE BEZIER MOVEMENT
					if (waypointsAnimation.Count < 4)
					{
						Vector3 startingPosition = this.gameObject.transform.localPosition;
						int totalSegments = 4 - waypointsAnimation.Count;
						float segmentDistance = (firstWaypoint - startingPosition).magnitude / totalSegments;	
						Vector3 forwardToTarget = (firstWaypoint - startingPosition).normalized;					
						int counter = 0;
						while (waypointsAnimation.Count < 4)
						{
							GameObject waypointGO = MainController.Instance.CreateWaypoint(assetName, NavMeshController.Instance.AreaMaxST.transform, startingPosition, Quaternion.identity, new Vector3(0.2f, 0.2f, 0.2f), false);
							waypointsAnimation.Insert(counter, waypointGO.transform);
							startingPosition += forwardToTarget * segmentDistance;
							counter++;
						}
					}
					ApplyAnimation(waypointsAnimation);					
				}
			}
			if (nameEvent.Equals(NarrationToken.EventNarrationTokenEnd))
			{
				bool mainNarration = (bool)parameters[0];
				int poiIndex = (int)parameters[1];
				string endEventNarration = (string)parameters[2];
			}
			if (nameEvent.Equals(EventTourGuideViewSpeakActivation))
			{
				bool isActivated = (bool)parameters[0];
				if (isActivated)
				{
					SetNarrationAudioSource = SoundsController.Instance.GetChannelAudioSource(SoundsController.ChannelsAudio.FX1);	
				}
				else
				{					
					SetNarrationAudioSource = null;
					SoundSpectrumAnalyzer.Instance.Stop();
				}
			}
			if (nameEvent.Equals(ScreenReplayingPOIView.EventScreenReplayingPOIViewSignalDestruction))
			{
				POIData currPOI = (POIData)parameters[0];				
				DestroyThingsBuildDuringNarration(currPOI);
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerDoRestart))
			{
				POIData currPOI = (POIData)parameters[0];
				SetPositionOutsideNarration(currPOI.GOPosition.transform.position);
				RotateFixedTowardsTarget(PositionPlayer);
				Model.SetActive(true);
				DestroyThingsBuildDuringNarration(currPOI);
			}
		}

		private void OnUIEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(GameStateRun.EventGameStateRunEndCurrentNarration))
			{				
				POIData targetPOI = (POIData)parameters[0];
				DestroyThingsBuildDuringNarration(targetPOI);
			}
        }

		private void DestroyThingsBuildDuringNarration(POIData targetPOI)
		{
			if (targetPOI == null) return;
			if (targetPOI.Root == null) return;
		}

		public void ApplyIdleAnimation()
		{
			ChangeGuideAnimation(AnimationIdle);
		}

		private void ChangeState(TourGuideStates newState)
		{
			_state = newState;
			_aligned = false;
			switch (_state)
			{
				case TourGuideStates.Idle:
					ChangeGuideAnimation(AnimationIdle);
					break;
				case TourGuideStates.WaitForPlayerNear:
					ChangeGuideAnimation(AnimationIdle);
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
					SystemEventController.Instance.DelaySystemEvent(ScreenInfoNextButtonView.EventScreenInfoNextButtonViewVisibilityContent, 0.1f, true);
#endif						
					break;
				case TourGuideStates.Navigation:
					ChangeGuideAnimation(AnimationRun);					
					_waypointFollower.Stop();

					iTween.Stop(this.gameObject);
					_hasSynchronized = false;

					InitNavigationPath(true);
					
					_delayToSync = 0.1f;
					break;

				case TourGuideStates.Moving:
					break;

				case TourGuideStates.Bezier:
					ChangeGuideAnimation(AnimationRun);					
					iTween.Stop(this.gameObject);
					break;

				case TourGuideStates.Talk:				
					ChangeGuideAnimation(AnimationTalk, true);
					break;
			}
		}

		private bool RotateProgressTowardsTarget(Vector3 target, float speed)
		{
			Vector3 toCamera = target - this.transform.position;

			this.transform.rotation = MainController.Instance.LevelView.TourGuideWorld.transform.rotation;

			Quaternion localTargetRotation = Quaternion.identity;
			if (IsNormalAxis)
			{
				localTargetRotation = Quaternion.Euler(new Vector3(0,yourvrexperience.Utils.Utilities.GetAngleFromNormal(new Vector2(toCamera.x, toCamera.z)),0));
				Model.transform.localRotation = Quaternion.Lerp(Model.transform.localRotation, localTargetRotation, speed * Time.deltaTime);
			}
			else
			{
				localTargetRotation = Quaternion.Euler(new Vector3(0,yourvrexperience.Utils.Utilities.GetAngleFromNormal(new Vector2(toCamera.x, toCamera.y)),0));
				Model.transform.localRotation = Quaternion.Lerp(Model.transform.localRotation, localTargetRotation, speed * Time.deltaTime);
			}

			return Quaternion.Angle(Model.transform.localRotation, localTargetRotation) <= 0.01f;
		}

		private void RotateFixedTowardsTarget(Vector3 target)
		{
			if (this == null) return;
			if (MainController.Instance.LevelView == null) return;
			if (MainController.Instance.LevelView.TourGuideWorld == null) return;
			
			Vector3 toCamera = target - this.transform.position;

			this.transform.rotation = MainController.Instance.LevelView.TourGuideWorld.transform.rotation;
			
			if (IsNormalAxis)
			{
				Model.transform.localRotation = Quaternion.Euler(new Vector3(0,yourvrexperience.Utils.Utilities.GetAngleFromNormal(new Vector2(toCamera.x, toCamera.z)),0));
			}
			else
			{
				Model.transform.localRotation = Quaternion.Euler(new Vector3(0,yourvrexperience.Utils.Utilities.GetAngleFromNormal(new Vector2(toCamera.x, toCamera.y)),0));
			}
		}

		private void UpdateAlignment(Vector3 target, float speed)
		{
			if (!_aligned && _initialAlignmentCompleted)
			{
				_aligned = RotateProgressTowardsTarget(target, speed);
			}
			else
			{
				RotateFixedTowardsTarget(target);
			}
		}

		void Update()
		{
			if (!_runUpdate)
			{
				RotateFixedTowardsTarget(PositionPlayer);
				return;
			} 

			if (this.transform.parent != NavMeshController.Instance.AreaMaxST)
			{
				this.transform.parent = NavMeshController.Instance.AreaMaxST;
			}

			switch (_state)
			{
				case TourGuideStates.Idle:
					UpdateAlignment(PositionPlayer, RotationSpeedFixed);
					break;

				case TourGuideStates.WaitForPlayerNear:
					float distanceToPlayer = 0;
					UpdateAlignment(PositionPlayer, RotationSpeedFixed);
					if (IsNormalAxis)
					{
						distanceToPlayer = yourvrexperience.Utils.Utilities.DistanceXZ(PositionPlayer, this.transform.position);
					}
					else
					{
						distanceToPlayer = yourvrexperience.Utils.Utilities.DistanceXY(PositionPlayer, this.transform.position);
					}					

					if (distanceToPlayer < GameLevelData.Instance.DistanceToTriggerGuide)
					{
						ChangeState(TourGuideStates.Idle);
						SystemEventController.Instance.DispatchSystemEvent(EventTourGuideViewReportPlayerClose);
						_initialAlignmentCompleted = true;
					}
					break;

				case TourGuideStates.Navigation:
					if (_navigation != null)
					{
						if (_navigation.NavigationRunning)
						{
							_refNavigationHelper.transform.position = _navigation.transform.position;
							Vector3 posNavigation = NavMeshController.Instance.ConvertNavigationToARWorld(_refNavigationHelper.transform.localPosition, false);
							if (!_hasSynchronized)
							{
								_delayToSync -= Time.deltaTime; 
								if (_delayToSync <= 0)
								{
									Vector3 stepNav = (posNavigation - this.transform.position).normalized * _navigation.NavigationSpeed;
									UpdateAlignment(this.transform.position + stepNav, RotationSpeedMovement);
									this.transform.position += stepNav * Time.deltaTime;									
									if (Vector3.Distance(posNavigation, this.transform.position) < 0.1f)
									{
										_hasSynchronized = true;
										this.transform.position = posNavigation;
									}
								}
							}					
							else
							{
								UpdateAlignment(posNavigation, RotationSpeedMovement);
								this.transform.position = posNavigation;
							}
						}
						else
						{
							ChangeState(TourGuideStates.Moving);
						}
					}
					break;

				case TourGuideStates.Moving:
					float distanceToTarget = Vector3.Distance(_targetGO, this.transform.position);
					Vector3 stepMove = (_targetGO - this.transform.position).normalized * Speed;
					if (distanceToTarget < 0.5f)
					{
						UpdateAlignment(PositionPlayer, RotationSpeedMovement);
					}
					else
					{
						UpdateAlignment(_targetGO, RotationSpeedFixed);
					}					
					this.transform.position += stepMove * Time.deltaTime;
					if (Vector3.Distance(_targetGO, this.transform.position) < 0.02f)
					{
						this.transform.position = _targetGO;
						ChangeState(TourGuideStates.WaitForPlayerNear);
						SystemEventController.Instance.DispatchSystemEvent(EventTourGuideViewReachedTarget);
					}
					break;

				case TourGuideStates.Bezier:
					RotateFixedTowardsTarget(_targetBezier);
					break;

				case TourGuideStates.Talk:
					UpdateAlignment(PositionPlayer, RotationSpeedFixed);
					break;
			}
		}
    }
}

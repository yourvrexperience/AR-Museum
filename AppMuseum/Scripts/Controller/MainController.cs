using yourvrexperience.Utils;
using UnityEngine;
using yourvrexperience.VR;
using System.Collections.Generic;
using yourvrexperience.Narration;
using static yourvrexperience.Narration.NarrationController;
using yourvrexperience.ai;
using static yourvrexperience.Narration.GameLevelData;
using yourvrexperience.speech;
using yourvrexperience.UserManagement;
using yourvrexperience.Social;

#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
using UnityEngine.XR;
#endif
using yourvrexperience.Networking;

namespace yourvrexperience.template6dof
{
	public class MainController : MonoBehaviour
	{
		public const string PLAYERPREFS_LOCAL_ENCRYPTION = "sEcreT-fCky-UseR-ManAGemeNT";
		public const string EncryptionLocalAESKey = "TKpaBoVcaEiIWKci";

		public enum StatesGame { None = 0, Splash, Download, MainMenu, Settings, Floor, Network, Connecting, Loading, Run, Pause, ReleaseMemory }

		public const string EventMainControllerReleaseGameResources = "EventMainControllerReleaseGameResources";
		public const string EventMainControllerGameReadyToStart = "EventMainControllerGameReadyToStart";
		public const string EventMainControllerChangeState = "EventMainControllerChangeState";
		public const string EventMainControllerRequestState = "EventMainControllerRequestState";
		public const string EventMainControllerResponseState = "EventMainControllerResponseState";
		public const string EventMainControllerRequestLoadLevel = "EventMainControllerRequestLoadLevel";
		public const string EventMainControllerResponseLoadLevel = "EventMainControllerResponseLoadLevel";
		public const string EventMainControllerChangeCurrentLevel = "EventMainControllerChangeCurrentLevel";
		public const string EventMainControllerLocalPlayerViewAssigned = "EventMainControllerLocalPlayerViewAssigned";
		public const string EventMainControllerAllPlayerViewReadyToStartGame = "EventMainControllerAllPlayerViewReadyToStartGame";
		public const string EventMainControllerReportPlayerScore = "EventMainControllerReportPlayerScore";
		public const string EventMainControllerAllPlayersScoresReported = "EventMainControllerAllPlayersScoresReported";

        private static MainController _instance;

        public static MainController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(MainController)) as MainController;
                }
                return _instance;
            }
        }

		[SerializeField] private GameObject arMaxSTCamera;		
		[SerializeField] private GameObject arVuforiaCamera;	
		[SerializeField] private GameObject arNianticCamera;	
			
		[SerializeField] private GameLevelData narrationData;
		[SerializeField] private GameAIData aiData;
		[SerializeField] private Template6DOFAIData template6DOFAIData;		
		[SerializeField] private GameObject inputPanelTextAction;
		[SerializeField] private GameObject desktopPlayer;
		[SerializeField] private GameObject VRPlayer;
		[SerializeField] private GameObject PlayerViewHandLeftPrefab;
		[SerializeField] private GameObject PlayerViewHandRightPrefab;
		[SerializeField] private GameObject MenuLevel;
		[SerializeField] private string[] GameLevel;
		[SerializeField] private GameObject[] LevelPrefabs;
		[SerializeField] private UnityEngine.Object[] ARMaps;		

		[SerializeField] private GameObject CameraFade;
		[SerializeField] private GameObject BulletsController;
		[SerializeField] private GameObject FXsController;
		[SerializeField] private GameObject SplineController;
		[SerializeField] private Material SkyBoxMenu;
		[SerializeField] private Material SkyBoxGame;

		[SerializeField] private GameObject NarrationPrefabController;

		[SerializeField] private GameObject TourGuidePrefab;
		[SerializeField] private GameObject GoalTarget;

		[SerializeField] private GameObject POIVideo;
		[SerializeField] private GameObject POIPhotos;
		[SerializeField] private GameObject POIModel3D;

		[SerializeField] private GameObject EditionHighlightedPOI;
		[SerializeField] private GameObject EditionSelectedPOI;
		[SerializeField] private GameObject POIBase;
		[SerializeField] private GameObject EasterEggBase;
		[SerializeField] private Material occlusionMaterial;
		[SerializeField] private Material occlusionVuforia;		
		
		private IGameState _gameState;
		private IInputController _inputController;
		private PlayerView _playerView;
		private LevelView _levelView;
		private StatesGame _state;
		private StatesGame _previousState;
		private CameraFader _cameraFader;

		private bool _inputInited = false;
		private bool _screenInited = false;
		private bool _requestCreation = false;
		private int _numberClients = -1;
		private int _currentGameLevel = 0;

		private bool _isARMode = false;
		private GameObject _referenceEasterEgg;
		private float _rangeDetectionEasterEggPlaying = -1;

		private int _currentAreaGame = -1;
		private string _currentAreaName;
		private TourGuideView _tourGuideView;
		private bool _completedArea = false;	
		private bool _mainNarrationPlaying = false;
		private bool _initialPositioningDone = false;
		private bool _isMultiplayer = true;
		private bool _enableEditionPOIs = false;
		private int _currentNarrationPOI = -1;
		private bool _blockCameraMovement = false;

		private Vector3 _aeralCameraForward = Vector3.zero;

		private GameObject _highlightedPOI;
		private GameObject _selectedPOI;

		public IInputController GameInputController
		{
			get { return _inputController; }
		}
		public PlayerView PlayerView
		{
			get { return _playerView; }
		}
		public LevelView LevelView
		{
			get { return _levelView; }
		}
		public StatesGame State
		{
			get { return _state; }
		}
		public StatesGame PreviousState
		{
			get { return _previousState; }
		}
		public int CurrentGameLevel
		{
			get { return _currentGameLevel; }
			set { _currentGameLevel = value; }
		}
		public int NumberClients
		{
			get {  return _numberClients; }
			set { _numberClients = value; }
		}
		public bool IsARMode
		{
			get { 
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR
					return false;
#else
					return _isARMode; 
#endif
			}
		}
		
		public bool IsNormalAxis
		{
			get {
#if ENABLE_NREAL
				return false; 
#elif UNITY_EDITOR || ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || UNITY_WEBGL			
				return true; 
#elif ENABLE_VUFORIA || ENABLE_NIANTIC				
				return true;
#else
				return false; 
#endif				
			}
		}		
		public TourGuideView GuideTourView
		{
			get { return _tourGuideView; }
		}
		public GameObject ReferenceEasterEgg
		{
			get { return _referenceEasterEgg; }
			set { _referenceEasterEgg = value;}
		}
		public float RangeDetectionEasterEggPlaying
		{
			get { return _rangeDetectionEasterEggPlaying; }
			set { _rangeDetectionEasterEggPlaying = value;}
		}
		public bool IsMultiplayer
		{
			get { return _isMultiplayer; }
			set { _isMultiplayer = value; }
		}
		public bool EnableEditionPOIs
		{
			get { return _enableEditionPOIs; }
			set { _enableEditionPOIs = value; }
		}		
		public bool CompletedArea
		{
			get { return _completedArea; }
			set {
				bool prevCompletedArea = _completedArea;
				_completedArea = value;
				if (!prevCompletedArea && _completedArea)
				{
#if ENABLE_ANALYTICS
					if (!MainController.Instance.EnableEditionPOIs)
					{
						TourAnalyticsController.Instance.SceneCompletedEvent(GameLevelData.Instance.Age, (int)_currentAreaGame, _currentAreaName, GameLevelData.Instance.TotalTimeDone);
					}					
#endif					
				}
			}
		}
		public bool MainNarrationPlaying
		{
			get { return _mainNarrationPlaying; }
		}
		public bool InitialPositioningDone
		{
			set { _initialPositioningDone = value; }
		}		
		private Vector3 AeralCameraForward
		{
			get { return _aeralCameraForward; }
			set { _aeralCameraForward = value; }
		}
		public GameObject HighlightedPOI
		{
			get { return _highlightedPOI; }
		}
		public GameObject SelectedPOI
		{
			get { return _selectedPOI; }
		}
		public GameObject GetARWorldCamera()
        {
#if ENABLE_VUFORIA
			return arVuforiaCamera;
#elif ENABLE_NIANTIC
			return arNianticCamera;
#else
			return arMaxSTCamera;
#endif			
		}
		public int CurrentNarrationPOI
		{
			get { return _currentNarrationPOI; }
		}
		public bool BlockCameraMovement
		{
			get { return _blockCameraMovement; }
			set { _blockCameraMovement = value; }
		}

		private void SetNianticController(bool active)
		{
			arNianticCamera.transform.root.gameObject.SetActive(active);
		}

		void Awake()
		{
#if (UNITY_EDITOR || ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || UNITY_WEBGL)
			arMaxSTCamera.SetActive(false);
#if ENABLE_VUFORIA
			arVuforiaCamera.SetActive(true);
#else
			arVuforiaCamera.SetActive(false);
#endif			
			SetNianticController(false);
#elif ENABLE_VUFORIA
			_isARMode = true;
			arVuforiaCamera.AddComponent<InputController>();
			arVuforiaCamera.SetActive(true);
			arMaxSTCamera.SetActive(false);
			SetNianticController(false);
#elif ENABLE_NIANTIC
			_isARMode = true;
			arNianticCamera.transform.root.gameObject.AddComponent<InputController>();
			SetNianticController(true);
			arMaxSTCamera.SetActive(false);	
			arVuforiaCamera.SetActive(false);		
#else
			_isARMode = true;
			arMaxSTCamera.AddComponent<InputController>();
			arMaxSTCamera.SetActive(true);	
			arVuforiaCamera.SetActive(false);
			SetNianticController(false);	
#endif

			narrationData.Initialize();
			aiData.Initialize();			
			template6DOFAIData.Initialize();
			CommController.Instance.Init();
			UsersController.Instance.Initialize(PLAYERPREFS_LOCAL_ENCRYPTION, EncryptionLocalAESKey);
			CameraXRController.Instance.Initialize();

			SystemEventController.Instance.Event += OnSystemEvent;
		}

		void Start()
		{									
			RenderSettings.skybox = SkyBoxMenu;

			NetworkController.Instance.NetworkEvent += OnNetworkEvent;
			ScreenController.Instance.Initialize();
			SpeechDatabaseController.Instance.Initialize();

#if ENABLE_ANALYTICS
			TourAnalyticsController.Instance.Initialize();
#endif			
			CreateCameraFader();
		}

        void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
		}

		public PanelInputTextAction CreateInputActionEditText()
		{
			PanelInputTextAction output = null;
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			output = GameObject.FindAnyObjectByType<PanelInputTextAction>();
			if (output == null)
			{
				if (inputPanelTextAction != null)
				{
					output = Instantiate(inputPanelTextAction).GetComponent<PanelInputTextAction>();		
					output.Initialize();
				}
			}			
#endif		
			return output;
		}

		public POIBaseView CreatePOIBase()
		{
			return Instantiate(POIBase).GetComponent<POIBaseView>();
		}

		public EasterEggBaseView CreateSecretBase()
		{
			return Instantiate(EasterEggBase).GetComponent<EasterEggBaseView>();
		}

		public void FadeInCamera()
		{
			if (_cameraFader != null)
			{
				_cameraFader.FadeIn();
			}		
		}

		public void FadeOutCamera()
		{
			if (_cameraFader != null)
			{
				_cameraFader.FadeOut();
			}		
		}

		private void DestroyPreviousHighlightedPOI()
		{
			if (_highlightedPOI != null)
			{
				GameObject.Destroy(_highlightedPOI);
				_highlightedPOI = null;
			}
			if (_selectedPOI != null)
			{
				GameObject.Destroy(_selectedPOI);
				_selectedPOI = null;
			}
		}

		public void CreateMenuLevelView()
		{
			if (_levelView == null)		
			{
				_levelView = (Instantiate(MenuLevel) as GameObject).GetComponent<LevelView>();
			}
			_levelView.transform.position = Vector3.zero;

			RenderSettings.skybox = SkyBoxMenu;
		}

		public void CreateGameElementsView()
		{
			if (_requestCreation) return;
			_requestCreation = true;

			GameLevelData.Instance.CurrentScore = 0;
			GameLevelData.Instance.CurrentTime = 0;

			if (_playerView == null)
			{			
				if (!_isMultiplayer)
				{										
#if ENABLE_OCULUS || ENABLE_OPENXR
					Instantiate(VRPlayer, Vector3.zero, Quaternion.identity);
#else
					Instantiate(desktopPlayer, Vector3.zero, Quaternion.identity);
#endif					
				}
				else
				{
#if ENABLE_OCULUS || ENABLE_OPENXR
					NetworkController.Instance.CreateNetworkPrefab(false, VRPlayer.name, VRPlayer.gameObject, "GameElements\\Player\\" + VRPlayer.name, Vector3.zero, Quaternion.identity, 0);
#else
					NetworkController.Instance.CreateNetworkPrefab(false, desktopPlayer.name, desktopPlayer.gameObject, "GameElements\\Player\\" + desktopPlayer.name, Vector3.zero, Quaternion.identity, 0);
#endif					
				}
			}

			Instantiate(BulletsController);
			Instantiate(FXsController);

			if (EnableEditionPOIs)
			{
				_highlightedPOI = Instantiate(EditionHighlightedPOI);
				_selectedPOI = Instantiate(EditionSelectedPOI);
				_highlightedPOI.SetActive(false);
				_selectedPOI.SetActive(false);
			}

			RenderSettings.skybox = SkyBoxGame;

			TourAnalyticsController.Instance.Floor = _currentGameLevel;
			if (UsersController.Instance.CurrentUser != null)
			{
				TourAnalyticsController.Instance.Email = UsersController.Instance.CurrentUser.Email;
			}
			else
			{
				TourAnalyticsController.Instance.Email = "";
			}
			TourAnalyticsController.Instance.Language = LanguageController.Instance.CodeLanguage;			
		}

		private void UpdateNarration()
		{
			if (_currentAreaGame != _currentGameLevel)
			{
				_currentAreaGame = _currentGameLevel;
				GameLevelData.Instance.NextAreaGame = _currentAreaGame;				
				TextAsset finalText = GameLevelData.Instance.GetLevelNarration(GameLevelData.Instance.GetLevel(GameLevelData.Instance.Age, _currentAreaGame));
				_currentAreaName = "Narration" + _currentAreaGame;
#if ENABLE_ANALYTICS  					
				if (!MainController.Instance.EnableEditionPOIs)
				{
					TourAnalyticsController.Instance.SceneLoadedEvent(GameLevelData.Instance.Age, _currentAreaGame, _currentAreaName);
				}				
#endif			
				NarrationController narration = Instantiate(NarrationPrefabController).GetComponent<NarrationController>();
				narration.LoadNarrationTexts(finalText, true);
			}
		}

		public void CreateCameraFader()
		{
			if (_cameraFader == null)
			{
				_cameraFader = (Instantiate(CameraFade) as GameObject).GetComponent<CameraFader>();
			}
			if ((_inputController != null) && (_inputController.Camera != null))
			{
				_cameraFader.transform.parent = _inputController.Camera.gameObject.transform;
			}
			else
			{
				if (Camera.main != null)
				{
					_cameraFader.transform.parent = Camera.main.transform;
				}				
			}
			_cameraFader.transform.localPosition = Vector3.zero;
		}

		private void InitializeSystem(bool force)
		{
			if (((_state == StatesGame.None) && (_inputInited) && (_screenInited)) || force)
			{
				CreateCameraFader();
				ChangeGameState(StatesGame.Splash);	
			}
		}

		private void InitCurrentGameLevel(int level)
		{
			if (_levelView == null)
			{
				GameObject newLevel = null;
				if (LevelPrefabs != null)
				{
					if (LevelPrefabs.Length > GameLevelData.Instance.GetLevel(level))
					{
						if (LevelPrefabs[GameLevelData.Instance.GetLevel(level)] != null)
						{
							newLevel = Instantiate(LevelPrefabs[GameLevelData.Instance.GetLevel(level)]);
						}
					}
				}
				if (newLevel == null)
				{					
					int levelIndex = GameLevelData.Instance.GetLevel(level);
					newLevel = AssetBundleController.Instance.CreateGameObject(GameLevel[levelIndex]);
				}
				_levelView = newLevel.GetComponent<LevelView>();
			}
			_levelView.transform.position = Vector3.zero;
			UpdateNarration();
		}

		public void CreateTourGuide(Vector3 position)
		{
			if (_tourGuideView == null)
			{
				_tourGuideView = Instantiate(TourGuidePrefab, position, Quaternion.identity).GetComponent<TourGuideView>();
				_tourGuideView.Initialize();
			}
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(NarrationToken.EventNarrationTokenCreateNarrationObject))
			{
				bool isMainNarration = (bool)parameters[0];
				TypeObjectNarration typeNarrationObj = (TypeObjectNarration)parameters[1];
				string assetName = (string)parameters[2];
				Vector3 posObj = (Vector3)parameters[3];
				Quaternion rotObj = (Quaternion)parameters[4];
				Vector3 scaleObj = (Vector3)parameters[5];
				string animationObj = (string)parameters[6];
				switch (typeNarrationObj)
				{
					case TypeObjectNarration.Image:
						string[] photos =  assetName.Split(',');
						MainController.Instance.CreatePhotoGalleryController(!isMainNarration, photos, NavMeshController.Instance.AreaMaxST.transform, posObj, rotObj, scaleObj);
						break;

					case TypeObjectNarration.Video:
						MainController.Instance.CreateVideoController(!isMainNarration, assetName, NavMeshController.Instance.AreaMaxST.transform, posObj, rotObj, scaleObj, true, false);
						break;

					case TypeObjectNarration.Model3D:
						MainController.Instance.CreateModel3DController(!isMainNarration, assetName, NavMeshController.Instance.AreaMaxST.transform, posObj, rotObj, scaleObj, animationObj);	
						break;						

					case TypeObjectNarration.Interaction:
						GameObject interactable = MainController.Instance.CreateInteractable(assetName, NavMeshController.Instance.AreaMaxST.transform, posObj, rotObj, scaleObj);
						interactable.GetComponent<IGameInteractables>().Play();
						break;

					case TypeObjectNarration.Sound:
						AudioClip audioSegment = AssetBundleController.Instance.CreateAudioclip(assetName);
						SoundsController.Instance.PlaySoundClipFx(SoundsController.ChannelsAudio.FX3, audioSegment, false, 0.5f);
						break;
				}
			}
			if (nameEvent.Equals(EventMainControllerChangeCurrentLevel))
			{
				int nextGameLevel = (int)parameters[0];
				if (nextGameLevel != _currentGameLevel)
				{
					_currentGameLevel = nextGameLevel;
					SystemEventController.Instance.DelaySystemEvent(EventMainControllerResponseLoadLevel, 0.1f, _currentGameLevel);
					_levelView = null;
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerDisconnectParent);					
					SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerReleaseAllResources);
					SystemEventController.Instance.DispatchSystemEvent(NavMeshController.EventNavMeshControllerReleaseResources);				
					SystemEventController.Instance.DispatchSystemEvent(LevelView.EventLevelViewDestroy);		
					SystemEventController.Instance.DispatchSystemEvent(NavigationAgentView.EventNavigationAgentViewRelease);
				}
			}
			if (nameEvent.Equals(EventMainControllerResponseLoadLevel))
			{
				int currentGameLevel = (int)parameters[0];
				_currentGameLevel = currentGameLevel;
				InitCurrentGameLevel(_currentGameLevel);
			}
			if (nameEvent.Equals(InputController.EventInputControllerHasStarted))
			{
				_inputController = ((GameObject)parameters[0]).GetComponent<IInputController>();
				_inputController.Initialize();
				_inputInited = true;
#if UNITY_EDITOR || UNITY_WEBGL
				_isARMode = false;
#else
#if ENABLE_VUFORIA
				_isARMode = arVuforiaCamera.activeSelf;
#elif ENABLE_NIANTIC
				_isARMode = arNianticCamera.activeSelf;
#else
				_isARMode = arMaxSTCamera.activeSelf;
#endif
#endif
#if  ENABLE_NREAL && !UNITY_EDITOR && !UNITY_WEBGL
				_isARMode = true;
				arMaxSTCamera.gameObject.SetActive(true);
				ARMaxSTController.Instance.ARMaxSTCamera.enabled = false;
#elif !UNITY_EDITOR && !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || UNITY_WEBGL)
#if ENABLE_VUFORIA
				_inputController.Camera =  VuforiaController.Instance.ARVuforiaCamera;
#elif ENABLE_NIANTIC
				_inputController.Camera = NianticController.Instance.ARNianticCamera;
#else
				_inputController.Camera = ARMaxSTController.Instance.ARMaxSTCamera;
#endif				
#endif			
				InitializeSystem(false);
			}
			if (nameEvent.Equals(ScreenController.EventScreenControllerStarted))
			{
				_screenInited = true;
				InitializeSystem(false);
			}			
			if (nameEvent.Equals(GameStateMenu.EventGameStateMenuQuitGame))
			{
				Application.Quit();
			}
			if (nameEvent.Equals(EventMainControllerReleaseGameResources))
			{
				_levelView = null;
				_playerView = null;
				_requestCreation = false;
				_hasStartedSession = false;
				_players.Clear();
				_tourGuideView = null;		
				_mainNarrationPlaying = false;	
				_currentAreaGame = -1;
				DestroyPreviousHighlightedPOI();
				SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerReleaseAllResources);
			}
			if (nameEvent.Equals(PlayerView.EventPlayerAppHasStarted))
			{
				PlayerView player = (PlayerView)parameters[0];
				if (!_isMultiplayer)
				{
					_playerView = player;
					player.Initialize();

#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
					Instantiate(PlayerViewHandLeftPrefab);
					Instantiate(PlayerViewHandRightPrefab);
#endif
					InitCurrentGameLevel(_currentGameLevel);
					SystemEventController.Instance.DelaySystemEvent(EventMainControllerAllPlayerViewReadyToStartGame, 1);
				}
				else
				{
					if (!player.NetworkGameIDView.AmOwner())
					{
						player.Initialize();
					}
					else
					{					
						_playerView = player;

						if (_playerView != null)
						{
							_playerView.Initialize();

							NetworkController.Instance.DelayNetworkEvent(EventMainControllerRequestLoadLevel, 0.01f, -1, -1, NetworkController.Instance.UniqueNetworkID);
							SystemEventController.Instance.DispatchSystemEvent(EventMainControllerLocalPlayerViewAssigned);
						
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
							NetworkController.Instance.CreateNetworkPrefab(false, PlayerViewHandLeftPrefab.name, PlayerViewHandLeftPrefab.gameObject, "GameElements\\Player\\" + PlayerViewHandLeftPrefab.name, new Vector3(0, 0, 0), Quaternion.identity, 0);
							NetworkController.Instance.CreateNetworkPrefab(false, PlayerViewHandRightPrefab.name, PlayerViewHandRightPrefab.gameObject, "GameElements\\Player\\" + PlayerViewHandRightPrefab.name, new Vector3(0, 0, 0), Quaternion.identity, 0);
#endif
						}
					}
					if (!_players.ContainsKey(player))
					{
						_players.Add(player, 0);
					}
				}
			}
			if (nameEvent.Equals(PlayerHandView.EventPlayerViewHandHasStarted))
			{
				PlayerHandView playerAppView = (PlayerHandView)parameters[0];
				if (_isMultiplayer)
				{
					if (playerAppView.NetworkGameIDView.AmOwner())
					{
						playerAppView.Player = _playerView;
					}
				}
			}
			if (nameEvent.Equals(LevelView.EventLevelViewStarted))
			{
				Vector3 position = _levelView.InitialPosition.transform.position;
				Quaternion orientation = _levelView.InitialPosition.transform.rotation;
				if (_playerView == null)
				{
					if (_inputController != null)
					{
						_inputController.Camera.transform.position = position;
						_inputController.Camera.transform.rotation = orientation;
					}
					else
					{
						Camera.main.transform.position = position;
						Camera.main.transform.rotation = orientation;
					}
					SystemEventController.Instance.DelaySystemEvent(PlayerView.EventPlayerViewPositionUpdated, 0.2f);
				}
				else
				{
					if (!_initialPositioningDone)
					{
						_playerView.transform.position = position;
						_playerView.transform.rotation = orientation;
#if UNITY_EDITOR && !((ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || UNITY_WEBGL))
						_inputController.Camera.transform.rotation = Quaternion.Euler(new Vector3(0, _levelView.InitialPosition.transform.eulerAngles.y, 0));
#endif
					}
				}				
				if (!_initialPositioningDone)
				{
					_initialPositioningDone = true;					
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
#if ENABLE_OCULUS
					VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerResetToInitial, position, orientation);
#elif ENABLE_OPENXR
					VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerResetToInitial, position + new Vector3(0, -1, 0), orientation);
#else
					VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerResetToInitial, position, orientation);
#endif					
#else				
#endif					
					SystemEventController.Instance.DelaySystemEvent(PlayerView.EventPlayerViewPositionUpdated, 0.2f);
				}				
			}
			if (nameEvent.Equals(NarrationToken.EventNarrationTokenStart))
			{
				bool isMainNarration = (bool)parameters[0];
				if (isMainNarration)
				{
					_mainNarrationPlaying = true;
				}
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerFinished))
			{
				_mainNarrationPlaying = false;
			}		
			if (nameEvent.Equals(NarrationController.EventNarrationControllerPlayPOIByIndex))
			{
				if (_currentNarrationPOI < (int)parameters[0])
				{
					_currentNarrationPOI = (int)parameters[0];
				}				
			}	
			if (nameEvent.Equals(GameLevelData.EventGameLevelDataRefreshLocalData))
			{
				if (MainController.Instance.EnableEditionPOIs)
				{
					MainController.Instance.SaveEditionPOIs(false);
				}				
			}
        }

		private bool _changeStateRequested = false;
		private bool _hasStartedSession = false;
		private bool _isHost = false;
		private string _roomName = "RoomName";
		private PlayerView _localPlayer;
		private Dictionary<PlayerView, int> _players = new Dictionary<PlayerView, int>();

		public string RoomName 
		{
			get { return _roomName; }
			set { _roomName = value; }
		}

		public Dictionary<PlayerView, int> PlayersNetwork
		{
			get { return _players; }
		}

		public void SaveEditionPOIs(bool shouldUpdateDatabase = true)
		{			
			int currID = GameLevelData.Instance.GetLevel(MainController.Instance.CurrentGameLevel);
			GameAge currAge = GameLevelData.Instance.Age;
			int currLevel = MainController.Instance.CurrentGameLevel;
			string poisData = MainController.Instance.LevelView.PackPOIsContent();
			string secretsData = MainController.Instance.LevelView.PackSecretsContent();
			string narrationData = GameLevelData.Instance.GetLevelNarration(currID).text;
			if (UsersController.Instance.CurrentUser != null)
			{
				if (!UsersController.Instance.CurrentUser.IsEmptyUser())
				{
					if (UsersController.Instance.CurrentUser.Admin)
					{					
						GameLevelData.Instance.InsertPOIs((int)UsersController.Instance.CurrentUser.Id, UsersController.Instance.CurrentUser.PasswordPlain, currID, (int)currAge, currLevel, poisData, secretsData, narrationData, shouldUpdateDatabase);
					}
				}
			}			
		}

		protected virtual void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			if (nameEvent.Equals(NetworkController.EventNetworkControllerListRoomsConfirmedUpdated))
			{
				if (!_hasStartedSession)
				{
					_hasStartedSession = true;
#if ENABLE_MIRROR
					NetworkController.Instance.JoinRoom(_roomName);
#else
					if (NetworkController.Instance.RoomsLobby.Count == 0) 
					{
#if ENABLE_MIRROR						
						NumberClients = -1;
#else
						NumberClients = 10;
#endif						
						NetworkController.Instance.CreateRoom(_roomName, NumberClients);
					}
					else 
					{
						NetworkController.Instance.JoinRoom(_roomName);
					}
#endif					
				}
			}
			if (nameEvent.Equals(NetworkController.EventNetworkControllerConfirmationConnectionWithRoom))
			{
				if (_state == StatesGame.Connecting)
				{
					ChangeGameState(StatesGame.Loading);
				}
				yourvrexperience.Utils.Utilities.DebugLogColor("JOINED ROOM WITH ID["+(int)parameters[0]+"] OF A TOTAL OF CONNECTIONS[" + NetworkController.Instance.Connections.Count + "]", Color.red);
				if (NetworkController.Instance.IsServer)
				{
					NetworkController.Instance.DelayNetworkEvent(MainController.EventMainControllerGameReadyToStart, 0.2f, -1, -1);
				}
			}
			if (nameEvent.Equals(NetworkController.EventNetworkControllerNewPlayerJoinedRoom))
			{
				yourvrexperience.Utils.Utilities.DebugLogColor("NEW PLAYER["+(int)parameters[0]+"] JOINED TO THE ROOM", Color.red);
			}
			if (nameEvent.Equals(NetworkController.EventNetworkControllerPlayerDisconnected))
			{
				int netIDDisconnected = -1;
				if (parameters != null)
				{
					if (parameters.Length > 0)
					{
						netIDDisconnected = (int)parameters[0];
					}
				}
				foreach (KeyValuePair<PlayerView, int> playerConnected in _players)
				{
					if (playerConnected.Key != null)
					{
						if (playerConnected.Key.NetworkGameIDView.GetOwnerID() == netIDDisconnected)
						{
							_players.Remove(playerConnected.Key);
							GameObject.Destroy(playerConnected.Key.gameObject);
							yourvrexperience.Utils.Utilities.DebugLogColor("PLAYER["+netIDDisconnected+"] SUCCESSFULLY DESTROYED", Color.red);
						}
					}
				}				
			}		
			if (nameEvent.Equals(NetworkController.EventNetworkControllerDisconnected))
			{
				DestroyNetworkLevelObjects();
			}
			if (nameEvent.Equals(EventMainControllerRequestLoadLevel))
			{
				if (NetworkController.Instance.IsServer)
				{
					int netID = (int)parameters[0];
					NetworkController.Instance.DelayNetworkEvent(EventMainControllerResponseLoadLevel, 0.01f, -1, -1, netID, _currentGameLevel);
				} 
			}
			if (nameEvent.Equals(EventMainControllerResponseLoadLevel))
			{
				int netID = (int)parameters[0];
				int currentGameLevel = (int)parameters[1];
				if ((netID == NetworkController.Instance.UniqueNetworkID) || (netID == -1))
				{
					_currentGameLevel = currentGameLevel;
					InitCurrentGameLevel(_currentGameLevel);
					NetworkController.Instance.DispatchEvent(EventMainControllerAllPlayerViewReadyToStartGame);
				}
			}
			if (nameEvent.Equals(EventMainControllerChangeCurrentLevel))
			{
				int nextGameLevel = (int)parameters[0];
				if (nextGameLevel != _currentGameLevel)
				{
					if (NetworkController.Instance.IsServer)
					{
						_currentGameLevel = nextGameLevel;
						NetworkController.Instance.DelayNetworkEvent(EventMainControllerResponseLoadLevel, 0.1f, -1, -1, -1, _currentGameLevel);
					}
					_levelView = null;
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerDisconnectParent);					
					SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerReleaseAllResources);
					SystemEventController.Instance.DispatchSystemEvent(NavMeshController.EventNavMeshControllerReleaseResources);				
					SystemEventController.Instance.DispatchSystemEvent(LevelView.EventLevelViewDestroy);				
					SystemEventController.Instance.DispatchSystemEvent(NavigationAgentView.EventNavigationAgentViewRelease);
				}
			}
			if (nameEvent.Equals(EventMainControllerChangeState))
			{
				int newState = (int)parameters[0];
				_changeStateRequested = false;
				ChangeLocalGameState((StatesGame)newState);
			}
			if (nameEvent.Equals(EventMainControllerRequestState))
			{
				if (NetworkController.Instance.IsServer)
				{
					int netIDOrigin = (int)parameters[0];
					int newState = (int)parameters[1];
					if (netIDOrigin != NetworkController.Instance.UniqueNetworkID)
					{
						NetworkController.Instance.DelayNetworkEvent(EventMainControllerResponseState, 0.01f, -1, -1, netIDOrigin, newState);
					}
				}	
			}
			if (nameEvent.Equals(EventMainControllerResponseState))
			{
				if (!NetworkController.Instance.IsServer)
				{
					int netIDOrigin = (int)parameters[0];
					int newState = (int)parameters[1];
					if (netIDOrigin == NetworkController.Instance.UniqueNetworkID)
					{
						_changeStateRequested = false;
						ChangeLocalGameState((StatesGame)newState);
					}
				}	
			}
			if (nameEvent.Equals(EventMainControllerReportPlayerScore))
			{
				int playerID = (int)parameters[0];
				int scorePlayer = (int)parameters[1];
			}
			if (nameEvent.Equals(GameStateRun.EventGameStateKickOutUserFromApp))
			{
				int playerNetID = (int)parameters[0];
				foreach (KeyValuePair<PlayerView, int> playerNetwork in _players)
				{
					if (playerNetwork.Key.NetworkGameIDView.GetViewID() == playerNetID)
					{
						if (_players.Remove(playerNetwork.Key))
						{
							SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerViewDisableBody, playerNetID);
							SystemEventController.Instance.DispatchSystemEvent(ScreenPauseView.EventScreenPauseViewManageRefreshPlayerList);
							return;
						}
					}
				}
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerRequestReplayForAll))
			{
				int poiIndex = (int)parameters[0];
				SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerRequestReplayForAll, poiIndex);
			}
		}

		private void DestroyNetworkLevelObjects()
		{
			NetworkObjectID[] networkObjects = GameObject.FindObjectsOfType<NetworkObjectID>();
			foreach (NetworkObjectID netObjectID in networkObjects)
			{
				if (netObjectID != null)
				{
					string nameToDestroy = netObjectID.name;
					netObjectID.Destroy();
				}
			}
		}

		private void ChangeRemoteGameState(int newState)
		{
			if (!_changeStateRequested)
			{
				_changeStateRequested = true;
				if (NetworkController.Instance.IsServer)
				{
					NetworkController.Instance.DispatchNetworkEvent(EventMainControllerChangeState, NetworkController.Instance.UniqueNetworkID, -1, newState);
				}
				else
				{
					NetworkController.Instance.DispatchNetworkEvent(EventMainControllerRequestState, NetworkController.Instance.UniqueNetworkID, -1, NetworkController.Instance.UniqueNetworkID, newState);
				}
			}
		}

		public void CreateDestinationMarker(Transform target)
        {
			DestinationMarker destinationMarker = Instantiate(GoalTarget).GetComponent<DestinationMarker>();
			destinationMarker.Initialize(target);
		}

		public NarrationController CreateNarrationGeneric(string narrationData, bool aiNarration, bool autoDestroy)
		{
			NarrationController narrationGeneric = Instantiate(NarrationPrefabController).GetComponent<NarrationController>();
			narrationGeneric.LoadNarrationGeneric(narrationData, aiNarration, autoDestroy);
			return narrationGeneric;
		}

		public POIVideoController CreateVideoController(bool isEasterEgg, string video, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, bool shouldPlay, bool shouldMinimize)
		{
			POIVideoController poiVideo = Instantiate(POIVideo).GetComponent<POIVideoController>();
			poiVideo.Play(isEasterEgg, video, parent, position, rotation, scale, shouldPlay, shouldMinimize);
			return poiVideo;
		}

		public POIPhotoGalleryController CreatePhotoGalleryController(bool isEasterEgg, string[] photos, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale)
		{
			POIPhotoGalleryController poiPhotos = Instantiate(POIPhotos).GetComponent<POIPhotoGalleryController>();
			poiPhotos.Play(isEasterEgg, photos, parent, position, rotation, scale);
			return poiPhotos;
		}

		public POIModel3DController CreateModel3DController(bool isEasterEgg, string asset, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, string animation)
		{
			POIModel3DController poiModel3D = Instantiate(POIModel3D).GetComponent<POIModel3DController>();
			poiModel3D.Play(isEasterEgg, asset, parent, position, rotation, scale);
			poiModel3D.PlayAnimation(animation);
			if (poiModel3D.GetComponentInChildren<ParticleSystem>() != null)
			{
				poiModel3D.GetComponentInChildren<ParticleSystem>().Scale(poiModel3D.Scale.y, true);
			}
			return poiModel3D;
		}

		public GameObject CreateInteractable(string asset, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale)
		{
			GameObject interactable = AssetBundleController.Instance.CreateGameObject(asset);
			interactable.transform.parent = parent;
			interactable.transform.transform.localPosition = position;
			interactable.transform.transform.localRotation = rotation;
			interactable.transform.transform.localScale = scale;
			return interactable;
		}
		
		public GameObject CreateWaypoint(string asset, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, bool render = true)
		{
			GameObject waypoint = AssetBundleController.Instance.CreateGameObject(asset);
			waypoint.AddComponent<POINarratorDestroyer>().Init();
			waypoint.transform.parent = parent;
			waypoint.transform.transform.localPosition = position;
			waypoint.transform.transform.localRotation = rotation;
			waypoint.transform.transform.localScale = scale;			
			if ((UsersController.Instance.CurrentUser != null) && (UsersController.Instance.CurrentUser.Admin))
			{
				if (MainController.Instance.EnableEditionPOIs)
				{
					waypoint.SetActive(true);
				}
				else
				{
					waypoint.SetActive(false);
				}
			}
			else
			{
				waypoint.SetActive(false);
			}
			return waypoint;
		}

        public NarrationController CreateNarrationPreviousPOI(NarrationData previousNarration)
        {            
            NarrationController narrationPrevPOI = Instantiate(NarrationPrefabController).GetComponent<NarrationController>();
            narrationPrevPOI.LoadNarrationData(previousNarration);
            return narrationPrevPOI;
        }
		
		public List<Vector3> GetPathToTarget(Vector3 origin, Vector3 target)
		{
			if (NavMeshController.Instance.NavigationAgentProviderView != null)
			{
				return NavMeshController.Instance.NavigationAgentProviderView.GetPathToTarget(origin, target);
			}
			else
			{
				return null;
			}
		}

		public List<Vector3> GetPathToTarget(Vector3 origin, Vector3 target, float waypointDistance)
		{
			List<Vector3> waypoints = GetPathToTarget(origin, target);
			if ((waypointDistance > 0) && (waypoints.Count > 0))
			{
				Vector3 lastWaypoint = waypoints[waypoints.Count - 1];
				int indexWaypoint = 0;
				Vector3 currentWaypoint = waypoints[indexWaypoint];
				do 
				{
					indexWaypoint++;
					if (indexWaypoint < waypoints.Count)
					{
						Vector3 nextWaypoint = waypoints[indexWaypoint];
						if (Vector3.Distance(currentWaypoint, nextWaypoint) > waypointDistance)
						{
							Vector3 newWaypoint = currentWaypoint + ((nextWaypoint - currentWaypoint).normalized * waypointDistance);
							waypoints.Insert(indexWaypoint, newWaypoint);
						}
						currentWaypoint = waypoints[indexWaypoint];
					}
				} while (indexWaypoint < waypoints.Count);
			}
			return waypoints;
		}

		public UnityEngine.Object GetCurrentMap()
		{
			return ARMaps[GameLevelData.Instance.GetLevel(_currentGameLevel)];
		}

		public void ApplyOclusionNavigation()
        {
#if !(UNITY_EDITOR || ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
			WallOclussion[] wallsDetected = GameObject.FindObjectsOfType<WallOclussion>();
			foreach (WallOclussion eachGameObject in wallsDetected)
			{
#if ENABLE_VUFORIA						
				Renderer[] cullingRenderer = eachGameObject.GetComponentsInChildren<Renderer>();
				foreach (Renderer eachRenderer in cullingRenderer)
				{
					Material[] materials = eachRenderer.materials;
					for (int i = 0; i < eachRenderer.materials.Length; i++)
					{
						materials[i] = occlusionVuforia;						
					}
					eachRenderer.materials = materials;
				}
#else
				Renderer[] cullingRenderer = eachGameObject.GetComponentsInChildren<Renderer>();
				foreach (Renderer eachRenderer in cullingRenderer)
				{
					Material[] materials = eachRenderer.materials;
					for (int i = 0; i < eachRenderer.materials.Length; i++)
					{
						materials[i] = occlusionMaterial;
						materials[i].renderQueue = 1900;
					}

					eachRenderer.materials = materials;
				}
#endif												
			}
#endif
		}


		public void ChangeGameState(StatesGame newGameState)
		{
			if (!_isMultiplayer)
			{
				ChangeLocalGameState(newGameState);
			}
			else
			{
				switch (newGameState)
				{
					case StatesGame.Splash:
					case StatesGame.Download:
					case StatesGame.MainMenu:
					case StatesGame.Settings:
					case StatesGame.Floor:
					case StatesGame.Loading:
					case StatesGame.Connecting:
					case StatesGame.Network:
						ChangeLocalGameState(newGameState);
						break;

					default:
						ChangeRemoteGameState((int)newGameState);
						break;
				}
			}
		}

		private void ChangeLocalGameState(StatesGame newGameState)
		{
			if (_state == newGameState)
			{
				return;
			}
			if (_gameState != null)
			{
				_gameState.Destroy();
			}
			_gameState = null;
			_previousState = _state;
			_state = newGameState;
			switch (_state)
			{
				case StatesGame.Splash:
					_gameState = new GameStateSplash();
					break;

				case StatesGame.Download:
					_gameState = new GameStateDownload();
					break;

				case StatesGame.MainMenu:
					_gameState = new GameStateMenu();
					break;

				case StatesGame.Settings:
					_gameState = new GameStateSettings();
					break;

				case StatesGame.Floor:
					_gameState = new GameStateFloor();
					break;

				case StatesGame.Network:
					_gameState = new GameStateNetwork();
					break;

				case StatesGame.Connecting:
					_gameState = new GameStateConnecting();
					break;

				case StatesGame.Loading:
					_gameState = new GameStateLoad();
					break;

				case StatesGame.Run:
					_gameState = new GameStateRun();
					break;

				case StatesGame.Pause:
					_gameState = new GameStatePause();
					break;

				case StatesGame.ReleaseMemory:
					_gameState = new GameStateReleaseMemory();
					break;		
			}
			if (_gameState != null)
			{
				_gameState.Initialize();
			}					
		}

		void Update()
		{
			if (_gameState != null)
			{
				_gameState.Run();
			}
		}
	}
}
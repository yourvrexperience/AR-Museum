using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Narration;
using yourvrexperience.Networking;
using yourvrexperience.Utils;
using yourvrexperience.VR;
using static yourvrexperience.template6dof.LevelView;

namespace yourvrexperience.template6dof
{
	public class ScreenPauseView : BaseScreenView, IScreenView
	{
		[Serializable]
		public class MapData	
		{
			public GameObject Root;
			public Image Map;
			public float MapOrientationOffset;
			public Vector2 MapCenter;
			public EasterEggIcon[] EasterEggs;
		}

		public const string ScreenName = "ScreenPauseView";

		public const string EventScreenPauseViewResumeGame = "EventScreenPauseViewResumeGame";
		public const string EventScreenPauseViewExitGame = "EventScreenPauseViewExitGame";
		public const string EventScreenPauseViewTrackingRestored = "EventScreenPauseViewTrackingRestored";
		public const string EventScreenPauseDestroy = "EventScreenPauseDestroy";
		public const string EventScreenPauseViewManageNetwork = "EventScreenPauseViewManageNetwork";
		public const string EventScreenPauseViewManageRefreshPlayerList = "EventScreenPauseViewManageRefreshPlayerList";

		public const float WidthArea = 1140;
		public const float HeightArea = 1030;

		public const float WidthPlayer = 70;
		public const float HeightPlayer = 70;

		private enum StatePause { Normal, Worldmap, Synchronization, Management }

		[SerializeField] private GameObject containerVRControls;
		[SerializeField] private TextMeshProUGUI leftHandTitle;
		[SerializeField] private TextMeshProUGUI rightHandTitle;
		[SerializeField] private Button buttonLocomotionLeft;
		[SerializeField] private Button buttonLocomotionRight;
		[SerializeField] private TextMeshProUGUI leftHandInfo;
		[SerializeField] private TextMeshProUGUI rightHandInfo;

		private LocomotionMode _leftHand;
		private LocomotionMode _rightHand;

		[SerializeField] private MapData[] Maps;

		[SerializeField] private GameObject ContentTrackingLost;
		[SerializeField] private GameObject ContentGlobalMap;
		[SerializeField] private GameObject ContentManagement;

		[SerializeField] private Button buttonSaveData;
		[SerializeField] private Button buttonPublish;

		// CONTENT NORMAL
		[SerializeField] private Button buttonResume;

		[SerializeField] private GameObject playerPrefab;
		[SerializeField] private GameObject playerOtherPrefab;
		[SerializeField] private GameObject poiMapPrefab;
		[SerializeField] private GameObject secretMapPrefab;
		[SerializeField] private TextMeshProUGUI textNotVisited;
		[SerializeField] private TextMeshProUGUI textVisited;
		[SerializeField] private TextMeshProUGUI textDirection;
		[SerializeField] private TextMeshProUGUI textDiscovered;
		[SerializeField] private Button buttonWorldMap;
		[SerializeField] private TextMeshProUGUI worldMapTitle;
		[SerializeField] private GameObject informationPanel;
		[SerializeField] private GameObject panelPOIManagement;
		[SerializeField] private GameObject panelPOIListView;
		[SerializeField] private Button btnEditMode;

		// CONTENT SYNCHRONIZE
		[SerializeField] private Image okARDetected;
		[SerializeField] private TextMeshProUGUI titleScreen;

		// CONTENT WORLDMAP
		[SerializeField] private Button resumeFromWorldMap;

		[SerializeField] private Button buttonStairs1;
		[SerializeField] private Button buttonStairs2;
		[SerializeField] private Button buttonStairs3;

		[SerializeField] private TextMeshProUGUI textStairs1;
		[SerializeField] private TextMeshProUGUI textStairs2;
		[SerializeField] private TextMeshProUGUI textStairs3;

		// CONTENT MANAGEMENT
		[SerializeField] private GameObject cameraPlayer;
		[SerializeField] private Button resumeFromManagement;
		[SerializeField] private SlotManagerView slotManagerUsers;
		[SerializeField] private GameObject userMuseumItemPrefab;
		[SerializeField] private RawImage imageCameraPlayer;
		[SerializeField] private Button btnSetAerealCamera;
		[SerializeField] private Button btnKickOutUser;
		[SerializeField] private Button btnKillApp;
		[SerializeField] private TextMeshProUGUI textTitleCamera;
		[SerializeField] private GameObject iconUserCamera;
		[SerializeField] private GameObject iconAerealCamera;

		private StatePause _statePause = StatePause.Normal;

		private GameObject _player;
		private int _unlocked;
		private bool _trackingLostDuringPause = false;

		private bool _hasBeenPressedDown = false;

		private PlayerView _playerViewCamera;
		private Camera _cameraTexture;
		private Dictionary<PlayerView, GameObject> _avatarLogos = new Dictionary<PlayerView, GameObject>();
		private Image _map = null;
		private List<GameObject> _poisInMap = new List<GameObject>();
		private List<GameObject> _secretsInMap = new List<GameObject>();
		private float _pixelsPerMeter = 70f;
		private MapData _mapData;
		private Vector2 _sizeMap;
		private float _shiftHorizontal;
		private float _shiftVertical;

		public override string NameScreen
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			SystemEventController.Instance.Event += OnSystemEvent;
			UIEventController.Instance.Event += OnUIEvent;

			bool trackingLost = false;
#if UNITY_EDITOR || UNITY_WEBGL
			// trackingLost = true;
#elif !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
#if ENABLE_VUFORIA
			trackingLost = !VuforiaController.Instance.HasAreaBeenDetected;
#elif ENABLE_NIANTIC
			trackingLost = !NianticController.Instance.HasAreaBeenDetected;
#else
			trackingLost = !ARMaxSTController.Instance.HasAreaBeenDetected;
#endif			
#endif
			if (trackingLost)
			{
				_statePause = StatePause.Synchronization;
			}
			else
			{
				_statePause = StatePause.Normal;
			}

			buttonResume.onClick.AddListener(OnButtonResume);
			buttonWorldMap.onClick.AddListener(OnButtonWorldMap);			
			resumeFromWorldMap.onClick.AddListener(OnButtonResume);
			resumeFromManagement.onClick.AddListener(OnButtonResume);
			textNotVisited.text = LanguageController.Instance.GetText("screen.pause.map.not.visited");
			textVisited.text = LanguageController.Instance.GetText("screen.pause.map.visited");
			textDirection.text = LanguageController.Instance.GetText("screen.pause.map.direction");
			worldMapTitle.text =  LanguageController.Instance.GetText("screen.pause.world.map.title");

			titleScreen.text = LanguageController.Instance.GetText("screen.synchronization.hall.working");

			buttonStairs1.onClick.AddListener(OnButtonStairs1);
			buttonStairs2.onClick.AddListener(OnButtonStairs2);
			buttonStairs3.onClick.AddListener(OnButtonStairs3);
			buttonStairs1.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.pause.stairs.1");
			buttonStairs2.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.pause.stairs.2");
			buttonStairs3.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.pause.stairs.3");

			btnSetAerealCamera.onClick.AddListener(OnButtonSetAerealCamera);
			btnKickOutUser.onClick.AddListener(OnButtonKickOutUser);
			btnKillApp.onClick.AddListener(OnButtonKillApp);
			btnSetAerealCamera.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.pause.map.management.set.aereal.camera");
			btnKickOutUser.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.pause.map.management.kick.out.user");
			btnKillApp.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.pause.map.management.kill.app");

			foreach (MapData mapContent in Maps)
			{
				mapContent.Root.SetActive(false);
			}

			_mapData = Maps[GameLevelData.Instance.NextAreaGame];
			_mapData.Root.SetActive(true);
			_map = _mapData.Map;
			_sizeMap = _map.transform.GetComponent<RectTransform>().sizeDelta;
			_shiftHorizontal = (_sizeMap.x/10);
			_shiftVertical = (_sizeMap.y/10);
			if (_map != null)
			{
				_player = Instantiate(playerPrefab);
				_player.transform.parent = _map.transform;
				_player.transform.localPosition = Vector3.zero;
			}

			ShowConfiguration();

			if (!MainController.Instance.IsMultiplayer)
			{
				buttonWorldMap.gameObject.SetActive(true);
			}
			else
			{
				if (!NetworkController.Instance.IsServer)
				{
					buttonWorldMap.gameObject.SetActive(false);
				}
			}
			if (GameLevelData.Instance.TotalAreas == 1)			
			{
				buttonWorldMap.gameObject.SetActive(false);
			}
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR
			containerVRControls.SetActive(true);
			_leftHand = VRInputController.Instance.LocomotionLeftHand;
			_rightHand = VRInputController.Instance.LocomotionRightHand;
			buttonLocomotionLeft.onClick.AddListener(OnLocomotionLeft);
			buttonLocomotionRight.onClick.AddListener(OnLocomotionRight);
			leftHandInfo.text = _leftHand.ToString();
			rightHandInfo.text = _rightHand.ToString();
			leftHandTitle.text = LanguageController.Instance.GetText("screen.pause.left.hand.locomotion");
			rightHandTitle.text = LanguageController.Instance.GetText("screen.pause.right.hand.locomotion");
			
			RefocusScreen refocusComponent = this.gameObject.GetComponent<RefocusScreen>();
			if (refocusComponent == null)
			{
				refocusComponent = this.gameObject.AddComponent<RefocusScreen>();
			}
			refocusComponent.Activate(VRInputController.Instance.Camera, ScreenController.Instance.DistanceScreen, 1, 0.4f);
#elif ENABLE_NREAL
			containerVRControls.SetActive(false);
			RefocusScreen refocusComponent = this.gameObject.GetComponent<RefocusScreen>();
			if (refocusComponent == null)
			{
				refocusComponent = this.gameObject.AddComponent<RefocusScreen>();
			}
			refocusComponent.Activate(VRInputController.Instance.Camera, ScreenController.Instance.DistanceScreen, 1, 0.4f);
#else
			containerVRControls.SetActive(false);
#endif			
			if (MainController.Instance.EnableEditionPOIs)
			{
				containerVRControls.SetActive(false);
				informationPanel.SetActive(false);

				panelPOIManagement.SetActive(true);
				panelPOIListView.SetActive(true);
				btnEditMode.gameObject.SetActive(true);

				buttonSaveData.gameObject.SetActive(true);
				buttonSaveData.onClick.AddListener(OnSaveEditionPOIs);
				buttonSaveData.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.poi.select.edition.save");

				buttonPublish.gameObject.SetActive(true);
				buttonPublish.onClick.AddListener(OnPublish);
				buttonPublish.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.poi.select.edition.publish");

				btnEditMode.onClick.AddListener(OnToggleEditMode);

				RefreshEditMode();
			}
			else
			{
				panelPOIManagement.SetActive(false);
				panelPOIListView.SetActive(false);

				buttonSaveData.gameObject.SetActive(false);
				buttonPublish.gameObject.SetActive(false);
				btnEditMode.gameObject.SetActive(false);
			}

#if ENABLE_ONE_FLOOR
           buttonWorldMap.gameObject.SetActive(false);
#endif			
		}

        private void OnToggleEditMode()
        {
            GameLevelData.Instance.EditPOIsMode = !GameLevelData.Instance.EditPOIsMode;		
        }

		private void RefreshEditMode()
		{
			string textButton = "";
			if (GameLevelData.Instance.EditPOIsMode)
			{				
				textButton = LanguageController.Instance.GetText("screen.pause.edit.mode.pois");
			}
			else
			{
				textButton = LanguageController.Instance.GetText("screen.pause.edit.mode.secrets");
			}
			btnEditMode.GetComponentInChildren<TextMeshProUGUI>().text = textButton;
		}

        private void OnSaveEditionPOIs()
        {
			UIEventController.Instance.DispatchUIEvent(EventScreenPauseViewResumeGame);
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
			SystemEventController.Instance.DelaySystemEvent(GameSubStateEditPOI.EventGameStateRunSaveEditionPOIs, 0.1f);
        }

        private void OnPublish()
        {	
			UIEventController.Instance.DispatchUIEvent(EventScreenPauseViewResumeGame);
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
			SystemEventController.Instance.DelaySystemEvent(GameSubStateEditPOI.EventGameStateRunPublishEditionPOIs, 0.1f);
        }

		private void ClearPOIsInMap()
		{
			if (_poisInMap != null)
			{
				for (int i = 0; i < _poisInMap.Count; i++)
				{
					if (_poisInMap[i] != null)
					{
						GameObject.Destroy(_poisInMap[i]);
					}
				}
				_poisInMap.Clear();
			}
		}

		static Vector2 Flat(Vector3 p) => new Vector2(p.x, p.z);

		public Vector2 WorldToMap(float mapOrientationOffset, Vector2 mapCenter, Vector3 worldPos)
		{
			Vector3 worldCenter = MainController.Instance.LevelView.GetCenter.transform.position;
#if ENABLE_NIANTIC && !UNITY_EDITOR
			worldCenter = NavMeshController.Instance.ConvertARWorldToNavigation(worldCenter, false);			
#endif

			// 1. relative to the chosen center
			Vector2 center = Flat(worldCenter);
			Vector2 rel = Flat(worldPos) - center;

			// 2. rotate to the desired orientation
			float deg = mapOrientationOffset;
			float r = deg * Mathf.Deg2Rad;
			float c = Mathf.Cos(r), s = Mathf.Sin(r);
			Vector2 rot = new Vector2(rel.x * c - rel.y * s,
									rel.x * s + rel.y * c);

			// 3. scale world → pixels
			return mapCenter + rot * _pixelsPerMeter;
		}


		private void DisplayPOIs()
		{
			if (MainController.Instance.EnableEditionPOIs)
			{
				ClearPOIsInMap();
				ClearSecretsInMap();
			}
			else
			{
				ClearPOIsInMap();
			}			

			POIData[] poisLevel = MainController.Instance.LevelView.GetPOIS();
			if (poisLevel != null)
			{
				for (int k = 0; k < poisLevel.Length; k++)
				{
					POIData poiData = poisLevel[k];
#if ENABLE_NIANTIC && !UNITY_EDITOR					
					Vector3 positionPOI = poiData.Root.transform.position;
#else
					Vector3 positionPOI = poiData.Root.transform.localPosition;
#endif					
					GameObject poiMap = Instantiate(poiMapPrefab);
					poiMap.GetComponentInChildren<TextMeshProUGUI>().text = (k+1).ToString();
					_poisInMap.Add(poiMap);
					poiMap.transform.parent = _map.transform;
					poiMap.transform.localPosition = Vector3.zero;
					positionPOI = NavMeshController.Instance.ConvertARWorldToNavigation(positionPOI, false);
					Vector2 position2DPOI = WorldToMap(_mapData.MapOrientationOffset, new Vector2(_map.transform.localPosition.x + _shiftHorizontal, _map.transform.localPosition.y - _shiftVertical), positionPOI);
					poiMap.transform.localPosition = new Vector2(position2DPOI.x, position2DPOI.y);
#if UNITY_WEBGL					
					poiMap.transform.localScale = new Vector3(1f, 1f, 1f);
#else
					poiMap.transform.localScale = new Vector3(1f, 1f, 1f);
#endif
					if (!MainController.Instance.EnableEditionPOIs)
					{
#if UNLOCK_EVERYTHING
						poiMap.SetActive(true);
#else
						if (k > MainController.Instance.CurrentNarrationPOI)
						{
							poiMap.SetActive(false);
						}		
#endif										
					}
				}
			}
		}

		private void ClearSecretsInMap()
		{
			if (_secretsInMap != null)
			{
				for (int i = 0; i < _secretsInMap.Count; i++)
				{
					if (_secretsInMap[i] != null)
					{
						GameObject.Destroy(_secretsInMap[i]);
					}
				}
				_secretsInMap.Clear();
			}
		}

		private void DisplaySecrets()
		{
			if (MainController.Instance.EnableEditionPOIs)
			{
				ClearPOIsInMap();
				ClearSecretsInMap();
			}
			else
			{
				_unlocked = 0;
				ClearSecretsInMap();
			}	

			EasterEgg[] secretsLevel = MainController.Instance.LevelView.GetEasterEggs();
			if (secretsLevel != null)
			{
				for (int k = 0; k < secretsLevel.Length; k++)
				{
					EasterEgg secretData = secretsLevel[k];
#if ENABLE_NIANTIC && !UNITY_EDITOR	
					Vector3 positionSecret = secretData.Target.transform.position;
#else					
					Vector3 positionSecret = secretData.Target.transform.localPosition;
#endif					
					GameObject secretMap = Instantiate(secretMapPrefab);
					secretMap.GetComponentInChildren<TextMeshProUGUI>().text = (k+1).ToString();
					_secretsInMap.Add(secretMap);
					secretMap.transform.parent = _map.transform;
					secretMap.transform.localPosition = Vector3.zero;
					positionSecret = NavMeshController.Instance.ConvertARWorldToNavigation(positionSecret, false);
					Vector2 position2DPOI = WorldToMap(_mapData.MapOrientationOffset, new Vector2(_map.transform.localPosition.x + _shiftHorizontal, _map.transform.localPosition.y - _shiftVertical), positionSecret);
					secretMap.transform.localPosition = new Vector2(position2DPOI.x, position2DPOI.y);
#if UNITY_WEBGL					
					secretMap.transform.localScale = new Vector3(1f, 1f, 1f);
#else
					secretMap.transform.localScale = new Vector3(1f, 1f, 1f);
#endif					
					if (!MainController.Instance.EnableEditionPOIs)
					{
						secretMap.SetActive(secretData.Played);
						_unlocked++;
					}
				}
			}
		}

        private void ShowConfiguration()
		{
			switch (_statePause)
            {
				case StatePause.Normal:
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
					VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerEnableLocomotion, true);
#endif			
					_player.gameObject.SetActive(true);

					Content.gameObject.SetActive(true);
					ContentTrackingLost.SetActive(false);
					ContentGlobalMap.SetActive(false);
					ContentManagement.SetActive(false);
					_playerViewCamera = null;
					if (_cameraTexture != null)
					{
						GameObject.Destroy(_cameraTexture.gameObject);
						_cameraTexture = null;
					}
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerViewEnableBody, false);

					if (MainController.Instance.EnableEditionPOIs)
					{
						RefreshMapIcons();
					}
					else
					{
						DisplayPOIs();
						DisplaySecrets();
					}

					textDiscovered.text = LanguageController.Instance.GetText("screen.pause.map.discovered") + " (" + _unlocked + "/" + GameLevelData.Instance.LengthUnlockedEasterEggs(GameLevelData.Instance.NextAreaGame) + ")";

					CreateAvatarLogosNetworkPlayers();
					MainController.Instance.BlockCameraMovement = true;
					break;

				case StatePause.Synchronization:
					_player.gameObject.SetActive(false);

					Content.gameObject.SetActive(false);
					ContentTrackingLost.SetActive(true);
					ContentGlobalMap.SetActive(false);
					ContentManagement.SetActive(false);
					_playerViewCamera = null;
					if (_cameraTexture != null)
					{
						GameObject.Destroy(_cameraTexture.gameObject);
						_cameraTexture = null;
					}

					okARDetected.gameObject.SetActive(false);

#if UNITY_EDITOR
					SystemEventController.Instance.DelaySystemEvent(ARMaxSTController.EventARMaxSTControllerAreaRecognized, 2);
#endif
					break;

				case StatePause.Worldmap:
					Content.gameObject.SetActive(false);
					ContentTrackingLost.SetActive(false);
					ContentGlobalMap.SetActive(true);
					ContentManagement.SetActive(false);
					_playerViewCamera = null;
					if (_cameraTexture != null)
					{
						GameObject.Destroy(_cameraTexture.gameObject);
						_cameraTexture = null;
					}
					MainController.Instance.BlockCameraMovement = true;
					break;

				case StatePause.Management:
					Content.gameObject.SetActive(false);
					ContentTrackingLost.SetActive(false);
					ContentGlobalMap.SetActive(false);
					ContentManagement.SetActive(true);
					_playerViewCamera = null;
					if (_cameraTexture != null)
					{
						GameObject.Destroy(_cameraTexture.gameObject);
						_cameraTexture = null;
					}
					btnKickOutUser.interactable = false;

					imageCameraPlayer.gameObject.SetActive(true);
					if (_cameraTexture == null)
					{
						_cameraTexture = (Instantiate(cameraPlayer) as GameObject).GetComponent<Camera>();
					}
					_originalAerealCameraPosition = MainController.Instance.LevelView.AerealCamera.transform.position;
					_cameraTexture.transform.position = _originalAerealCameraPosition;
					imageCameraPlayer.texture = _cameraTexture.targetTexture;

					EnableTitleCameraSelected(true);

					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerViewEnableBody, true);				

					LoadListNetworkPlayers();
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR	|| ENABLE_NREAL			
					VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerEnableLocomotion, false);
#endif					
					MainController.Instance.BlockCameraMovement = true;
					break;
			}
		}

		public override void Destroy()
		{
			base.Destroy();

			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;

			if (slotManagerUsers != null)
			{
				slotManagerUsers.Destroy();
				slotManagerUsers = null;
			}
			_playerViewCamera = null;
			if (_cameraTexture != null)
			{
				GameObject.Destroy(_cameraTexture.gameObject);
				_cameraTexture = null;
			}			
			if (_trackingLostDuringPause)
			{
				SystemEventController.Instance.DelaySystemEvent(ARMaxSTController.EventARMaxSTControllerAreaLost, 0.2f);
			}
			SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerViewEnableBody, false);
			MainController.Instance.BlockCameraMovement = false;
		}

		private void RefreshMapIcons()
		{
			if (GameLevelData.Instance.EditPOIsMode)
			{
				DisplayPOIs();
			}
			else
			{
				DisplaySecrets();
			}
		}

		private void CreateAvatarLogosNetworkPlayers()
		{
			foreach (KeyValuePair<PlayerView, int> playerNetwork in MainController.Instance.PlayersNetwork)
			{
				PlayerView userPlayer = playerNetwork.Key;
				if (userPlayer != MainController.Instance.PlayerView)
				{
					if (!_avatarLogos.ContainsKey(userPlayer))
					{
						GameObject playerOther = Instantiate(playerOtherPrefab);
						playerOther.transform.parent = Maps[GameLevelData.Instance.NextAreaGame].Map.transform;
						playerOther.transform.localPosition = Vector3.zero;
						_avatarLogos.Add(userPlayer, playerOther);
					}
				}
			}			
		}

		private void RenderAvatarLogosNetworkPlayers()
		{
			foreach (KeyValuePair<PlayerView, GameObject> playerNetwork in _avatarLogos)
			{
				PlayerView userPlayer = playerNetwork.Key;
				GameObject logoPlayer = playerNetwork.Value;
				UpdatePosition(userPlayer, logoPlayer);
			}			
		}

		private void LoadListNetworkPlayers()
		{
			slotManagerUsers.ClearCurrentGameObject(true);
			List<ItemMultiObjectEntry> networkPlayers = new List<ItemMultiObjectEntry>();		
			int counter = 0;			
			foreach (KeyValuePair<PlayerView, int> playerNetwork in MainController.Instance.PlayersNetwork)
			{
				PlayerView userPlayer = playerNetwork.Key;
				if (userPlayer != null)
				{
					if (MainController.Instance.PlayerView != userPlayer)
					{
						counter++;
						ItemMultiObjectEntry data = new ItemMultiObjectEntry(LanguageController.Instance.GetText("user.museum") + " " + counter, userPlayer);
						networkPlayers.Add(new ItemMultiObjectEntry(this.gameObject, counter, data));
					}
				}
			}
			slotManagerUsers.Initialize(MainController.Instance.PlayersNetwork.Count, networkPlayers, userMuseumItemPrefab);
		}

		private void EnableTitleCameraSelected(bool isAereal)
		{
			if (isAereal)
			{
				iconUserCamera.SetActive(false);
				iconAerealCamera.SetActive(true);
				textTitleCamera.text = LanguageController.Instance.GetText("screen.pause.map.management.title.aereal.camera");
			}
			else
			{
				iconUserCamera.SetActive(true);
				iconAerealCamera.SetActive(false);
				textTitleCamera.text = LanguageController.Instance.GetText("screen.pause.map.management.title.user.camera");
			}
		}

		private void OnLocomotionRight()
		{
			_rightHand++;
			if ((int)_rightHand > 3) _rightHand = 0;
			rightHandInfo.text = _rightHand.ToString();
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR	|| ENABLE_NREAL			
			VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerChangeLocomotion, true, _rightHand);
#endif			
		}

		private void OnLocomotionLeft()
		{
			_leftHand++;
			if ((int)_leftHand > 3) _leftHand = 0;
			leftHandInfo.text = _leftHand.ToString();
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR	|| ENABLE_NREAL			
			VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerChangeLocomotion, false, _leftHand);
#endif			
		}

		private void UpdatePosition(PlayerView target, GameObject logo)
		{
			if (MainController.Instance.LevelView == null) return;

			Vector3 posPlayer =  Vector3.zero;
#if ENABLE_NIANTIC && !UNITY_EDITOR
			posPlayer = NavMeshController.Instance.ConvertARWorldToNavigation(target.transform.position, false);
#else
			posPlayer = NavMeshController.Instance.ConvertARWorldToNavigation(target.transform.localPosition, false);
#endif			
			Vector2 position2DPOI = WorldToMap(_mapData.MapOrientationOffset, new Vector2(_map.transform.localPosition.x + _shiftHorizontal, _map.transform.localPosition.y - _shiftVertical), posPlayer);
			Vector3 finalPosition = new Vector2(position2DPOI.x, position2DPOI.y);
			if (Mathf.Infinity != Mathf.Abs(finalPosition.x) && (Mathf.Infinity != Mathf.Abs(finalPosition.y)))
			{
				logo.transform.localPosition = finalPosition;
			}
		}

		private Vector3 MapToWorld(Vector2 mapPosition)
		{
			float xPlayerInMap = -mapPosition.x - (WidthArea / 2) + (WidthPlayer / 2) + WidthArea;
			float yPlayerInMap = -mapPosition.y - (HeightArea / 2) + (HeightPlayer / 2) + HeightArea;

			float posPlayerZ = ((xPlayerInMap/WidthArea) * MainController.Instance.LevelView.Area.height) + MainController.Instance.LevelView.Area.y;
			float posPlayerX = (MainController.Instance.LevelView.Area.width - ((yPlayerInMap/(HeightArea - HeightPlayer)) * MainController.Instance.LevelView.Area.width)) + MainController.Instance.LevelView.Area.x;

			return new Vector3(posPlayerX, MainController.Instance.PlayerView.transform.position.y / 2, posPlayerZ);
		}

        private void OnButtonResume()
        {			
			switch (_statePause)
			{
				case StatePause.Normal:
					UIEventController.Instance.DispatchUIEvent(EventScreenPauseViewResumeGame);
					UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
					break;

				case StatePause.Synchronization:
					break;

				case StatePause.Worldmap:
					_statePause = StatePause.Normal;
					ShowConfiguration();
					break;

				case StatePause.Management:				
					_statePause = StatePause.Normal;
					ShowConfiguration();
					break;
			}
        }

        private void OnButtonWorldMap()
        {
#if !ENABLE_ONE_FLOOR
            _statePause = StatePause.Worldmap;
			ShowConfiguration();
#endif			
        }

        private void OnButtonStairs1()
        {
			if (MainController.Instance.CurrentGameLevel != 0)
			{
				if (!MainController.Instance.IsMultiplayer)
				{
					SystemEventController.Instance.DispatchSystemEvent(MainController.EventMainControllerChangeCurrentLevel, 0);
				}
				else
				{
					if (NetworkController.Instance.IsServer)
					{
						NetworkController.Instance.DelayNetworkEvent(MainController.EventMainControllerChangeCurrentLevel, 0.01f, -1, -1, 0);
					}
				}
			}            
        }

        private void OnButtonStairs2()
        {
			if (GameLevelData.Instance.NextAreaGame != 1)
			{
				if (!MainController.Instance.IsMultiplayer)
				{
					SystemEventController.Instance.DispatchSystemEvent(MainController.EventMainControllerChangeCurrentLevel, 1);
				}
				else
				{
					if (NetworkController.Instance.IsServer)
					{
						NetworkController.Instance.DelayNetworkEvent(MainController.EventMainControllerChangeCurrentLevel, 0.01f, -1, -1, 1);
					}
				}
			}            
        }

        private void OnButtonStairs3()
        {
			if (GameLevelData.Instance.NextAreaGame != 2)
			{
				if (!MainController.Instance.IsMultiplayer)
				{
					SystemEventController.Instance.DispatchSystemEvent(MainController.EventMainControllerChangeCurrentLevel, 2);
				}
				else
				{
					if (NetworkController.Instance.IsServer)
					{
						NetworkController.Instance.DelayNetworkEvent(MainController.EventMainControllerChangeCurrentLevel, 0.01f, -1, -1, 2);
					}
				}
			}            
        }

        private void OnButtonKillApp()
        {
            NetworkController.Instance.DelayNetworkEvent(GameStateRun.EventGameStateKickOutUserFromApp, 0.1f, -1, -1, -1);
			UIEventController.Instance.DispatchUIEvent(EventScreenPauseViewResumeGame);
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
        }

        private void OnButtonKickOutUser()
        {
			if (_playerViewCamera != null)
			{				
				btnKickOutUser.interactable = false;
				NetworkController.Instance.DelayNetworkEvent(GameStateRun.EventGameStateKickOutUserFromApp, 0.1f, -1, -1, _playerViewCamera.NetworkGameIDView.GetViewID());
				OnButtonSetAerealCamera();
			}
        }

        private void OnButtonSetAerealCamera()
        {
			_playerViewCamera = null;
			btnKickOutUser.interactable = false;			
            UIEventController.Instance.DispatchUIEvent(ItemUserMuseum.EventItemUserMuseumReset);			
			EnableTitleCameraSelected(true);		
			_cameraTexture.transform.position = _originalAerealCameraPosition;	
        }

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(GameLevelData.EventGameLevelDataEditModeChanged))
			{
#if !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)       				
				RefreshEditMode();
				RefreshMapIcons();
#else
				OnButtonResume();
				UIEventController.Instance.DelayUIEvent(GameStateRun.EventGameStateRunTriggerPause, 0.4f);
#endif				
			}
			if (nameEvent.Equals(GameLevelData.EventGameLevelDataRefreshPOILevel))
			{
#if !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
				RefreshMapIcons();
#else
				OnButtonResume();
				UIEventController.Instance.DelayUIEvent(GameStateRun.EventGameStateRunTriggerPause, 0.4f);
#endif				
			}
			if (nameEvent.Equals(EventScreenPauseViewManageRefreshPlayerList))
			{				
				LoadListNetworkPlayers();
			}
			if (nameEvent.Equals(ScreenPauseView.EventScreenPauseViewManageNetwork))
			{
				_statePause = StatePause.Management;
				ShowConfiguration();
			}
			if (nameEvent.Equals(ARMaxSTController.EventARMaxSTControllerAreaRecognized))
			{
				_trackingLostDuringPause = false;
				okARDetected.gameObject.SetActive(true);
				titleScreen.text = LanguageController.Instance.GetText("screen.synchronization.hall.completed");					
				SystemEventController.Instance.DelaySystemEvent(EventScreenPauseViewTrackingRestored, 2);
			}
			if (nameEvent.Equals(EventScreenPauseViewTrackingRestored))
			{
				_statePause = StatePause.Normal;
				OnButtonResume();
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerUpdateTexts))
			{
				textNotVisited.text = LanguageController.Instance.GetText("screen.pause.map.not.visited");
				textVisited.text = LanguageController.Instance.GetText("screen.pause.map.visited");
				textDirection.text = LanguageController.Instance.GetText("screen.pause.map.direction");
				textDiscovered.text = LanguageController.Instance.GetText("screen.pause.map.discovered") + " (" + _unlocked + "/" + GameLevelData.Instance.LengthUnlockedEasterEggs(GameLevelData.Instance.NextAreaGame) + ")";
			}
			if (nameEvent.Equals(EventScreenPauseDestroy))
			{				
				OnButtonResume();
			}
			if (nameEvent.Equals(ARMaxSTController.EventARMaxSTControllerAreaLost))
			{
				_trackingLostDuringPause = true;
			}
		}

        private void OnUIEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(ItemUserMuseum.EventItemUserMuseumSelected))
			{
				int indexSelected = (int)parameters[2];
				if (indexSelected != -1)
				{
					_playerViewCamera = (PlayerView)parameters[3];
					btnKickOutUser.interactable = true;
					EnableTitleCameraSelected(false);
				}
				else
				{
					_playerViewCamera = null;
					btnKickOutUser.interactable = false;
					_cameraTexture.transform.position = _originalAerealCameraPosition;
					EnableTitleCameraSelected(true);
				}
			}
        }

		private void LogicSelectTargetToMove()
		{
			if (Input.GetMouseButtonDown(0))
			{
				_hasBeenPressedDown = true;
			}
			if (_hasBeenPressedDown)
			{
				if (Input.GetMouseButtonUp(0))
				{
					_hasBeenPressedDown = false;
				}
			}
		}

		private Vector3 _originalAerealCameraPosition;
		private Vector3 _anchorStartPosition;
		private Vector3 _anchorCurrentPosition;
		private Vector3 _anchorLastPosition = Vector3.zero;

		private void MoveAerealCamera()
		{
#if ENABLE_NREAL			
			return;
#endif
			bool hasDownBeenPressed = false;
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
			hasDownBeenPressed = VRInputController.Instance.VRController.GetVector2Joystick(XR_HAND.both).magnitude > 0;	
#else
			hasDownBeenPressed = Input.GetMouseButtonDown(0);
#endif
			if (hasDownBeenPressed)
			{
				_hasBeenPressedDown = true;				
				
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)				
				_anchorStartPosition = Vector3.zero;
				_anchorCurrentPosition = Vector3.zero;
				_anchorLastPosition = Vector3.zero;
#else
				_anchorStartPosition = Input.mousePosition;
#endif
			}
			if (_hasBeenPressedDown)
			{
				bool hasDownBeenReleased = false;
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)							
				float speedMovement = 200f;		
				Vector2 axisValues = VRInputController.Instance.VRController.GetVector2Joystick(XR_HAND.both);
				if (axisValues.x > 0.5f) _anchorCurrentPosition.x -= Time.deltaTime * speedMovement;
				if (axisValues.x < -0.5f) _anchorCurrentPosition.x += Time.deltaTime * speedMovement;
				if (axisValues.y > 0.5f) _anchorCurrentPosition.y -= Time.deltaTime * speedMovement;
				if (axisValues.y < -0.5f) _anchorCurrentPosition.y += Time.deltaTime * speedMovement;
				_anchorCurrentPosition.z = 0;
				hasDownBeenReleased = VRInputController.Instance.VRController.GetVector2Joystick(XR_HAND.both).magnitude == 0;	
#else
				hasDownBeenReleased = Input.GetMouseButtonUp(0);
				_anchorCurrentPosition = Input.mousePosition;
#endif
				if (hasDownBeenReleased)
				{
					_hasBeenPressedDown = false;
					if (!_anchorLastPosition.Equals(Vector3.zero)) _originalAerealCameraPosition =	_anchorLastPosition;					
				}
				else
				{
					Vector3 distanceMovement = ( _anchorCurrentPosition - _anchorStartPosition ) / 100f;
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)							
					_cameraTexture.transform.position = _originalAerealCameraPosition - new Vector3(distanceMovement.x, 0, distanceMovement.y);
#elif UNITY_EDITOR
					_cameraTexture.transform.position = _originalAerealCameraPosition - new Vector3(distanceMovement.x, 0, distanceMovement.y);
#else
					_cameraTexture.transform.position = _originalAerealCameraPosition - new Vector3(distanceMovement.x, distanceMovement.y, 0);
#endif					
					_anchorLastPosition = _cameraTexture.transform.position;
				}
			}
		}

		void Update()
		{
			if (_player != null)
			{
				UpdatePosition(MainController.Instance.PlayerView, _player);
				RenderAvatarLogosNetworkPlayers();

				if (_statePause == StatePause.Management)
				{
					if (_playerViewCamera != null)
					{
						_cameraTexture.transform.position = _playerViewCamera.transform.position;
						_cameraTexture.transform.forward = _playerViewCamera.transform.forward;
					}
					else
					{
						MoveAerealCamera();

#if UNITY_EDITOR || ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || UNITY_WEBGL
						_cameraTexture.transform.forward = Vector3.down;
#else						
						_cameraTexture.transform.forward = Vector3.down;
#endif						
					}
				}
				else
				{
					LogicSelectTargetToMove();
				}
			}
		}
	}
}
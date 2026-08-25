using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.Utils
{
	public class ScreenControllerApp : MonoBehaviour
	{
		public const string EventScreenControllerBasicMainMenu = "EventScreenControllerBasicMainMenu";
		public const string EventScreenControllerBasicLoadSession = "EventScreenControllerBasicLoadSession";
		public const string EventScreenControllerBasicProfile = "EventScreenControllerBasicProfile";
		public const string EventScreenControllerBasicSettings = "EventScreenControllerBasicSettings";
		public const string EventScreenControllerBasicLoadCompleted = "EventScreenControllerBasicLoadCompleted";
		public const string EventScreenControllerBasicExit = "EventScreenControllerBasicExit";
		public const string EventScreenControllerBasicBuildInLevel = "EventScreenControllerBasicBuildInLevel";
		public const string EventScreenControllerBasicResumeInLevel = "EventScreenControllerBasicResumeInLevel";
		public const string EventScreenControllerBasicExitInLevel = "EventScreenControllerBasicExitInLevel";
		public const string EventScreenControllerBasicRestorePlayerMovement = "EventScreenControllerBasicRestorePlayerMovement";
		public const string EventScreenControllerBasicRequestInteractionMouse = "EventScreenControllerBasicRequestInteractionMouse";

		public enum StatesApp { MainMenu = 0, Profile, Settings, Loading, InLevel, InBuild, InPause, Exiting }

        private static ScreenControllerApp _instance;

        public static ScreenControllerApp Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(ScreenControllerApp)) as ScreenControllerApp;
                }
                return _instance;
            }
        }

		[SerializeField] private GameObject PlayerDesktopPrefab;
		[SerializeField] private GameObject LevelPrefab;
		private StatesApp _state;
		private StatesApp _lastState;
		private IInputController _inputController;

		private PlayerDesktop _playerDesktop;
		private GameObject _level;
		private List<StatesGameObject> _stateObjects = new List<StatesGameObject>();
		private int _layerFloor;

		private int _iterator = 0;
		private int _timer = 0;

		public GameObject Level
		{
			get { return _level; }
		}
		public int LayerFloor
		{
			get { return _layerFloor; }
		}
		
		void Start()
		{
			_layerFloor = LayerMask.GetMask("Floor");
			UIEventController.Instance.Event += OnUIEvent;
			SystemEventController.Instance.Event += OnSystemEvent;
		}

		void OnDestroy()
		{
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		private void InteractWithStatesGameObject(GameObject target)
		{
			foreach (StatesGameObject item in _stateObjects)
			{
				if (item.CheckInObject(target))
				{
					item.State = (item.State + 1) % item.StatesLength();
				}
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(InputController.EventInputControllerHasStarted))
			{
				_inputController = ((GameObject)parameters[0]).GetComponent<IInputController>();
				_inputController.Initialize();		
				ChangeState(StatesApp.MainMenu);
			}
			if (nameEvent.Equals(EventScreenControllerBasicLoadCompleted))
			{
				ScreenController.Instance.DestroyScreens();
				ChangeState(StatesApp.InLevel);
				string information = LanguageController.Instance.GetText("text.info");
				string welcomeDescription = LanguageController.Instance.GetText("in.level.welcome.instructions");
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, information, welcomeDescription, EventScreenControllerBasicRestorePlayerMovement);
				SystemEventController.Instance.DelaySystemEvent(PlayerDesktop.EventPlayerAvatarEnableMovement, 0.1f, false);
			}
			if (nameEvent.Equals(EventScreenControllerBasicRestorePlayerMovement))
			{
				SystemEventController.Instance.DispatchSystemEvent(PlayerDesktop.EventPlayerAvatarEnableMovement, true);
			}
			if (nameEvent.Equals(StatesGameObject.EventStatesGameObjectStarted))
			{
				_stateObjects.Add((StatesGameObject)parameters[0]);
			}
			if (nameEvent.Equals(ScreenBuilder.EventScreenBuilderBuildObject))
			{
				ChangeState(StatesApp.InLevel);
			}
			if (nameEvent.Equals(EventScreenControllerBasicRequestInteractionMouse))
			{
				RaycastHit hitObjectData = new RaycastHit();
				GameObject hitObject = RaycastingTools.GetMouseCollisionObject(Camera.main, ref hitObjectData, Physics.DefaultRaycastLayers);
				if (hitObject != null)
				{
					InteractWithStatesGameObject(hitObject);
				}
			}
		}

		private void OnUIEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(EventScreenControllerBasicMainMenu))
			{
				ChangeState(StatesApp.MainMenu);
			}
			if (nameEvent.Equals(EventScreenControllerBasicLoadSession))
			{
				ChangeState(StatesApp.Loading);
			}
			if (nameEvent.Equals(EventScreenControllerBasicProfile))
			{
				ChangeState(StatesApp.Profile);
			}
			if (nameEvent.Equals(EventScreenControllerBasicSettings))
			{
				ChangeState(StatesApp.Settings);
			}						
			if (nameEvent.Equals(EventScreenControllerBasicExit))
			{
				string titleWarning = LanguageController.Instance.GetText("text.warning");
				string textAskToExit = LanguageController.Instance.GetText("screen.main.do.you.want.to.exit");
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenConfirmation, this.gameObject, titleWarning, textAskToExit);				
			}
			if (nameEvent.Equals(ScreenInformationView.EventScreenInformationResponse))
			{
				if (this.gameObject == (GameObject)parameters[0])
				{
					ScreenInformationResponses userResponse = (ScreenInformationResponses)parameters[1];
					if (userResponse == ScreenInformationResponses.Confirm)
					{
						string titleInfo = LanguageController.Instance.GetText("text.info");
						string textNowExiting = LanguageController.Instance.GetText("screen.main.now.exiting");
						ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, this.gameObject, titleInfo, textNowExiting);
						Application.Quit();
					}
				}
			}
			if (nameEvent.Equals(EventScreenControllerBasicResumeInLevel))
			{
				ScreenController.Instance.DestroyScreens();
				ChangeState(StatesApp.InLevel);
			}
			if (nameEvent.Equals(EventScreenControllerBasicExitInLevel))
			{
				ScreenController.Instance.DestroyScreens();
				ChangeState(StatesApp.Exiting);
			}
			if (nameEvent.Equals(EventScreenControllerBasicBuildInLevel))
			{
				ScreenController.Instance.DestroyScreens();
				ChangeState(StatesApp.InBuild);
			}
		}

		private void ChangeState(StatesApp state)
		{
			_lastState = _state;
			_state = state;
			_iterator = 0;
			_timer = 0;
			switch (_state)
			{
				case StatesApp.MainMenu:
					Camera.main.transform.position = Vector3.zero;
					Camera.main.transform.forward = Vector3.zero;
					ScreenController.Instance.CreateScreen(ScreenMainMenu.ScreenName, false, true);
					break;
				case StatesApp.Profile:
					ScreenController.Instance.CreateScreen(ScreenProfile.ScreenName, false, true);
					break;
				case StatesApp.Settings:
					ScreenController.Instance.CreateScreen(ScreenSettings.ScreenName, false, true);
					break;
				case StatesApp.Loading:
					ScreenController.Instance.CreateScreen(ScreenLoadBundle.ScreenName, false, true);
					break;
				case StatesApp.InLevel:
					SystemEventController.Instance.DispatchSystemEvent(PlayerDesktop.EventPlayerAvatarEnableMovement, true);
					if (_playerDesktop == null)
					{
						_playerDesktop = (Instantiate(PlayerDesktopPrefab) as GameObject).GetComponent<PlayerDesktop>();
						_playerDesktop.gameObject.transform.position = Vector3.zero;
					}
					if (_level == null)
					{
						_level = Instantiate(LevelPrefab) as GameObject;
					}
					break;
				case StatesApp.InPause:
					SystemEventController.Instance.DispatchSystemEvent(PlayerDesktop.EventPlayerAvatarEnableMovement, false);
					ScreenController.Instance.CreateScreen(ScreenPause.ScreenName, true, false);
					break;
				case StatesApp.InBuild:
					SystemEventController.Instance.DispatchSystemEvent(PlayerDesktop.EventPlayerAvatarEnableMovement, false);
					ScreenController.Instance.CreateScreen(ScreenBuilder.ScreenName, true, false);
					break;
				case StatesApp.Exiting:
					ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, "Now exiting...", "");
					break;
			}
		}

		void Update()
		{
			if (_iterator < 100) _iterator++;

			switch (_state)
			{
				case StatesApp.InLevel:
					if (Input.GetKeyDown(KeyCode.P))
					{
						ChangeState(StatesApp.InPause);
					}
					break;

				case StatesApp.InPause:
					break;

				case StatesApp.Exiting:
					switch (_iterator)
					{
						case 1:
							if (_playerDesktop != null)
							{
								GameObject.Destroy(_playerDesktop.gameObject);
								_playerDesktop = null;
							}
							if (_level != null)
							{
								GameObject.Destroy(_level);
								_level = null;
							}
							_stateObjects.Clear();
							break;

						case 2:
							ChangeState(StatesApp.MainMenu);
							break;
					}				
					break;
			}
		}
	}
}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using yourvrexperience.ai;
using yourvrexperience.Narration;
using yourvrexperience.Networking;
using yourvrexperience.Utils;
using static yourvrexperience.ai.NewConversationChatGPTHTTP;
using static yourvrexperience.template6dof.LevelView;
using static yourvrexperience.Narration.GameLevelData;
using static yourvrexperience.Narration.NarrationController;
using yourvrexperience.VR;
using yourvrexperience.UserManagement;
using yourvrexperience.speech;

namespace yourvrexperience.template6dof
{
	public class GameStateRun : IGameState
    {
		public const string EventGameStateRunTriggerNextButton = "EventGameStateRunTriggerNextButton";
		public const string EventGameStateRunEndCurrentNarration = "EventGameStateRunEndCurrentNarration";		
		public const string EventGameStateRunTriggerPause = "EventGameStateRunTriggerPause";
		public const string EventGameStateRunAIInteraction = "EventGameStateRunAIInteraction";		
		public const string EventGameStateRunRestartExperience = "EventGameStateRunRestartExperience";
		public const string EventGameStateRunSkipLastNarration = "EventGameStateRunSkipLastNarration";
		public const string EventGameStateRunNavigateToCurrentPOI = "EventGameStateRunNavigateToCurrentPOI";

		public const string EventGameStateRunNetworkStartNarration = "EventGameStateRunNetworkStartNarration";
		public const string EventGameStateRunNetworkChangeSubState = "EventGameStateRunNetworkChangeSubState";
		public const string EventGameStateRunNetworkRequestSubState = "EventGameStateRunNetworkRequestSubState";
		public const string EventGameStateRunNetworkResponseSubState = "EventGameStateRunNetworkResponseSubState";

		public const string EventGameStateKickOutUserFromApp = "EventGameStateKickOutUserFromApp";

		public const string EventGameStateRunShowResumeButtonInEasterEgg = "EventGameStateRunShowResumeButtonInEasterEgg";	
		public const string EventGameStateRunShowDownloadAudioTracksPOI = "EventGameStateRunShowDownloadAudioTracksPOI";	
		public const string SubEventGameStateRunConfirmationExit = "SubEventGameStateRunConfirmationExit";	

		public const float SizeInfoScreen = 0.005f;
		public const float SizeEasterEggScreen = 0.001f;		
		public const float SizeEasterEggNarration = 0.0005f;		

		public enum ConfigurationEasterEggScreen { None = 0, Video, Photos, Discover, Narration }

		private GameLevelStates _gameLevelState = GameLevelStates.Initialization;
		private GameLevelStates _previousStateToPause = GameLevelStates.Initialization;
		private GameLevelSubStates _gameLevelSubState = GameLevelSubStates.Null;
        private float _timerLevel = 0;
		private int _presentationCounter = 3;
		private int _currentPOIIndex = 0;
		private POIData _currentPOI = null;
		private int _totalTimeEasterEgg = -1;		
		private ConfigurationEasterEggScreen _configurationEasterEgg = ConfigurationEasterEggScreen.None;
		private EasterEgg _currentEggFound = null;
		private bool _enableDetectionEasterEggs = false;
		private bool _allowTriggerNextPOI = true;
		private POIData _replayPOI = null;

		private List<QuestionForm> _questions = new List<QuestionForm>();

		private GameSubStateEditPOI _subStateEditPOI;

		private POIData CurrentPOI
        {
			set
            {
				_currentPOI = value;
				MainController.Instance.LevelView.CurrentPOI = _currentPOI;
			}
        }

		public void Initialize()
		{
			SystemEventController.Instance.Event += OnSystemEvent;
			UIEventController.Instance.Event += OnUIEvent;
			NetworkController.Instance.NetworkEvent += OnNetworkEvent;

			_timerLevel = 0;
			_gameLevelSubState = GameLevelSubStates.Null;
			ChangeGameLevelState(GameLevelStates.Initialization);

			Assert.IsNotNull(MainController.Instance.PlayerView, "The player is null");
			Assert.IsNotNull(MainController.Instance.LevelView, "The level is null");

			GameLevelData.Instance.CurrentLevel = GameLevelData.Instance.GetLevel(GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel);
			GameLevelData.Instance.TotalTimeDone = 0;
			MainController.Instance.CompletedArea = false;
						
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
			ScreenController.Instance.ApplyOverlay = true;
#endif			
		}

        public void Destroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;			

			_currentPOI = null;
			_replayPOI = null;
			if (_subStateEditPOI != null)
			{
				_subStateEditPOI.Destroy();
			}			
			Cursor.lockState = CursorLockMode.None;
			GameLevelData.Instance.SaveGameLevelState(_gameLevelState, _timerLevel);
			if (SoundsController.Instance.CurrentAudioMelodyPlaying == GameSounds.MelodyInGameLevel)
			{
				SoundsController.Instance.PauseSoundBackground();
			}			
			MainController.Instance.HighlightedPOI?.SetActive(false);
			MainController.Instance.SelectedPOI?.SetActive(false);
		}

        private void PlayerShoot()
		{
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
			Vector3 positionCurrentController = Vector3.zero;
			Vector3 forwardCurrentController = Vector3.zero;
			if (VRInputController.Instance.VRController.CurrentController != null)
			{
				positionCurrentController = VRInputController.Instance.VRController.CurrentController.transform.position;
				forwardCurrentController = VRInputController.Instance.VRController.CurrentController.transform.forward;
				positionCurrentController += VRInputController.Instance.VRController.CurrentController.transform.forward;
			}
#else
			Vector3 positionCurrentController = Camera.main.transform.position;
			Vector3 forwardCurrentController = Camera.main.transform.forward;
			positionCurrentController += Camera.main.transform.forward;
#endif
			
			BulletsController.Instance.ShootBullet(positionCurrentController, forwardCurrentController, 10);
			SoundsController.Instance.PlaySoundFX(SoundsController.ChannelsAudio.FX2, GameSounds.FxShoot, false, 1);
		}
		
        private bool HasBeenPlayed(string eventName)
        {
			int hasBeenPlayed = PlayerPrefs.GetInt(eventName, -1);
			if (hasBeenPlayed == -1)
            {
				PlayerPrefs.SetInt(eventName, 1);
			}			
			return (hasBeenPlayed == 1);
		}

		private bool CheckAllowInteractionOutsideNarration()
		{
			if (!_enableDetectionEasterEggs) return false;
			if (!_allowTriggerNextPOI) return false;
			if (MainController.Instance.IsMultiplayer)
			{
				if (!GameLevelData.Instance.EnablePauseAccess) return false;
			}
			return true;
		}

		private void CheckInteractionWithEasterEgg()
		{
			if (CheckAllowInteractionOutsideNarration())
			{
				// NEAR EGG
				EasterEgg eggFound = null;
				float minDistance = 1000000;
				if (MainController.Instance.LevelView.EasterEggs != null)
				{
					foreach (EasterEgg egg in MainController.Instance.LevelView.EasterEggs)
					{
						if (egg.Target != null)
						{
							float distanceToEgg = Vector3.Distance(MainController.Instance.GameInputController.Camera.transform.position, egg.Target.transform.position);
							if (distanceToEgg < 2.2f)
							{
								if (yourvrexperience.Utils.Utilities.IsVisibleFrom(egg.Target.GetComponent<Collider>().bounds, MainController.Instance.GameInputController.Camera))
								{
									if (distanceToEgg < minDistance)
									{
										minDistance = distanceToEgg;
										eggFound = egg;
									}
								}
							}					
						}
					}
				}

				// NEAR GUIDE
				float distanceToTourGuide = Vector3.Distance(MainController.Instance.GameInputController.Camera.transform.position, MainController.Instance.GuideTourView.gameObject.transform.position);
				if (distanceToTourGuide < 3)
				{
					if (yourvrexperience.Utils.Utilities.IsVisibleFrom(MainController.Instance.GuideTourView.gameObject.GetComponent<Collider>().bounds, MainController.Instance.GameInputController.Camera))
					{
						eggFound = null;
					}
				}

				if (_currentEggFound != null)
				{
					if (_currentEggFound != eggFound)
					{
						SystemEventController.Instance.DispatchSystemEvent(ScreenFoundEasterEggView.EventScreenFoundEasterEggViewDestroy);
						_currentEggFound.SetActive(false);
					}
				}
				_currentEggFound = eggFound;

				// CHECK INTERACTION WITH EGG
				if (_currentEggFound != null)
				{
					if (_currentEggFound.Enabled)
					{
						int easterEggIndex = _currentEggFound.Index;
						if (!_currentEggFound.Active)
						{
							if (GameObject.FindAnyObjectByType<ScreenFoundEasterEggView>() == null)
							{
								string title = LanguageController.Instance.GetText(_currentEggFound.GetTitle());
								string description = LanguageController.Instance.GetText(_currentEggFound.GetDescription());								
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
								Vector3 normalToTarget = (_currentEggFound.Star.transform.position - MainController.Instance.PlayerView.transform.position).normalized;
								Vector3 posScreen = _currentEggFound.Star.transform.position;
								ScreenController.Instance.CreateScreen3DAnchor(ScreenFoundEasterEggView.ScreenName, _currentEggFound.Star, posScreen, normalToTarget, SizeEasterEggScreen, false, true, title, description, _currentEggFound);
#else
								ScreenController.Instance.CreateScreen(ScreenFoundEasterEggView.ScreenName, false, true, title, description, _currentEggFound);
#endif								
							}
						}
						if (!_currentEggFound.Appeared && !GameLevelData.Instance.GetUnlockedEasterEgg(GameLevelData.Instance.NextAreaGame, easterEggIndex))
						{
							_currentEggFound.Appeared = true;						
							GameLevelData.Instance.SetUnlockEasterEgg(GameLevelData.Instance.NextAreaGame, easterEggIndex);
						}
						else
						{
							_currentEggFound.SetActive(true);
						}
					}
				}
			}
		}

		private void RunActionWhenPlayerCloseBy()
		{
			if (_currentPOI != null)
			{
				if ((_currentPOI.EventStart != null) && (_currentPOI.EventStart.Length > 0))
				{
					SystemEventController.Instance.DispatchSystemEvent(_currentPOI.EventStart, _currentPOI.ExtraData);
				}
				if (_gameLevelState == GameLevelStates.InGame)
				{
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventDestroyArrowPath);
					SystemEventController.Instance.DelaySystemEvent(DestinationMarker.EventDestinationMarkerDestroy, 0.1f);
					SystemEventController.Instance.DelaySystemEvent(NavigationLineView.EventNavigationLineViewDestroy, 0.15f);
					if (MainController.Instance.IsMultiplayer)
					{
						if (NetworkController.Instance.IsServer)
						{
							NetworkController.Instance.DelayNetworkEvent(LevelView.EventLevelViewUnlockReplayPOI, 0.01f, -1, -1, _currentPOIIndex - 1);
						}						
					}
					else
					{
						SystemEventController.Instance.DispatchSystemEvent(LevelView.EventLevelViewUnlockReplayPOI, _currentPOIIndex - 1);
					}					
					ChangeGameLevelSubState(GameLevelSubStates.PlayAudio);
				}
			}
		}

		private void PresentationScreenForNarration()
		{
			string descriptionStart = "screen.next.button.press.to.start.narration";
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
			Vector3 forwardGuide = -MainController.Instance.GuideTourView.GetModel().transform.forward;
			GameObject contentVRScreen = MainController.Instance.GuideTourView.ScreenVR;
			Vector3 posScreen = contentVRScreen.transform.position;
			ScreenController.Instance.CreateScreen3DAnchor(ScreenInfoNextButtonView.ScreenName, MainController.Instance.GuideTourView.ScreenVR, posScreen, forwardGuide, SizeInfoScreen, true, false, MainController.Instance.IsMultiplayer, GameLevelData.Instance.EnablePauseAccess, descriptionStart);
#else
			ScreenController.Instance.CreateScreen(ScreenInfoNextButtonView.ScreenName, true, false, MainController.Instance.IsMultiplayer, GameLevelData.Instance.EnablePauseAccess, descriptionStart);
#endif
			SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerRequestTitlePOI, _currentPOIIndex);
		}

        private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
        {
			if (nameEvent.Equals(EventGameStateKickOutUserFromApp))
			{
				int playerNetID = (int)parameters[0];
				if ((playerNetID == -1) || (MainController.Instance.PlayerView.NetworkGameIDView.GetViewID() == playerNetID))
				{
					ChangeGameLevelState(GameLevelStates.ExitApp);
				}
			}
            if (nameEvent.Equals(EventGameStateRunNetworkStartNarration))
			{
				SystemEventController.Instance.DispatchSystemEvent(ScreenReplayingPOIView.EventScreenReplayingPOIViewDestroy);
				SystemEventController.Instance.DispatchSystemEvent(ScreenHUDAIInteractionView.EventScreenAIInteractionDestroy);
				SystemEventController.Instance.DispatchSystemEvent(ScreenPauseView.EventScreenPauseDestroy);
				SystemEventController.Instance.DispatchSystemEvent(LevelView.EventLevelViewDestroyEasterEgg);
				SystemEventController.Instance.DispatchSystemEvent(POIReplayView.EventPOIReplayViewEnablePOIs, false);	
				_enableDetectionEasterEggs = false;			
				ChangeGameLevelState(GameLevelStates.InGame);
				if (GameObject.FindAnyObjectByType<ScreenInfoNextButtonView>() == null)
				{
					PresentationScreenForNarration();
				}
				MainController.Instance.GuideTourView.RunUpdate = true;				
				TypeActionNext actionNext = (TypeActionNext)Enum.Parse(typeof(TypeActionNext), (string)parameters[0]);
				UIEventController.Instance.DelayUIEvent(GameStateRun.EventGameStateRunTriggerNextButton, 0.1f, actionNext);
				GameLevelData.Instance.EnablePauseAccess = false;
				SystemEventController.Instance.DispatchSystemEvent(DestinationMarker.EventDestinationMarkerDestroy);
			}
			if (nameEvent.Equals(EventGameStateRunNetworkChangeSubState))
			{
				GameLevelStates stateNext = (GameLevelStates)Enum.Parse(typeof(GameLevelStates), (string)parameters[0]);
				if (_gameLevelState != stateNext)
				{
					ChangeGameLevelState(stateNext);
				}				
				GameLevelSubStates subStateNext = (GameLevelSubStates)Enum.Parse(typeof(GameLevelSubStates), (string)parameters[1]);
				ChangeLocalGameLevelSubState(subStateNext);
			}
			if (nameEvent.Equals(EventGameStateRunNetworkRequestSubState))
			{
				if (NetworkController.Instance.IsServer)
				{
					int clientNetID = (int)parameters[0];
					NetworkController.Instance.DelayNetworkEvent(EventGameStateRunNetworkResponseSubState, 0.01f, -1, -1, clientNetID, _currentPOIIndex, _gameLevelState.ToString(), _gameLevelSubState.ToString(), MainController.Instance.GuideTourView.HasBeenInited);
				}
			}
			if (nameEvent.Equals(EventGameStateRunNetworkResponseSubState))
			{
				int clientNetID = (int)parameters[0];
				if (NetworkController.Instance.UniqueNetworkID == clientNetID)
				{
					_currentPOIIndex = (int)parameters[1];
					CurrentPOI = GetPOI(_currentPOIIndex);
					GameLevelStates stateNext = (GameLevelStates)Enum.Parse(typeof(GameLevelStates), (string)parameters[2]);
					if (_gameLevelState != stateNext)
					{
						ChangeGameLevelState(stateNext);
					}
					GameLevelSubStates subStateNext = (GameLevelSubStates)Enum.Parse(typeof(GameLevelSubStates), (string)parameters[3]);
					if (_gameLevelSubState != subStateNext)
					{
						ChangeLocalGameLevelSubState(subStateNext);
					}
					bool setUpNewPosition = (bool)parameters[4];
					if (setUpNewPosition)
					{
						InitializeGuideTour();
					}
				}
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerDoRestart))
			{
				SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerDoRestart, _currentPOI);
			}
			if (nameEvent.Equals(EventGameStateRunEndCurrentNarration))
			{
				UIEventController.Instance.DispatchUIEvent(GameStateRun.EventGameStateRunEndCurrentNarration, _currentPOI);
			}
			if (nameEvent.Equals(LevelView.EventLevelViewUnlockReplayPOI))
			{
				int poiIndex = (int)parameters[0];
				SystemEventController.Instance.DispatchSystemEvent(LevelView.EventLevelViewUnlockReplayPOI, poiIndex);
			}
        }

		private void OnUIEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(EventGameStateRunTriggerPause))
			{
				// FORCE TRANSITION TO EDITIONN IF SYNCRONIZATION AND EDITION ENABLED
				if (MainController.Instance.EnableEditionPOIs)
				{
					if (_gameLevelState == GameLevelStates.Synchronization)
					{
						ChangeGameLevelState(GameLevelStates.EditPOIs);
						ChangeGameLevelSubState(GameLevelSubStates.Idle);
					}
				}
				ChangeToPause();
			}
			if (nameEvent.Equals(EventGameStateRunAIInteraction))
			{
				ChangeToAIInteraction();
			}
			if (nameEvent.Equals(ScreenPauseView.EventScreenPauseViewResumeGame))
			{
				SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerResumeIfPaused);
				MainController.Instance.GuideTourView.RunUpdate = true;
				ChangeGameLevelState(_previousStateToPause);				
			}
			if (nameEvent.Equals(ScreenPauseView.EventScreenPauseViewExitGame))
			{
				string titleWarning = LanguageController.Instance.GetText("text.warning");
				string textAskToExit = LanguageController.Instance.GetText("panel.exit.do.you.want.to.exit.to.main.menu");
				string confirmButton = LanguageController.Instance.GetText("text.confirm");
				string cancelButton = LanguageController.Instance.GetText("text.cancel");
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenConfirmation, null, titleWarning, textAskToExit, SubEventGameStateRunConfirmationExit, confirmButton, cancelButton);
			}
			if (nameEvent.Equals(SubEventGameStateRunConfirmationExit))
			{
				ScreenInformationResponses userResponse = (ScreenInformationResponses)parameters[1];
				if (userResponse == ScreenInformationResponses.Confirm)
				{
					ScreenController.Instance.DestroyScreens();
					MainController.Instance.ChangeGameState(MainController.StatesGame.ReleaseMemory);
				}
			}
			if (nameEvent.Equals(EventGameStateRunTriggerNextButton))
			{
				if (_gameLevelState == GameLevelStates.InGame)
				{
					switch (_gameLevelSubState)
					{
						case GameLevelSubStates.InitialWelcome:
							ChangeGameLevelSubState(GameLevelSubStates.PlayAudio);
							if (MainController.Instance.IsMultiplayer)
							{
								SystemEventController.Instance.DelaySystemEvent(NarrationController.EventNarrationControllerPlayPOIByIndex, 0.4f, _currentPOIIndex);
							}
							else
							{
								SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerPlayPOIByIndex, _currentPOIIndex);							
							}							
							break;

						case GameLevelSubStates.WaitForPlayerClose:
							break;

						case GameLevelSubStates.PlayAudio:
							TypeActionNext action = (TypeActionNext)parameters[0];
							switch (action)
                            {
								case TypeActionNext.Play:
									SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerPlayPOIByIndex, _currentPOIIndex);							
									break;

								case TypeActionNext.Pause:
									SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerDoPause, true, true);
									break;

								case TypeActionNext.Walk:
									ChangeGameLevelSubState(GameLevelSubStates.GoToNextPOI);
									break;
                            }							
							break;
					}
				}
			}
			if (nameEvent.Equals(EventGameStateRunEndCurrentNarration))
            {
				if (_gameLevelState == GameLevelStates.InGame)
				{
					switch (_gameLevelSubState)
					{
						case GameLevelSubStates.PlayAudio:
							SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerDoStop);
							ChangeGameLevelSubState(GameLevelSubStates.GoToNextPOI);
							if ((_currentPOIIndex >= MainController.Instance.LevelView.GetPOIS().Length) && (MainController.Instance.LevelView.GetPOIS().Length > 4))
							{
								ChangeGameLevelState(GameLevelStates.GameOver);
							}
							break;
					}
				}
			}
            if (nameEvent.Equals(EventGameStateRunRestartExperience))
            {
                _currentPOIIndex = 0;
                CurrentPOI = GetPOI(_currentPOIIndex);
                SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerResumeIfPaused);
                MainController.Instance.GuideTourView.RunUpdate = true;
                MainController.Instance.GuideTourView.NavigateTo(_currentPOI);
                ChangeGameLevelState(GameLevelStates.Synchronization);
                ChangeGameLevelSubState(GameLevelSubStates.Idle);
            }
			if (nameEvent.Equals(ScreenInputMuseumFormView.EventScreenInputFormViewSubmitResponse))
            {
				string responseQuestion = (string)parameters[0];
				_questions.Add(new QuestionForm(_questions.Count, responseQuestion));
				GameLevelData.Instance.CurrentQuestion++;
				if (GameLevelData.Instance.CurrentQuestion < GameLevelData.Instance.TotalQuestions)
                {
					CreateNextFormScreen();
				}
				else
                {
					ScreenController.Instance.CreateScreen(ScreenFormMuseumCompletedView.ScreenName, false, true);
#if ENABLE_INPUT_FORM					
					GameLevelData.Instance.InsertFormHTTP(_questions);
#endif					
				}
            }
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(ScreenReplayingPOIView.EventScreenReplayingPOIViewReplay))
			{
				int poiReplayIndex = (int)parameters[0];
				NarrationData narrationReplay = (NarrationData)parameters[1];

				if (!MainController.Instance.IsMultiplayer)
				{
					SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerRunNarrationPOI, poiReplayIndex, narrationReplay);
				}
				else
				{
					if (NetworkController.Instance.IsServer)
					{
						NetworkController.Instance.DispatchNetworkEvent(NarrationController.EventNarrationControllerRequestReplayForAll, -1, -1, poiReplayIndex);
					}
					return;
				}
#if ENABLE_ANALYTICS
				if (!MainController.Instance.EnableEditionPOIs)
				{
					TourAnalyticsController.Instance.LogPOIReplayEvent(GameLevelData.Instance.Age, poiReplayIndex);
				}			
#endif
			}
			if (nameEvent.Equals(PlayerView.EventPlayerDisconnectParent))
			{
				_timerLevel = 0;
				_previousStateToPause = GameLevelStates.Initialization;
				_gameLevelSubState = GameLevelSubStates.Null;
				_presentationCounter = 3;
				_currentPOIIndex = 0;
				_currentPOI = null;
				_totalTimeEasterEgg = -1;		
				_configurationEasterEgg = ConfigurationEasterEggScreen.None;
				_currentEggFound = null;
				_enableDetectionEasterEggs = false;
				_allowTriggerNextPOI = true;
				_questions = new List<QuestionForm>();
				_gameLevelSubState = GameLevelSubStates.Null;
				ChangeGameLevelState(GameLevelStates.Initialization);
			}
			if (nameEvent.Equals(ARMaxSTController.EventARMaxSTControllerAreaRecognized))
			{
				switch (_gameLevelState)
				{
					case GameLevelStates.Synchronization:
						if (_currentPOI != null)
						{
							SystemEventController.Instance.DelaySystemEvent(PlayerView.EventShowArrowPath, 0.2f, _currentPOI.Root);
							if (MainController.Instance.IsMultiplayer)
							{
								if (!NetworkController.Instance.IsServer)
								{
									NetworkController.Instance.DelayNetworkEvent(EventGameStateRunNetworkRequestSubState, 0.01f, -1, -1, NetworkController.Instance.UniqueNetworkID);
								}
							}
						}
						break;

					case GameLevelStates.InGame:
						break;
				}
			}
			if (nameEvent.Equals(ARMaxSTController.EventARMaxSTControllerAreaLost))
            {
				switch (_gameLevelState)
				{
					case GameLevelStates.InGame:					
#if ENABLE_ANALYTICS
						if (!MainController.Instance.EnableEditionPOIs)					
						{
							TourAnalyticsController.Instance.LogTrackingLostEvent(GameLevelData.Instance.Age, _currentPOIIndex);
						}						
#endif						
						UIEventController.Instance.DispatchUIEvent(GameStateRun.EventGameStateRunTriggerPause);
						break;
				}				
			}
			if (nameEvent.Equals(TourGuideView.EventTourGuideViewReachedTarget))
			{
				if (_gameLevelState == GameLevelStates.InGame)
				{
					switch (_gameLevelSubState)
					{
						case GameLevelSubStates.GoToNextPOI:
							ChangeGameLevelSubState(GameLevelSubStates.WaitForPlayerClose);
							break;
					}
				}
			}
			if (nameEvent.Equals(TourGuideView.EventTourGuideViewReportPlayerClose))
			{
				if (_gameLevelState == GameLevelStates.InGame)
				{
					switch (_gameLevelSubState)
					{
						case GameLevelSubStates.WaitForPlayerClose:
							bool shouldRun = false;
							if (!MainController.Instance.IsMultiplayer)
							{
								shouldRun = true;
							}
							else
							{
								if (NetworkController.Instance.IsServer)
								{
									shouldRun = true;
								}
							}
							if (_allowTriggerNextPOI && shouldRun)
							{
								RunActionWhenPlayerCloseBy();
							}
							break;
					}
				}
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerPlayInfo))
			{
				bool mainNarration = (bool)parameters[0];
				if (mainNarration)
				{
					_enableDetectionEasterEggs = false;
					if (_currentEggFound != null)
					{
						SystemEventController.Instance.DispatchSystemEvent(ScreenFoundEasterEggView.EventScreenFoundEasterEggViewDestroy);
						_currentEggFound.SetActive(false);
					}
					_currentEggFound = null;
				}
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerPaused))
			{
				_enableDetectionEasterEggs = true;
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerFinished))
			{
				_enableDetectionEasterEggs = true;
			}
			if (nameEvent.Equals(LevelView.EventLevelViewPlayEasterEgg))
			{
				int indexEasterEgg = (int)parameters[0];
				string nameEasterEgg = (string)parameters[1];
				MainController.Instance.ReferenceEasterEgg = (GameObject)parameters[2];
				MainController.Instance.RangeDetectionEasterEggPlaying = GameLevelData.Instance.DistanceToTriggerGuide * 1.5f;
				bool hasBeenPlayed = HasBeenPlayed(nameEasterEgg);
				// CUSTOM EASTER EGGS
				if (nameEasterEgg.Equals(LevelView.EventEasterEggVideo1))
				{
#if UNITY_EDITOR
					_totalTimeEasterEgg = 3;
#else
					_totalTimeEasterEgg = 10;
#endif					
					MainController.Instance.RangeDetectionEasterEggPlaying = GameLevelData.Instance.DistanceToTriggerGuide * 1.5f;
					_configurationEasterEgg = ConfigurationEasterEggScreen.Video;
					ChangeToEasterEgg();
					MainController.Instance.CreateVideoController(true, "BigBuckBunny640", NavMeshController.Instance.AreaMaxST.transform, MainController.Instance.ReferenceEasterEgg.transform.position, MainController.Instance.ReferenceEasterEgg.transform.rotation, new Vector3(2,2,2), true, true);
				} 
				else
				if (nameEasterEgg.Equals(LevelView.EventEasterEggPhotos1))
				{
#if UNITY_EDITOR					
					_totalTimeEasterEgg = 3;
#else
					_totalTimeEasterEgg = 10;
#endif					
					_configurationEasterEgg = ConfigurationEasterEggScreen.Photos;
					ChangeToEasterEgg();
					string[] photoNames = new string[] { "deer", "desert", "hummingbird", "sunflower", "sunset", "yosemite" }; 
					MainController.Instance.CreatePhotoGalleryController(true, photoNames, NavMeshController.Instance.AreaMaxST.transform, MainController.Instance.ReferenceEasterEgg.transform.position, MainController.Instance.ReferenceEasterEgg.transform.rotation, new Vector3(2,2,2));
				}
				else
				{
					// GENERIC NARRATION FOR EASTER EGG
#if UNITY_EDITOR
					_totalTimeEasterEgg = 3;
#else
					_totalTimeEasterEgg = 10;
#endif
					_configurationEasterEgg = ConfigurationEasterEggScreen.Narration;
					ChangeToEasterEgg();				
					NarrationController narrationEasterEggController = MainController.Instance.CreateNarrationGeneric(_currentEggFound.Narration, false, true);
					narrationEasterEggController.Secret = indexEasterEgg;
					narrationEasterEggController.Play();	
				}
				if (hasBeenPlayed)
                {
					SystemEventController.Instance.DispatchSystemEvent(EventGameStateRunShowResumeButtonInEasterEgg);
				}
			}
			if (nameEvent.Equals(LevelView.EventLevelViewDestroyEasterEgg))
			{
				if (_currentEggFound != null)
                {
					_currentEggFound.Target.SetActive(true);
					_currentEggFound.SetActive(false);
				}
			}
			if (nameEvent.Equals(PanelVideoEffectView.PanelVideoEffectViewStarted))
			{
				_allowTriggerNextPOI = false;
			}
			if (nameEvent.Equals(PanelVideoEffectView.PanelVideoEffectViewCompleted))
			{
				_allowTriggerNextPOI = true;
			}
            if (nameEvent.Equals(NarrationToken.EventNarrationTokenEnd))
            {
                bool mainNarration = (bool)parameters[0];
                if (mainNarration)
                {
					int currPOI = (int)parameters[1];
                    string endEventNarration = (string)parameters[2];
                    if (endEventNarration.Equals(TourGuideView.EventPOIGameOver))
                    {
                        ChangeGameLevelState(GameLevelStates.GameOver);
                    }
                }
            }        	
            if (nameEvent.Equals(POIReplayView.EventPOIReplayViewDisplayScreen))
            {				
                switch (_gameLevelState)
                {
                    case GameLevelStates.InGame:
						if (CheckAllowInteractionOutsideNarration())
						{
							int poiReplay = (int)parameters[0];
							string description = LanguageController.Instance.GetText("screen.replay.previous.poi.description");
							bool shouldShowScreen = false;
							if (!MainController.Instance.IsMultiplayer)
							{
								shouldShowScreen = true;
							}
							else
							{
								if (NetworkController.Instance.IsServer)
								{
									shouldShowScreen = true;
								}
							}						
							if (shouldShowScreen)
							{
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
								Transform prevPOI = (Transform)parameters[1];
								Vector3 normalToTarget = (prevPOI.position - MainController.Instance.PlayerView.transform.position).normalized;
								Vector3 posScreen = prevPOI.position;
								ScreenController.Instance.CreateScreen3DAnchor(ScreenReplayPOIView.ScreenName, null, posScreen, normalToTarget, SizeEasterEggNarration, false, true, description, poiReplay);
#else
								ScreenController.Instance.CreateScreen(ScreenReplayPOIView.ScreenName, false, true, description, poiReplay);
#endif						
							}
						}
                        break;
                }
            }
            if (nameEvent.Equals(NarrationController.EventNarrationControllerRunNarrationPOI))
            {
                int poiReplayIndex = (int)parameters[0];
                NarrationData narrationReplay = (NarrationData)parameters[1];
                _replayPOI = GetPOI(poiReplayIndex);
                ScreenController.Instance.CreateScreen(ScreenReplayingPOIView.ScreenName, false, true, poiReplayIndex, narrationReplay, _replayPOI);
                ChangeToNarrationReplay();
            }
            if (nameEvent.Equals(EventGameStateRunNavigateToCurrentPOI))
            {
				if (_replayPOI != null)
				{
					MainController.Instance.GuideTourView.SetPositionOutsideNarration(_replayPOI.GOPosition.transform.position);
					_replayPOI = null;
				}
                MainController.Instance.GuideTourView.NavigateTo(_currentPOI);
            }
		}

		private void ChangeToPause()
		{
			_previousStateToPause = _gameLevelState;
			ChangeGameLevelState(GameLevelStates.Pause);			
			if (_subStateEditPOI != null)
			{
				_subStateEditPOI.Reset();
			}
		}

		private void ChangeToEasterEgg()
		{
			_previousStateToPause = _gameLevelState;
			ChangeGameLevelState(GameLevelStates.EasterEgg);
		}

		private void ChangeToAIInteraction()
		{
#if !ENABLE_AI_OPERATIONS
			string information = LanguageController.Instance.GetText("text.error");
			string aiOperationNotEnabled = LanguageController.Instance.GetText("message.ai.operation.not.enabled");
			UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
			ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, information, aiOperationNotEnabled);
#else
			if ((UsersController.Instance.CurrentUser != null) && !UsersController.Instance.CurrentUser.IsEmptyUser())
			{
				_previousStateToPause = _gameLevelState;
				ChangeGameLevelState(GameLevelStates.AIInteraction);
			}
			else
			{
				UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, LanguageController.Instance.GetText("text.warning"), LanguageController.Instance.GetText("screen.ai.interaction.login.required"));				
			}
#endif			
		}

        private void ChangeToNarrationReplay()
        {
            _previousStateToPause = _gameLevelState;
            ChangeGameLevelState(GameLevelStates.NarrationReplay);
        }

		private void ChangeGameLevelState(GameLevelStates newGameLevelState)
		{
			_gameLevelState = newGameLevelState;
			_timerLevel = 0;
			ApplyActionState();
		}

		private void ApplyActionState()
		{
			// Debug.LogError("MAIN STATE=" + _gameLevelState.ToString());
			switch (_gameLevelState)
			{
				case GameLevelStates.Initialization:
					SystemEventController.Instance.DispatchSystemEvent(CameraController.EventCameraPlayerUnlinkCameraFromPlayer);
					BulletsController.Instance.Initialize(10);
					FXsController.Instance.Initialize();
					SpeechRecognitionController.Instance.Initialize();
					_currentPOIIndex = 0;
					break;

				case GameLevelStates.Synchronization:
					SystemEventController.Instance.DispatchSystemEvent(POIReplayView.EventPOIReplayViewEnablePOIs, false);
					SoundsController.Instance.StopAllSounds();
					ScreenController.Instance.CreateScreen(ScreenSynchronizationView.ScreenName, true, false);
					if (MainController.Instance.IsMultiplayer && !NetworkController.Instance.IsServer && !MainController.Instance.IsARMode)
					{
						NetworkController.Instance.DelayNetworkEvent(EventGameStateRunNetworkRequestSubState, 0.01f, -1, -1, NetworkController.Instance.UniqueNetworkID);
					}								
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
					VRInputController.Instance.SpeedJoystickMovement = GameLevelData.Instance.PlayerVRSpeed;
					VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerEnableLocomotion, true);
#endif						
					break;

				case GameLevelStates.InGame:
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, true);
					SystemEventController.Instance.DispatchSystemEvent(POIReplayView.EventPOIReplayViewEnablePOIs, true);
					MainController.Instance.PlayerView.ActivatePhysics(true);
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
					VRInputController.Instance.SpeedJoystickMovement = GameLevelData.Instance.PlayerVRSpeed;
					VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerEnableLocomotion, true);
#endif						
					break;

				case GameLevelStates.Pause:
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, false);
					SystemEventController.Instance.DispatchSystemEvent(POIReplayView.EventPOIReplayViewEnablePOIs, false);
					SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerDoPause, true);
					MainController.Instance.GuideTourView.RunUpdate = false;					
					ScreenController.Instance.CreateScreen(ScreenPauseView.ScreenName, false, true);
					break;

				case GameLevelStates.EasterEgg:
					MainController.Instance.GuideTourView.RunUpdate = false;
					SystemEventController.Instance.DispatchSystemEvent(POIReplayView.EventPOIReplayViewEnablePOIs, false);
					SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerDoPause, true);
					switch (_configurationEasterEgg)
                    {
						case ConfigurationEasterEggScreen.Narration:
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
							Vector3 normalToTarget = (_currentEggFound.Star.transform.position - MainController.Instance.PlayerView.transform.position).normalized;
							Vector3 posScreen = _currentEggFound.Star.transform.position;
							GameObject screenNarration = ScreenController.Instance.CreateScreen3DAnchor(ScreenHUDEasterEggView.ScreenName, _currentEggFound.Star, posScreen, normalToTarget, SizeEasterEggNarration, false, true, _totalTimeEasterEgg, LanguageController.Instance.GetText(_currentEggFound.GetTitle()));
							screenNarration.gameObject.transform.parent = ScreenController.Instance.transform;
							screenNarration.gameObject.SetActive(true);
#else
							ScreenController.Instance.CreateScreen(ScreenHUDEasterEggView.ScreenName, false, true, _totalTimeEasterEgg, LanguageController.Instance.GetText(_currentEggFound.GetTitle()));
#endif						
							break;

						case ConfigurationEasterEggScreen.Video:
							ScreenController.Instance.CreateScreen(ScreenHUDEggVideoView.ScreenName, false, true, _totalTimeEasterEgg, true);
							break;

						case ConfigurationEasterEggScreen.Photos:
							ScreenController.Instance.CreateScreen(ScreenHUDEggPhotosView.ScreenName, false, true, _totalTimeEasterEgg, true);
							break;

						case ConfigurationEasterEggScreen.Discover:
							ScreenController.Instance.CreateScreen(ScreenHUDEggDiscoverView.ScreenName, false, true, _totalTimeEasterEgg);							
							break;
					}
					break;

				case GameLevelStates.AIInteraction:
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, false);
					MainController.Instance.GuideTourView.RunUpdate = false;
					SystemEventController.Instance.DispatchSystemEvent(POIReplayView.EventPOIReplayViewEnablePOIs, false);
					SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerDoPause, true);
					ScreenController.Instance.CreateScreen(ScreenHUDAIInteractionView.ScreenName, false, true);												
					break;

                case GameLevelStates.NarrationReplay:
                    SystemEventController.Instance.DispatchSystemEvent(POIReplayView.EventPOIReplayViewEnablePOIs, false);
                    MainController.Instance.GuideTourView.RunUpdate = false;
                    SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerDoPause, true);
                    break;

				case GameLevelStates.GameOver:
					SystemEventController.Instance.DispatchSystemEvent(POIReplayView.EventPOIReplayViewEnablePOIs, false);
					SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerDoPause, true);
					MainController.Instance.GuideTourView.RunUpdate = false;
					GameLevelData.Instance.CurrentQuestion = 0;
					MainController.Instance.CompletedArea = true;
#if ENABLE_INPUT_FORM						
					CreateNextFormScreen();
#else
					ScreenController.Instance.CreateScreen(ScreenFormMuseumCompletedView.ScreenName, false, true);
#endif					
					break;

				case GameLevelStates.ExitApp:
					UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyAllScreens);
					string titleInfo = LanguageController.Instance.GetText("screen.game.run.exit.title");
					string descriptionInfo = LanguageController.Instance.GetText("screen.game.run.exit.description");
					ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, titleInfo, descriptionInfo);
					NetworkController.Instance.Disconnect();
					break;

				case GameLevelStates.EditPOIs:
					// UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyAllScreens);
					if (_subStateEditPOI == null)
					{
						_subStateEditPOI = new GameSubStateEditPOI();
						_subStateEditPOI.Initialize();
					}					
					SystemEventController.Instance.DispatchSystemEvent(LevelView.EventLevelViewEnablePOIsForEdit);
					MainController.Instance.GuideTourView.RunUpdate = false;
					break;
			}
		}

		private void CreateNextFormScreen()
        {
			string page = LanguageController.Instance.GetText("screen.questionarie.page") + (GameLevelData.Instance.CurrentQuestion + 1) + LanguageController.Instance.GetText("screen.questionarie.of") + GameLevelData.Instance.TotalQuestions;
			string nextText = LanguageController.Instance.GetText("screen.questionarie.next");
			string description = LanguageController.Instance.GetText("screen.questionarie.question." + GameLevelData.Instance.CurrentQuestion);
			ScreenController.Instance.CreateScreen(ScreenInputMuseumFormView.ScreenName, false, true, page, description, nextText);
		}

		private POIData GetPOI(int poi)
		{
			POIData[] poiData = MainController.Instance.LevelView.GetPOIS();
			if (poiData != null)
			{
				int finalPOI = poi % MainController.Instance.LevelView.GetPOIS().Length;
				if (!MainController.Instance.EnableEditionPOIs)
				{
					SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerRequestPOIAudios, finalPOI);
				}			
				return MainController.Instance.LevelView.GetPOIS()[finalPOI];
			}
			else
			{
				return null;
			}
		}

		private void ChangeGameLevelSubState(GameLevelSubStates newSubState)
		{
			bool shouldRun = false;
			bool shouldReportNetworkEvent = false;
			if (!MainController.Instance.IsMultiplayer)
			{
				shouldRun = true;
			}
			else
			{
				if (NetworkController.Instance.IsServer)
				{
					shouldRun = true;
					shouldReportNetworkEvent = true;
				}
			}
			if (shouldRun)
			{
				if (shouldReportNetworkEvent)
				{
					NetworkController.Instance.DelayNetworkEvent(EventGameStateRunNetworkChangeSubState, 0.01f, -1, -1, _gameLevelState.ToString(), newSubState.ToString());	
				}
				else
				{
					ChangeLocalGameLevelSubState(newSubState);
				}
			}
		}

		private void InitializeGuideTour()
		{
			if (MainController.Instance.GuideTourView != null)
			{
				if (!MainController.Instance.GuideTourView.HasBeenInited)
				{
					MainController.Instance.GuideTourView.HasBeenInited = true;
					MainController.Instance.GuideTourView.Activate(true);
					MainController.Instance.GuideTourView.SetPosition(_currentPOI.GOPosition.transform.position);
					MainController.Instance.GuideTourView.FacePosition(MainController.Instance.PlayerView.gameObject.transform.position);
					MainController.Instance.GuideTourView.gameObject.transform.parent = MainController.Instance.LevelView.Content.transform;
				}
			}
		}

		private void ChangeLocalGameLevelSubState(GameLevelSubStates newSubState)
		{
			string description = "";
			string buttonText = "";

			_gameLevelSubState = newSubState;
			// Debug.LogError("SUB STATE=" + _gameLevelSubState.ToString());
			switch (_gameLevelSubState)
			{
				case GameLevelSubStates.InitialWelcome:
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventDestroyArrowPath);
					InitializeGuideTour();
					description = "screen.initial.presentation.everything.ready";
					buttonText = "screen.initial.presentation.next.action";
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
					Vector3 forwardGuide = -MainController.Instance.GuideTourView.GetModel().transform.forward;
					GameObject contentVRScreen = MainController.Instance.GuideTourView.ScreenVR;
					Vector3 posScreen = contentVRScreen.transform.position;
					ScreenController.Instance.CreateScreen3DAnchor(ScreenInfoNextButtonView.ScreenName, MainController.Instance.GuideTourView.ScreenVR, posScreen, forwardGuide, SizeInfoScreen, true, false, MainController.Instance.IsMultiplayer, GameLevelData.Instance.EnablePauseAccess, description, buttonText);
#else
					ScreenController.Instance.CreateScreen(ScreenInfoNextButtonView.ScreenName, true, false, MainController.Instance.IsMultiplayer, GameLevelData.Instance.EnablePauseAccess, description, buttonText);
#endif
					SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerRequestTitlePOI, _currentPOIIndex);
#if UNLOCK_EVERYTHING
					_enableDetectionEasterEggs = true;
#endif					
					break;

				case GameLevelSubStates.WaitForPlayerClose:
					MainController.Instance.GuideTourView.WaitForPlayerToBeClose();
					break;

				case GameLevelSubStates.PlayAudio:
					bool shouldRunPlayAudio = false;
					if (!MainController.Instance.IsMultiplayer)
					{
						shouldRunPlayAudio = true;
					}
					else
					{
						if (NetworkController.Instance.IsServer)
						{
							shouldRunPlayAudio = true;
						}
					}
					if (shouldRunPlayAudio)
					{
						PresentationScreenForNarration();
					}
					break;

				case GameLevelSubStates.PlayAnimation:
					break;										

				case GameLevelSubStates.Completed:
					break;				

				case GameLevelSubStates.GoToNextPOI:
					SoundsController.Instance.StopAllSounds();
					SystemEventController.Instance.DispatchSystemEvent(TourGuideView.EventTourGuideViewEnableModel, true);
					if ((_currentPOI.EventEnd != null) && (_currentPOI.EventEnd.Length > 0))
					{
						SystemEventController.Instance.DispatchSystemEvent(_currentPOI.EventEnd, _currentPOI.ExtraData);
					}
					ScreenController.Instance.CreateScreen(ScreenHUDView.ScreenName, true, false);
					MainController.Instance.GuideTourView.SetPositionOutsideNarration(_currentPOI.GOPosition.transform.position);
					_currentPOIIndex++;
					CurrentPOI = GetPOI(_currentPOIIndex);
					MainController.Instance.GuideTourView.NavigateTo(_currentPOI);
					if ((_currentPOIIndex >= MainController.Instance.LevelView.GetPOIS().Length) && (MainController.Instance.LevelView.GetPOIS().Length > 4))
					{
						ChangeGameLevelState(GameLevelStates.GameOver);
					}
					GameLevelData.Instance.EnablePauseAccess = true;
					_enableDetectionEasterEggs = true;
					SystemEventController.Instance.DispatchSystemEvent(POIReplayView.EventPOIReplayViewEnablePOIs, true);
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventShowArrowPath, _currentPOI.Root);
					if (MainController.Instance.IsMultiplayer)
					{
						NetworkController.Instance.DelayNetworkEvent(NarrationToken.EventNarrationTokenDestroyNarrationObject, 0.01f, -1, -1);
					}
					else
					{
						SystemEventController.Instance.DispatchSystemEvent(NarrationToken.EventNarrationTokenDestroyNarrationObject);
					}					
					break;

				case GameLevelSubStates.Idle:
					ScreenController.Instance.CreateScreen(ScreenHUDView.ScreenName, true, false);
					break;
			}
		}

		private void RunGameLevelSubState()
		{
			MainController.Instance.PlayerView.Run();

#if UNITY_EDITOR
			if (Input.GetKeyDown(KeyCode.J) && ((Input.GetKey(KeyCode.LeftControl)|| (Input.GetKey(KeyCode.LeftCommand)))))
            {
				SystemEventController.Instance.DispatchSystemEvent(LevelView.EventLevelViewUnlockEasterEggs);
            }
#endif
			switch (_gameLevelSubState)
			{
				case GameLevelSubStates.InitialWelcome:
					break;

				case GameLevelSubStates.WaitForPlayerClose:
					CheckInteractionWithEasterEgg();
					break;

				case GameLevelSubStates.PlayAudio:
					CheckInteractionWithEasterEgg();
					break;					

				case GameLevelSubStates.PlayAnimation:
					break;

				case GameLevelSubStates.GoToNextPOI:
					CheckInteractionWithEasterEgg();
					break;

				case GameLevelSubStates.Completed:
					break;										

				case GameLevelSubStates.Idle:
					break;
			}
		}

		private void CreateTourGuideInPOI(int currentPOIIndex)
		{
			CurrentPOI = GetPOI(currentPOIIndex);						
			Vector3 initialPosition = MainController.Instance.LevelView.InitialPosition.transform.position;
			MainController.Instance.CreateTourGuide(initialPosition);
		}

		public void Run()
		{
			GameLevelData.Instance.TotalTimeDone += Time.deltaTime;

			switch (_gameLevelState)
			{
				case GameLevelStates.Initialization:
					_timerLevel += Time.deltaTime;
					if ((_timerLevel > 0.2f) 
						&& (MainController.Instance.LevelView != null)
						&& (MainController.Instance.PlayerView != null))
					{
						CreateTourGuideInPOI(_currentPOIIndex);
						if (MainController.Instance.IsARMode)
						{							
#if !UNITY_WEBGL && !ENABLE_VUFORIA && ENABLE_MAXST
							ARMaxSTController.Instance.AddTrackerData(MainController.Instance.LevelView.MaxSTPackageFileName);
#endif
						}
						ChangeGameLevelState(GameLevelStates.Synchronization);
					}
					break;

				case GameLevelStates.Synchronization:		
#if !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || UNITY_WEBGL) && !ENABLE_VUFORIA
					MainController.Instance.PlayerView.RotateCamera();
#endif
					MainController.Instance.PlayerView.Run();
					float distanceToInitialPOI = 0;
					if (_currentPOI != null)
					{
						if (MainController.Instance.IsNormalAxis)
						{
							distanceToInitialPOI = yourvrexperience.Utils.Utilities.DistanceXZ(_currentPOI.GOPosition.transform.position, MainController.Instance.PlayerView.transform.position);
						}
						else
						{
							distanceToInitialPOI = yourvrexperience.Utils.Utilities.DistanceXY(_currentPOI.GOPosition.transform.position, MainController.Instance.PlayerView.transform.position);
						}
					}
					bool hasBeenARRecognized = true;
					if (MainController.Instance.IsARMode)
					{
#if ENABLE_VUFORIA
						hasBeenARRecognized = VuforiaController.Instance.HasAreaBeenDetected;
#elif ENABLE_NIANTIC
						hasBeenARRecognized = NianticController.Instance.HasAreaBeenDetected;
#else			
						hasBeenARRecognized = ARMaxSTController.Instance.HasAreaBeenDetected;
#endif			 
					}
					if (_currentPOI == null)
					{
						ChangeGameLevelState(GameLevelStates.EditPOIs);
						ChangeGameLevelSubState(GameLevelSubStates.Idle);
					}
					else
					{
						bool isPOIVisible = yourvrexperience.Utils.Utilities.IsVisibleFrom(_currentPOI.GOPosition.transform.position, MainController.Instance.GameInputController.Camera);
#if !UNITY_EDITOR						
#if ENABLE_VUFORIA 
						isPOIVisible = VuforiaController.Instance.CheckVisiblePoint(_currentPOI.GOPosition.transform.position);
#elif ENABLE_MAXST
						isPOIVisible = ARMaxSTController.Instance.CheckVisiblePoint(_currentPOI.GOPosition.transform.position);
#elif ENABLE_NIANTIC
						isPOIVisible = NianticController.Instance.CheckVisiblePoint(_currentPOI.GOPosition.transform.position);						
#endif
#endif
						if (hasBeenARRecognized && !MainController.Instance.EnableEditionPOIs)
						{
							InitializeGuideTour();
						}

						if (hasBeenARRecognized && (distanceToInitialPOI < GameLevelData.Instance.DistanceToTriggerGuide) && isPOIVisible)
						{							
							if (!MainController.Instance.IsMultiplayer)
							{
								ScreenController.Instance.DestroyScreens();
								if (MainController.Instance.EnableEditionPOIs)
								{
									ChangeGameLevelState(GameLevelStates.EditPOIs);
									ChangeGameLevelSubState(GameLevelSubStates.Idle);
								}
								else
								{
									ChangeGameLevelState(GameLevelStates.InGame);	
									ChangeGameLevelSubState(GameLevelSubStates.InitialWelcome);
								}
							}
							else
							{
								if (NetworkController.Instance.IsServer)
								{
									ScreenController.Instance.DestroyScreens();
									ChangeGameLevelState(GameLevelStates.InGame);
									ChangeGameLevelSubState(GameLevelSubStates.InitialWelcome);
								}
								else
								{
									ChangeGameLevelState(GameLevelStates.InGame);
								}
							}
						}
					}

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
					if (VRInputController.Instance.ActionMenuPressed())
					{
						ChangeToPause();
					}
#endif					
					break;

				case GameLevelStates.InGame:
					_timerLevel += Time.deltaTime;
					if (_timerLevel > 1)
					{
						_timerLevel -= 1;
						GameLevelData.Instance.CurrentTime++;
					}

					RunGameLevelSubState();

					if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L))
                    {
						SystemEventController.Instance.DispatchSystemEvent(ARMaxSTController.EventARMaxSTControllerAreaLost);
					}

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
					if (VRInputController.Instance.ActionMenuPressed())
					{
						ChangeToPause();
					}
#endif					

					if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.N))
                    {
						if (MainController.Instance.IsMultiplayer)
						{
							NetworkController.Instance.DelayNetworkEvent(MainController.EventMainControllerChangeCurrentLevel, 0.01f, -1, -1, (MainController.Instance.CurrentGameLevel + 1));
						}
						else
						{
							SystemEventController.Instance.DispatchSystemEvent(MainController.EventMainControllerChangeCurrentLevel, (MainController.Instance.CurrentGameLevel + 1));
						}						
					}
					break;

				case GameLevelStates.Pause:
					MainController.Instance.PlayerView.Run();
					break;

				case GameLevelStates.EasterEgg:
					MainController.Instance.PlayerView.Run();
					break;

				case GameLevelStates.AIInteraction:
					Template6DOFAIData.Instance.Update();
					MainController.Instance.PlayerView.Run();
					break;

                case GameLevelStates.NarrationReplay:
                    MainController.Instance.PlayerView.Run();
                    break;

				case GameLevelStates.GameOver:
					// MainController.Instance.PlayerView.Run();
					/*
					_timerLevel += Time.deltaTime;
					if (_timerLevel > 6)
					{
						MainController.Instance.ChangeGameState(MainController.StatesGame.ReleaseMemory);
					}
					*/
					break;

				case GameLevelStates.ExitApp:
					_timerLevel += Time.deltaTime;
					if (_timerLevel > 6)
					{
						Application.Quit();
					}
					break;

				case GameLevelStates.EditPOIs:
					_subStateEditPOI.Run();
					break;
			}
		}
	}
}
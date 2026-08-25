using UnityEngine;
using UnityEngine.Assertions;
using yourvrexperience.Narration;
using yourvrexperience.Networking;
using yourvrexperience.Utils;
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
using yourvrexperience.VR;
#endif

namespace yourvrexperience.template6dof
{
	public class GameStateMenu : IGameState
    {
		public const float BoxGunShiftFromCamera = -1;

		public const string EventGameStateMenuPositionReady = "EventGameStateMenuPositionReady";
		public const string EventGameStateMenuQuitGame = "EventGameStateMenuQuitGame";

		public const string SubEventExitAppConfirmation = "SubEventExitAppConfirmation";		

		private GameObject _source;

		public void Initialize()
		{
			UIEventController.Instance.Event += OnUIEvent;
			SystemEventController.Instance.Event += OnSystemEvent;
						
			if (MainController.Instance.PreviousState == MainController.StatesGame.None)
			{				
				MainController.Instance.FadeOutCamera();
			}
			MainController.Instance.CreateMenuLevelView();		
			GameLevelData.Instance.ResetGameLevelData();	

			Assert.IsNull(MainController.Instance.PlayerView, "The player is not null");

#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
			VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerEnableLocomotion, false);
#if ENABLE_NREAL				
			ScreenController.Instance.CreateScreen(ScreenMainMenuView.ScreenName, true, false);
#else 
			ScreenController.Instance.CreateForwardScreen(ScreenMainMenuView.ScreenName, new Vector3(0, 0, 1), true, false);
#endif				
#else
			ScreenController.Instance.CreateScreen(ScreenMainMenuView.ScreenName, true, false);			
#endif			
		}

		public void Destroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}

        private void OnUIEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(ScreenMainMenuView.EventScreenMainMenuViewPlayGame))
			{		
				MainController.Instance.InitialPositioningDone = false;
				if (GameLevelData.Instance.TotalAreas > 1)
				{
					MainController.Instance.ChangeGameState(MainController.StatesGame.Floor);
				}
				else
				{
					if (MainController.Instance.IsMultiplayer)
					{
						MainController.Instance.ChangeGameState(MainController.StatesGame.Network);
					}
					else
					{
						MainController.Instance.NumberClients = 1;
						MainController.Instance.ChangeGameState(MainController.StatesGame.Loading);
					}
				}
			}
			if (nameEvent.Equals(ScreenMainMenuView.EventScreenMainMenuViewSettings))
			{
				MainController.Instance.ChangeGameState(MainController.StatesGame.Settings);
			}
			if (nameEvent.Equals(ScreenMainMenuView.EventScreenMainMenuViewExitGame))
			{
				_source = (GameObject)parameters[0];
				string titleWarning = LanguageController.Instance.GetText("text.warning");
				string textAskToExit = LanguageController.Instance.GetText("screen.main.do.you.want.to.exit");
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenConfirmation, _source, titleWarning, textAskToExit, SubEventExitAppConfirmation);
			}
			if (nameEvent.Equals(SubEventExitAppConfirmation))
			{
				ScreenInformationResponses userResponse = (ScreenInformationResponses)parameters[1];
				if (userResponse == ScreenInformationResponses.Confirm)
				{
					SoundsController.Instance.PlaySoundFX(GameSounds.FxSelection, false, 1);
					ScreenController.Instance.DestroyScreens();
					string titleInfo = LanguageController.Instance.GetText("text.info");
					string textNowExiting = LanguageController.Instance.GetText("screen.main.now.exiting");
					ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, _source, titleInfo, textNowExiting);
					SystemEventController.Instance.DelaySystemEvent(EventGameStateMenuQuitGame, 2);
				}
			}
			if (nameEvent.Equals(ScreenFloorView.EventScreenFloorSelected))
			{
				MainController.Instance.CurrentGameLevel = (int)parameters[0];
				if (MainController.Instance.IsMultiplayer)
				{
					MainController.Instance.ChangeGameState(MainController.StatesGame.Network);
				}
				else
				{
					MainController.Instance.NumberClients = 1;
					MainController.Instance.ChangeGameState(MainController.StatesGame.Loading);
				}
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(CameraFader.EventCameraFaderFadeCompleted))
			{
				bool isFadeIn = (bool)parameters[0];
			}			
            if (nameEvent.Equals(PlayerView.EventPlayerViewPositionUpdated))
			{
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)				
				ScreenController.Instance.CreateForwardScreen(ScreenMainMenuView.ScreenName, new Vector3(0, 0, 1), true, false);
#elif ENABLE_NREAL				
				ScreenController.Instance.CreateScreen(ScreenMainMenuView.ScreenName, true, false);
#endif				
			}
        }
		
		public void Run()
		{
		}
	}
}
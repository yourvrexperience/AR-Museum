using UnityEngine;
using yourvrexperience.Utils;
using yourvrexperience.VR;

namespace yourvrexperience.template6dof
{
	public class GameStatePause : IGameState
    {
		private GameObject _source;

		public void Initialize()
		{
			UIEventController.Instance.Event += OnUIEvent;

			ScreenController.Instance.CreateScreen(ScreenPauseView.ScreenName, true, false);

#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
			VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerEnableLocomotion, false);
#endif			
		}

        public void Destroy()
		{
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}
		
		private void OnUIEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(ScreenPauseView.EventScreenPauseViewResumeGame))
			{
				ScreenController.Instance.DestroyScreens();
				MainController.Instance.ChangeGameState(MainController.StatesGame.Run);
			}
			if (nameEvent.Equals(ScreenPauseView.EventScreenPauseViewExitGame))
			{
				_source = (GameObject)parameters[0];
				string titleWarning = LanguageController.Instance.GetText("text.warning");
				string textAskToExit = LanguageController.Instance.GetText("screen.pause.exit.to.main");
				string confirmButton = LanguageController.Instance.GetText("text.confirm");
				string cancelButton = LanguageController.Instance.GetText("text.cancel");
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenConfirmation, _source, titleWarning, textAskToExit, "", confirmButton, cancelButton);
			}
			if (nameEvent.Equals(ScreenInformationView.EventScreenInformationResponse))
			{
				ScreenInformationResponses userResponse = (ScreenInformationResponses)parameters[1];
				if (userResponse == ScreenInformationResponses.Confirm)
				{
					ScreenController.Instance.DestroyScreens();
					MainController.Instance.ChangeGameState(MainController.StatesGame.ReleaseMemory);
				}
			}		
        }

		public void Run()
		{
		}
	}
}
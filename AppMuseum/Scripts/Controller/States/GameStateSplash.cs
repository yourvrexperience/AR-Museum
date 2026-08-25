using UnityEngine;
using yourvrexperience.Utils;
using System;
using yourvrexperience.UserManagement;
using yourvrexperience.Networking;

namespace yourvrexperience.template6dof
{
	public class GameStateSplash : IGameState
    {
		public const string EventGameStateSplashCompleted = "EventGameStateSplashCompleted";

		public void Initialize()
		{
			SystemEventController.Instance.Event += OnSystemEvent;

			CameraFader.Instance.FadeOut();

#if UNITY_EDITOR
			SystemEventController.Instance.DelaySystemEvent(EventGameStateSplashCompleted, 0.01f);
#else				
			SystemEventController.Instance.DelaySystemEvent(EventGameStateSplashCompleted, 2);
#endif				
			ScreenController.Instance.CreateScreen(ScreenSplashView.ScreenName, true, false);
		}

        public void Destroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(EventGameStateSplashCompleted))
			{
				MainController.Instance.ChangeGameState(MainController.StatesGame.Download);				
			}
        }
		
		public void Run()
		{
		}
	}
}
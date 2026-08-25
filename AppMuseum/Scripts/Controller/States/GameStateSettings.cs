using System;
using UnityEngine;
using UnityEngine.Assertions;
using yourvrexperience.Networking;
using yourvrexperience.Utils;

namespace yourvrexperience.template6dof
{
	public class GameStateSettings : IGameState
    {
		public void Initialize()
		{
			UIEventController.Instance.Event += OnUIEvent;
			SystemEventController.Instance.Event += OnSystemEvent;

			ScreenController.Instance.CreateScreen(ScreenSettingsView.ScreenName, true, false);
		}

        public void Destroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}

        private void OnUIEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(ScreenSettingsView.EventScreenSettingsViewBack))
			{
				MainController.Instance.ChangeGameState(MainController.StatesGame.MainMenu);
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
        {

        }

		public void Run()
		{
		}
	}
}
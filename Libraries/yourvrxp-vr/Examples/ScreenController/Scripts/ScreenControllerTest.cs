using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
using UnityEngine;

namespace yourvrexperience.VR
{
	public class ScreenControllerTest : MonoBehaviour
	{
		public const string EventScreenControllerTestMainMenu = "EventScreenControllerTestMainMenu";
		public const string EventScreenControllerTestProfile = "EventScreenControllerTestProfile";
		public const string EventScreenControllerTestSettings = "EventScreenControllerTestSettings";
		public const string EventScreenControllerTestExit = "EventScreenControllerTestExit";

		public enum StatesApp { MainMenu, Profile, Settings }

		private StatesApp _state;
		private IInputController _inputController;
		
		void Start()
		{
			UIEventController.Instance.Event += OnUIEvent;
			SystemEventController.Instance.Event += OnSystemEvent;

			CameraXRController.Instance.Initialize();
		}

		void OnDestroy()
		{
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(InputController.EventInputControllerHasStarted))
			{
				_inputController = ((GameObject)parameters[0]).GetComponent<IInputController>();
				_inputController.Initialize();
				ChangeState(StatesApp.MainMenu);
			}
		}

		private void OnUIEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(EventScreenControllerTestMainMenu))
			{
				ChangeState(StatesApp.MainMenu);
			}
			if (nameEvent.Equals(EventScreenControllerTestProfile))
			{
				ChangeState(StatesApp.Profile);
			}
			if (nameEvent.Equals(EventScreenControllerTestSettings))
			{
				ChangeState(StatesApp.Settings);
			}						
			if (nameEvent.Equals(EventScreenControllerTestExit))
			{
				Debug.LogError("QUITTING APPLICATION");
				Application.Quit();
			}						
		}

		private void ChangeState(StatesApp state)
		{
			_state = state;
			switch (_state)
			{
				case StatesApp.MainMenu:
					ScreenController.Instance.CreateScreen(ScreenMainMenu.ScreenName, true, false);
					break;
				case StatesApp.Profile:
					ScreenController.Instance.CreateScreen(ScreenProfile.ScreenName, true, false);
					break;
				case StatesApp.Settings:
					ScreenController.Instance.CreateScreen(ScreenSettings.ScreenName, true, false);
					break;
			}
		}
	}
}
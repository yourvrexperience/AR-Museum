using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.Utils
{
	public class ScreenMainMenu : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenMainMenu";

		[SerializeField] private Button buttonLoadSession;
		[SerializeField] private Button buttonProfile;
		[SerializeField] private Button buttonSettings;
		[SerializeField] private Button buttonExit;

		[SerializeField] private TextMeshProUGUI titleScreen;
		[SerializeField] private TextMeshProUGUI descriptionScreen;
		[SerializeField] private TextMeshProUGUI textButtonLoadSession;
		[SerializeField] private TextMeshProUGUI textButtonProfile;
		[SerializeField] private TextMeshProUGUI textButtonSettings;
		[SerializeField] private TextMeshProUGUI textButtonExit;

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			buttonLoadSession.onClick.AddListener(OnButtonLoadSession);
			buttonProfile.onClick.AddListener(OnButtonProfile);
			buttonSettings.onClick.AddListener(OnButtonSettings);
			buttonExit.onClick.AddListener(OnButtonExit);

			titleScreen.text = LanguageController.Instance.GetText("screen.main.menu.title");
			textButtonLoadSession.text = LanguageController.Instance.GetText("screen.main.menu.load.session");
			descriptionScreen.text = LanguageController.Instance.GetText("screen.main.menu.description");
			textButtonProfile.text = LanguageController.Instance.GetText("screen.main.menu.profile");
			textButtonSettings.text = LanguageController.Instance.GetText("screen.main.menu.settings");
			textButtonExit.text = LanguageController.Instance.GetText("text.exit");
		}

		private void OnButtonLoadSession()
		{
			UIEventController.Instance.DispatchUIEvent(ScreenControllerApp.EventScreenControllerBasicLoadSession);
		}

		private void OnButtonExit()
		{
			UIEventController.Instance.DispatchUIEvent(ScreenControllerApp.EventScreenControllerBasicExit);
		}

		private void OnButtonSettings()
		{
			UIEventController.Instance.DispatchUIEvent(ScreenControllerApp.EventScreenControllerBasicSettings);
		}

		private void OnButtonProfile()
		{
			UIEventController.Instance.DispatchUIEvent(ScreenControllerApp.EventScreenControllerBasicProfile);
		}

		public override void Destroy()
		{
			base.Destroy();
		}
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.Utils
{
	public class ScreenPause : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenPause";

		[SerializeField] private Button buttonResume;
		[SerializeField] private Button buttonBuild;
		[SerializeField] private Button buttonExit;

		[SerializeField] private TextMeshProUGUI titleScreen;
		[SerializeField] private TextMeshProUGUI buildTitle;
		[SerializeField] private TextMeshProUGUI resumeTitle;
		[SerializeField] private TextMeshProUGUI exitTitle;

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			buttonResume.onClick.AddListener(OnButtonResume);
			buttonBuild.onClick.AddListener(OnButtonBuild);
			buttonExit.onClick.AddListener(OnButtonExit);

			titleScreen.text = LanguageController.Instance.GetText("screen.pause.title");			
			resumeTitle.text = LanguageController.Instance.GetText("screen.pause.resume");
			buildTitle.text = LanguageController.Instance.GetText("screen.pause.build.title");
			exitTitle.text = LanguageController.Instance.GetText("screen.pause.exit");
		}

		private void OnButtonBuild()
		{			
			UIEventController.Instance.DispatchUIEvent(ScreenControllerApp.EventScreenControllerBasicBuildInLevel);
		}

		private void OnButtonResume()
		{
			UIEventController.Instance.DispatchUIEvent(ScreenControllerApp.EventScreenControllerBasicResumeInLevel);
		}

		private void OnButtonExit()
		{
			UIEventController.Instance.DispatchUIEvent(ScreenControllerApp.EventScreenControllerBasicExitInLevel);
		}

		public override void Destroy()
		{
			base.Destroy();
		}
	}
}
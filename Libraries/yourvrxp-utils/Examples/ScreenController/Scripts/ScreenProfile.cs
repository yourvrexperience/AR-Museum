using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.Utils
{
	public class ScreenProfile : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenProfile";

		[SerializeField] private Button buttonBack;
		[SerializeField] private TextMeshProUGUI titleScreen;
		[SerializeField] private TextMeshProUGUI textButtonExit;

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			buttonBack.onClick.AddListener(OnButtonBack);

			titleScreen.text = LanguageController.Instance.GetText("screen.main.menu.profile");
			textButtonExit.text = LanguageController.Instance.GetText("text.back");
		}

		private void OnButtonBack()
		{
			UIEventController.Instance.DispatchUIEvent(ScreenControllerApp.EventScreenControllerBasicMainMenu);
		}

		public override void Destroy()
		{
			base.Destroy();
		}
	}
}
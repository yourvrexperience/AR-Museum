using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;

namespace yourvrexperience.template6dof
{
	public class ScreenSplashView : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenSplashView";

		[SerializeField] private TextMeshProUGUI titleScreen;

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			titleScreen.text = LanguageController.Instance.GetText("screen.main.menu.title");
		}
    }
}
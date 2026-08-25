using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.VR
{
	public class ScreenProfile : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenProfile";

		[SerializeField] private Button buttonBack;

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] _list)
		{
			base.Initialize(_list);

			buttonBack.onClick.AddListener(OnButtonBack);
		}

		private void OnButtonBack()
		{
			UIEventController.Instance.DispatchUIEvent(ScreenControllerTest.EventScreenControllerTestMainMenu);
		}

		public override void Destroy()
		{
			base.Destroy();
		}
	}
}
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;

namespace yourvrexperience.template6dof
{
	public class ScreenLoadingView : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenLoadingView";

		public const string EventScreenLoadingViewUpdateText = "EventScreenLoadingViewUpdateText";

		[SerializeField] private TextMeshProUGUI titleScreen;

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}

        public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			if ((parameters != null) && (parameters.Length > 0))
			{
				titleScreen.text = (string)parameters[0];
			}
			else
			{
				titleScreen.text = LanguageController.Instance.GetText("text.loading");
			}

			SystemEventController.Instance.Event += OnSystemEvent;
		}

		void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {			
            if (nameEvent.Equals(EventScreenLoadingViewUpdateText))
			{
				titleScreen.text = (string)parameters[0];
			}
        }
    }
}
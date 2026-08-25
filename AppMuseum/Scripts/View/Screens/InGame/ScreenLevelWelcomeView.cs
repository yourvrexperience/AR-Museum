using TMPro;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.template6dof
{
	public class ScreenLevelWelcomeView : BaseScreenView, IScreenView
	{
		public const string EventScreenLevelWelcomeViewUpdateText = "EventScreenLevelWelcomeViewUpdateText";
		public const string ScreenName = "ScreenLevelWelcomeView";

		[SerializeField] private TextMeshProUGUI titleScreen;

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			titleScreen.text = "";

			SystemEventController.Instance.Event += OnSystemEvent;
		}

        public override void Destroy()
		{
			base.Destroy();

			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;			
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(EventScreenLevelWelcomeViewUpdateText))
			{
				titleScreen.text = (string)parameters[0];
			}
        }
    }
}
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;

namespace yourvrexperience.template6dof
{
	public class ScreenReleaseResourcesView : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenReleaseResourcesView";

		public const string EventScreenReleaseResourcesViewStarted = "EventScreenReleaseResourcesViewStarted";

		[SerializeField] private TextMeshProUGUI titleScreen;

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			titleScreen.text = "";

			SystemEventController.Instance.DispatchSystemEvent(EventScreenReleaseResourcesViewStarted, this);
		}

		public void UpdateText(string text)
		{
			titleScreen.text = text;
		}
	}
}
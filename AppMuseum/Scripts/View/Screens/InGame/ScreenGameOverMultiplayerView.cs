using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;
using yourvrexperience.VR;
using System.Collections.Generic;


namespace yourvrexperience.template6dof
{
	public class ScreenGameOverMultiplayerView : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenGameOverMultiplayerView";

		[SerializeField] private TextMeshProUGUI title;
		[SerializeField] private TextMeshProUGUI description;

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			title.text = LanguageController.Instance.GetText("screen.level.game.over.title");
			description.text = LanguageController.Instance.GetText("screen.level.game.over.description");

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR	|| ENABLE_NREAL	
			RefocusScreen refocusComponent = this.gameObject.GetComponent<RefocusScreen>();
			if (refocusComponent == null)
			{
				refocusComponent = this.gameObject.AddComponent<RefocusScreen>();
			}
			refocusComponent.Activate(VRInputController.Instance.Camera, ScreenController.Instance.DistanceScreen, 1, 0.4f);
#endif			
		}
	}
}
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;

namespace yourvrexperience.template6dof
{
	public class ScreenFormMuseumCompletedView : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenFormMuseumCompletedView";

		[SerializeField] private TextMeshProUGUI title;
		[SerializeField] private TextMeshProUGUI descriptionBig;
		[SerializeField] private TextMeshProUGUI descriptionSmall;
		[SerializeField] private Button buttonNext;

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			title.text = LanguageController.Instance.GetText("screen.main.menu.title");

			descriptionBig.text = LanguageController.Instance.GetText("screen.form.completed.congratulations.thanks");
			descriptionSmall.text = LanguageController.Instance.GetText("screen.form.completed.congratulations.detailed");

			buttonNext.onClick.AddListener(OnButtonNext);
			buttonNext.transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.form.completed.repeat");
		}

		public override void Destroy()
		{
			base.Destroy();
		}

		private void OnButtonNext()
		{			
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
			UIEventController.Instance.DispatchUIEvent(GameStateRun.EventGameStateRunRestartExperience);
		}
	}
}
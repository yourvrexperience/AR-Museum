using yourvrexperience.Utils;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using yourvrexperience.Narration;

namespace yourvrexperience.template6dof
{
	public class PanelExitView : MonoBehaviour
	{
		[SerializeField] private Button buttonExit;
		[SerializeField] private TextMeshProUGUI titleScreen;

		void Start()
		{
			buttonExit.onClick.AddListener(OnExitButton);

			if (titleScreen != null) titleScreen.text = LanguageController.Instance.GetText("text.exit");
		}

		private void OnExitButton()
		{
			UIEventController.Instance.DispatchUIEvent(ScreenPauseView.EventScreenPauseViewExitGame);
		}
	}
}

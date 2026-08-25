using yourvrexperience.Utils;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using yourvrexperience.Narration;
using yourvrexperience.Networking;

namespace yourvrexperience.template6dof
{
	public class PanelManagementView : MonoBehaviour
	{
		[SerializeField] private Button buttonManagement;
		[SerializeField] private TextMeshProUGUI titleScreen;

		private bool _shouldShow;
		
		void Start()
		{
			_shouldShow = false;
			if (MainController.Instance.IsMultiplayer)
			{
				if (NetworkController.Instance.IsServer)
				{
					_shouldShow = true;
				}
			}
			this.gameObject.SetActive(_shouldShow);

			if (_shouldShow)
			{
				buttonManagement.onClick.AddListener(OnManagementButton);
				if (titleScreen != null) titleScreen.text = LanguageController.Instance.GetText("text.management");
			}

			if (MainController.Instance.EnableEditionPOIs)
			{
				this.gameObject.SetActive(false);
			}
		}

		private void OnManagementButton()
		{
			SystemEventController.Instance.DispatchSystemEvent(ScreenPauseView.EventScreenPauseViewManageNetwork);
		}
	}
}

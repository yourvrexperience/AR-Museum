using yourvrexperience.Utils;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using yourvrexperience.Narration;

namespace yourvrexperience.template6dof
{
	public class PanelSubtitlesView : MonoBehaviour
	{
		[SerializeField] private Toggle toggleSubtitles;
		[SerializeField] private TextMeshProUGUI titleScreen;

		void Start()
		{
			toggleSubtitles.isOn = GameLevelData.Instance.SubtitlesActivated;

			toggleSubtitles.onValueChanged.AddListener(OnSubtitlesActivation);

			titleScreen.text = LanguageController.Instance.GetText("word.subtitles");

			SystemEventController.Instance.Event += OnSystemEvent;

			if (MainController.Instance.EnableEditionPOIs)
			{
				this.gameObject.SetActive(false);
			}			
		}

		void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		private void OnSubtitlesActivation(bool value)
		{
			GameLevelData.Instance.SubtitlesActivated = !GameLevelData.Instance.SubtitlesActivated;
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(MainController.EventMainControllerReleaseGameResources))
			{
				GameObject.Destroy(this.gameObject);
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerUpdateTexts))
			{
				titleScreen.text = LanguageController.Instance.GetText("word.subtitles");
			}
		}
	}
}

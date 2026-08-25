using yourvrexperience.Utils;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using yourvrexperience.Narration;

namespace yourvrexperience.template6dof
{
	public class PanelLanguageView : MonoBehaviour
	{
		[SerializeField] private ToggleGroup toggleLanguages;
		[SerializeField] private Toggle toggleEnglish;
		[SerializeField] private Toggle toggleSpanish;
		[SerializeField] private Toggle toggleCatalan;
		[SerializeField] private Toggle toggleFrench;
		[SerializeField] private TextMeshProUGUI titleScreen;

		private bool _reportEvent = true;

		void Start()
		{
			toggleEnglish.onValueChanged.AddListener(OnLanguageEnglish);
			toggleSpanish.onValueChanged.AddListener(OnLanguageSpanish);
			toggleCatalan.onValueChanged.AddListener(OnLanguageCatalan);
			toggleFrench.onValueChanged.AddListener(OnLanguageFrench);
			
			if (titleScreen != null) titleScreen.text = LanguageController.Instance.GetText("word.language");

			SystemEventController.Instance.Event += OnSystemEvent;

			_reportEvent = false;
			if (LanguageController.Instance.CodeLanguage.Equals(LanguageController.CodeLanguageEnglish))
            {
				toggleEnglish.isOn = true;
			}
			if (LanguageController.Instance.CodeLanguage.Equals(LanguageController.CodeLanguageSpanish))
			{
				toggleSpanish.isOn = true;
			}
			if (LanguageController.Instance.CodeLanguage.Equals(LanguageController.CodeLanguageCatalan))
			{
				toggleCatalan.isOn = true;
			}
			_reportEvent = true;

			UpdateTexts();
		}

        void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

        private void OnLanguageFrench(bool value)
        {
			ResetColorToBlack();
			toggleFrench.transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
			LanguageController.Instance.ChangeLanguage(LanguageController.CodeLanguageFrench);
			if (_reportEvent && value) SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerUpdateTexts);
        }

        private void OnLanguageCatalan(bool value)
        {
			ResetColorToBlack();
			toggleCatalan.transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
			LanguageController.Instance.ChangeLanguage(LanguageController.CodeLanguageCatalan);
			if (_reportEvent && value) SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerUpdateTexts);
        }

        private void OnLanguageSpanish(bool value)
        {
			ResetColorToBlack();
			toggleSpanish.transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
			LanguageController.Instance.ChangeLanguage(LanguageController.CodeLanguageSpanish);
			if (_reportEvent && value) SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerUpdateTexts);
        }

        private void OnLanguageEnglish(bool value)
        {
			ResetColorToBlack();
			toggleEnglish.transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
			LanguageController.Instance.ChangeLanguage(LanguageController.CodeLanguageEnglish);
			if (_reportEvent && value) SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerUpdateTexts);
        }

		private void UpdateTexts()
		{
			if (titleScreen != null) titleScreen.text = LanguageController.Instance.GetText("word.language");
			toggleEnglish.transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.english");
			toggleSpanish.transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.spanish");
			toggleCatalan.transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.catalan");
			toggleFrench.transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.french");
		}

		private void ResetColorToBlack()
		{
			toggleEnglish.transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.black;
			toggleSpanish.transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.black;
			toggleCatalan.transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.black;
			toggleFrench.transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.black;
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(MainController.EventMainControllerReleaseGameResources))
			{
				GameObject.Destroy(this.gameObject);
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerUpdateTexts))
			{
				UpdateTexts();
			}
        }
    }
}

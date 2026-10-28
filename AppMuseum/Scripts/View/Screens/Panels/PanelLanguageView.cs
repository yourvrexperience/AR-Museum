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
		[SerializeField] private Toggle[] toggles;
		[SerializeField] private TextMeshProUGUI titleScreen;

		private bool _reportEvent = true;
		private string [] _languagesSupported;

		void Start()
		{
			for (int i = 0; i < toggles.Length; i++)
			{
				toggles[i].gameObject.SetActive(false);
			}
			_languagesSupported = new string[LanguageController.Instance.SupportedLanguages.Length];
			_reportEvent = false;
			for (int i = 0; i < LanguageController.Instance.SupportedLanguages.Length; i++)
			{
				string languageSupported = LanguageController.Instance.SupportedLanguages[i];
				_languagesSupported[i] = languageSupported;				
				toggles[i].gameObject.SetActive(true);
				toggles[i].isOn = false;
				switch (languageSupported)
				{
					case LanguageController.CodeLanguageEnglish:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.english");
						toggles[i].onValueChanged.AddListener(OnLanguageEnglish);
						if (LanguageController.Instance.CodeLanguage.Equals(LanguageController.CodeLanguageEnglish)) toggles[i].isOn = true;
						break;
        			case LanguageController.CodeLanguageSpanish:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.spanish");
						toggles[i].onValueChanged.AddListener(OnLanguageSpanish);
						if (LanguageController.Instance.CodeLanguage.Equals(LanguageController.CodeLanguageSpanish)) toggles[i].isOn = true;
						break;
					case LanguageController.CodeLanguageCatalan:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.catalan");
						toggles[i].onValueChanged.AddListener(OnLanguageCatalan);
						if (LanguageController.Instance.CodeLanguage.Equals(LanguageController.CodeLanguageCatalan)) toggles[i].isOn = true;
						break;
					case LanguageController.CodeLanguageGerman:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.german");
						toggles[i].onValueChanged.AddListener(OnLanguageGerman);
						if (LanguageController.Instance.CodeLanguage.Equals(LanguageController.CodeLanguageGerman)) toggles[i].isOn = true;
						break;
					case LanguageController.CodeLanguageFrench:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.french");
						toggles[i].onValueChanged.AddListener(OnLanguageFrench);
						if (LanguageController.Instance.CodeLanguage.Equals(LanguageController.CodeLanguageFrench)) toggles[i].isOn = true;
						break;
					case LanguageController.CodeLanguageItalian:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.italian");
						toggles[i].onValueChanged.AddListener(OnLanguageItalian);
						if (LanguageController.Instance.CodeLanguage.Equals(LanguageController.CodeLanguageItalian)) toggles[i].isOn = true;
						break;
					case LanguageController.CodeLanguageRussian:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.russian");
						toggles[i].onValueChanged.AddListener(OnLanguageRussian);
						if (LanguageController.Instance.CodeLanguage.Equals(LanguageController.CodeLanguageRussian)) toggles[i].isOn = true;
						break;
					case LanguageController.CodeLanguageChinese:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.chinese");
						toggles[i].onValueChanged.AddListener(OnLanguageChinese);
						if (LanguageController.Instance.CodeLanguage.Equals(LanguageController.CodeLanguageChinese)) toggles[i].isOn = true;
						break;
					case LanguageController.CodeLanguageJapanese:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.japanese");
						toggles[i].onValueChanged.AddListener(OnLanguageJapanese);
						if (LanguageController.Instance.CodeLanguage.Equals(LanguageController.CodeLanguageJapanese)) toggles[i].isOn = true;
						break;
					case LanguageController.CodeLanguagePortuguese:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.portuguese");
						toggles[i].onValueChanged.AddListener(OnLanguagePortuguese);
						if (LanguageController.Instance.CodeLanguage.Equals(LanguageController.CodeLanguagePortuguese)) toggles[i].isOn = true;
						break;
					case LanguageController.CodeLanguageArabic:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.arabic");
						toggles[i].onValueChanged.AddListener(OnLanguageArabic);
						if (LanguageController.Instance.CodeLanguage.Equals(LanguageController.CodeLanguageArabic)) toggles[i].isOn = true;
						break;
					case LanguageController.CodeLanguageGreek:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.greek");
						toggles[i].onValueChanged.AddListener(OnLanguageGreek);
						if (LanguageController.Instance.CodeLanguage.Equals(LanguageController.CodeLanguageGreek)) toggles[i].isOn = true;
						break;
					case LanguageController.CodeLanguageUrdu:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.urdu");
						toggles[i].onValueChanged.AddListener(OnLanguageUrdu);
						if (LanguageController.Instance.CodeLanguage.Equals(LanguageController.CodeLanguageUrdu)) toggles[i].isOn = true;
						break;
					case LanguageController.CodeLanguageKorean:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.korean");
						toggles[i].onValueChanged.AddListener(OnLanguageKorean);
						if (LanguageController.Instance.CodeLanguage.Equals(LanguageController.CodeLanguageKorean)) toggles[i].isOn = true;
						break;
				}
			}

			if (titleScreen != null) titleScreen.text = LanguageController.Instance.GetText("word.language");

			SystemEventController.Instance.Event += OnSystemEvent;
			_reportEvent = true;

			UpdateTexts();
		}

        void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		private Toggle GetToggleLanguage(string languageCode)
		{
			for (int i = 0; i < _languagesSupported.Length; i++)
			{
				if (_languagesSupported[i] == languageCode)
				{
					return toggles[i];
				}
			}
			return null;
		}

        private void OnLanguageChinese(bool value)
        {
			ResetColorToBlack();
			GetToggleLanguage(LanguageController.CodeLanguageChinese).transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
			LanguageController.Instance.ChangeLanguage(LanguageController.CodeLanguageChinese);
			if (_reportEvent && value) SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerUpdateTexts);
        }

        private void OnLanguageGerman(bool value)
        {
			ResetColorToBlack();
			GetToggleLanguage(LanguageController.CodeLanguageGerman).transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
			LanguageController.Instance.ChangeLanguage(LanguageController.CodeLanguageGerman);
			if (_reportEvent && value) SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerUpdateTexts);
        }

        private void OnLanguageFrench(bool value)
        {
			ResetColorToBlack();
			GetToggleLanguage(LanguageController.CodeLanguageFrench).transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
			LanguageController.Instance.ChangeLanguage(LanguageController.CodeLanguageFrench);
			if (_reportEvent && value) SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerUpdateTexts);
        }

        private void OnLanguageCatalan(bool value)
        {
			ResetColorToBlack();
			GetToggleLanguage(LanguageController.CodeLanguageCatalan).transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
			LanguageController.Instance.ChangeLanguage(LanguageController.CodeLanguageCatalan);
			if (_reportEvent && value) SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerUpdateTexts);
        }

        private void OnLanguageSpanish(bool value)
        {
			ResetColorToBlack();
			GetToggleLanguage(LanguageController.CodeLanguageSpanish).transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
			LanguageController.Instance.ChangeLanguage(LanguageController.CodeLanguageSpanish);
			if (_reportEvent && value) SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerUpdateTexts);
        }

        private void OnLanguageEnglish(bool value)
        {
			ResetColorToBlack();
			GetToggleLanguage(LanguageController.CodeLanguageEnglish).transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
			LanguageController.Instance.ChangeLanguage(LanguageController.CodeLanguageEnglish);
			if (_reportEvent && value) SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerUpdateTexts);
        }

        private void OnLanguageItalian(bool value)
        {
			ResetColorToBlack();
			GetToggleLanguage(LanguageController.CodeLanguageItalian).transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
			LanguageController.Instance.ChangeLanguage(LanguageController.CodeLanguageItalian);
			if (_reportEvent && value) SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerUpdateTexts);
        }

        private void OnLanguageRussian(bool value)
        {
			ResetColorToBlack();
			GetToggleLanguage(LanguageController.CodeLanguageRussian).transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
			LanguageController.Instance.ChangeLanguage(LanguageController.CodeLanguageRussian);
			if (_reportEvent && value) SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerUpdateTexts);
        }

        private void OnLanguageJapanese(bool value)
        {
			ResetColorToBlack();
			GetToggleLanguage(LanguageController.CodeLanguageJapanese).transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
			LanguageController.Instance.ChangeLanguage(LanguageController.CodeLanguageJapanese);
			if (_reportEvent && value) SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerUpdateTexts);
        }

        private void OnLanguagePortuguese(bool value)
        {
			ResetColorToBlack();
			GetToggleLanguage(LanguageController.CodeLanguagePortuguese).transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
			LanguageController.Instance.ChangeLanguage(LanguageController.CodeLanguagePortuguese);
			if (_reportEvent && value) SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerUpdateTexts);
        }

        private void OnLanguageGreek(bool value)
        {
			ResetColorToBlack();
			GetToggleLanguage(LanguageController.CodeLanguageGreek).transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
			LanguageController.Instance.ChangeLanguage(LanguageController.CodeLanguageGreek);
			if (_reportEvent && value) SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerUpdateTexts);
        }

        private void OnLanguageArabic(bool value)
        {
			ResetColorToBlack();
			GetToggleLanguage(LanguageController.CodeLanguageArabic).transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
			LanguageController.Instance.ChangeLanguage(LanguageController.CodeLanguageArabic);
			if (_reportEvent && value) SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerUpdateTexts);
        }

        private void OnLanguageUrdu(bool value)
        {
			ResetColorToBlack();
			GetToggleLanguage(LanguageController.CodeLanguageUrdu).transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
			LanguageController.Instance.ChangeLanguage(LanguageController.CodeLanguageUrdu);
			if (_reportEvent && value) SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerUpdateTexts);
        }

        private void OnLanguageKorean(bool value)
        {
			ResetColorToBlack();
			GetToggleLanguage(LanguageController.CodeLanguageKorean).transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
			LanguageController.Instance.ChangeLanguage(LanguageController.CodeLanguageKorean);
			if (_reportEvent && value) SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerUpdateTexts);
        }

		private void UpdateTexts()
		{
			if (titleScreen != null) titleScreen.text = LanguageController.Instance.GetText("word.language");
			for (int i = 0; i < _languagesSupported.Length; i++)
			{
				string languageSupported = _languagesSupported[i];
				_languagesSupported[i] = languageSupported;				
				switch (languageSupported)
				{
					case LanguageController.CodeLanguageEnglish:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.english");
						break;
        			case LanguageController.CodeLanguageSpanish:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.spanish");
						break;
					case LanguageController.CodeLanguageCatalan:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.catalan");
						break;
					case LanguageController.CodeLanguageGerman:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.german");
						break;
					case LanguageController.CodeLanguageFrench:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.french");
						break;
					case LanguageController.CodeLanguageItalian:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.italian");
						break;
					case LanguageController.CodeLanguageRussian:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.russian");
						break;
					case LanguageController.CodeLanguageChinese:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.chinese");
						break;
					case LanguageController.CodeLanguageJapanese:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.japanese");
						break;
					case LanguageController.CodeLanguagePortuguese:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.portuguese");
						break;
					case LanguageController.CodeLanguageArabic:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.arabic");
						break;
					case LanguageController.CodeLanguageGreek:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.greek");
						break;
					case LanguageController.CodeLanguageUrdu:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.urdu");
						break;
					case LanguageController.CodeLanguageKorean:
						toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("language.korean");
						break;
				}
			}
		}

		private void ResetColorToBlack()
		{
			for (int i = 0; i < toggles.Length; i++)
			{
				toggles[i].transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.black;
			}
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

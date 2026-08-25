using yourvrexperience.Utils;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using yourvrexperience.Narration;
using static yourvrexperience.Narration.GameLevelData;

namespace yourvrexperience.template6dof
{
	public class PanelAgeView : MonoBehaviour
	{
		public const string EventPanelAgeViewChanged = "EventPanelAgeViewChanged";

		[SerializeField] private ToggleGroup toggleAges;
		[SerializeField] private Toggle toggleKids;
		[SerializeField] private Toggle toggleAdults;
		[SerializeField] private Toggle toggleExperts;
		[SerializeField] private TextMeshProUGUI titleScreen;

		void Start()
		{
			if (toggleKids != null) toggleKids.onValueChanged.AddListener(OnKidsSelected);
			if (toggleAdults != null) toggleAdults.onValueChanged.AddListener(OnAdultsSelected);
			if (toggleExperts != null) toggleExperts.onValueChanged.AddListener(OnExpertsSelected);

			SystemEventController.Instance.Event += OnSystemEvent;

			if (GameLevelData.Instance.Age == GameAge.Kids)
            {
				if (toggleKids != null) toggleKids.isOn = true;
			}
			if (GameLevelData.Instance.Age == GameAge.Adults)
            {
				if (toggleAdults != null) toggleAdults.isOn = true;
			}
			if (GameLevelData.Instance.Age == GameAge.Experts)
            {
				if (toggleExperts != null) toggleExperts.isOn = true;
			}

			UpdateTexts();

#if ENABLE_ONE_AGE
			this.gameObject.SetActive(false);
#endif
		}

		void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

        private void OnKidsSelected(bool value)
        {
			GameLevelData.Instance.Age = GameAge.Kids;
			UpdateTexts();
			UIEventController.Instance.DispatchUIEvent(EventPanelAgeViewChanged);
        }

        private void OnAdultsSelected(bool value)
        {
			GameLevelData.Instance.Age = GameAge.Adults;
			UpdateTexts();
			UIEventController.Instance.DispatchUIEvent(EventPanelAgeViewChanged);
        }

        private void OnExpertsSelected(bool value)
        {
			GameLevelData.Instance.Age = GameAge.Experts;
			UpdateTexts();
			UIEventController.Instance.DispatchUIEvent(EventPanelAgeViewChanged);
        }

		private void UpdateTexts()
		{
			if (titleScreen != null)
			{
				switch (GameLevelData.Instance.Age)
				{
					case GameAge.Kids:
						titleScreen.text = LanguageController.Instance.GetText("word.age.kids");
						break;

					case GameAge.Adults:
						titleScreen.text = LanguageController.Instance.GetText("word.age.adults");
						break;		

					case GameAge.Experts:
						titleScreen.text = LanguageController.Instance.GetText("word.age.experts");
						break;
				}				
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

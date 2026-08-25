using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Narration;
using yourvrexperience.UserManagement;
using yourvrexperience.Utils;

namespace yourvrexperience.template6dof
{
	public class ScreenMainMenuView : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenMainMenuView";

		public const string EventScreenMainMenuViewPlayGame = "EventScreenMainMenuViewPlayGame";
		public const string EventScreenMainMenuViewSettings = "EventScreenMainMenuViewSettings";
		public const string EventScreenMainMenuViewExitGame = "EventScreenMainMenuViewExitGame";		

		[SerializeField] private TextMeshProUGUI appVersion;
		[SerializeField] private TextMeshProUGUI titleScreen;
		[SerializeField] private TextMeshProUGUI descriptionScreen;
		[SerializeField] private Button buttonPlayMultiPlayer;
		[SerializeField] private Button buttonEditPOIs;
		[SerializeField] private Button buttonExit;
		[SerializeField] private Button buttonSettings;

		[SerializeField] private Toggle toggleMultiplayer;
		[SerializeField] private Toggle toggleEditionNarration;

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			appVersion.text = "v" + Application.version;

			buttonPlayMultiPlayer.onClick.AddListener(OnButtonPlayMultiPlayer);
			buttonEditPOIs.onClick.AddListener(OnButtonEditPOIs);
			buttonExit.onClick.AddListener(OnButtonExit);
			buttonSettings.onClick.AddListener(OnButtonSettings);

			UpdateLocalTexts();

			SystemEventController.Instance.Event += OnSystemEvent;

			toggleMultiplayer.onValueChanged.AddListener(OnMultiplayerEnabled);

			if ((UsersController.Instance.CurrentUser != null) && (UsersController.Instance.CurrentUser.Admin))
			{
				toggleEditionNarration.isOn = GameLevelData.Instance.GetDeveloperMode();
				toggleEditionNarration.onValueChanged.AddListener(OnEditionNarration);
				buttonEditPOIs.gameObject.SetActive(GameLevelData.Instance.GetDeveloperMode());
			}
			else
			{
				toggleEditionNarration.gameObject.SetActive(false);
				buttonEditPOIs.gameObject.SetActive(false);
			}

			toggleMultiplayer.isOn = false;
#if UNITY_WEBGL
			toggleMultiplayer.gameObject.SetActive(false);
			buttonExit.gameObject.SetActive(false);
#endif
		}

        private void UpdateLocalTexts()
		{
			titleScreen.text = LanguageController.Instance.GetText("screen.main.menu.title");
			descriptionScreen.text = LanguageController.Instance.GetText("screen.main.menu.description");
			toggleMultiplayer.transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.main.menu.toggle.multiplayer." + MainController.Instance.IsMultiplayer);
			if (GameLevelData.Instance.GetDeveloperMode())
			{
				toggleEditionNarration.transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.main.menu.toggle.edition.narration.development");
			}			
			else
			{
				toggleEditionNarration.transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.main.menu.toggle.edition.narration.production");
			}
		}

        public override void Destroy()
		{
			base.Destroy();

			SystemEventController.Instance.Event -= OnSystemEvent;
		}

        private void OnButtonExit()
        {
			SoundsController.Instance.PlaySoundFX(GameSounds.FxSelection, false, 1);				
			UIEventController.Instance.DispatchUIEvent(EventScreenMainMenuViewExitGame, this.gameObject);            
        }

        private void OnButtonPlayMultiPlayer()
		{
			MainController.Instance.EnableEditionPOIs = false;
			SoundsController.Instance.PlaySoundFX(GameSounds.FxSelection, false, 1);				
			UIEventController.Instance.DispatchUIEvent(EventScreenMainMenuViewPlayGame, this.gameObject);
#if ENABLE_ONE_FLOOR
			UIEventController.Instance.DispatchUIEvent(ScreenFloorView.EventScreenFloorSelected, 2);
#endif
		}

        private void OnButtonEditPOIs()
        {
			MainController.Instance.EnableEditionPOIs = true;
			MainController.Instance.IsMultiplayer = false;
			SoundsController.Instance.PlaySoundFX(GameSounds.FxSelection, false, 1);
			UIEventController.Instance.DispatchUIEvent(EventScreenMainMenuViewPlayGame, this.gameObject);
#if ENABLE_ONE_FLOOR
			UIEventController.Instance.DispatchUIEvent(ScreenFloorView.EventScreenFloorSelected, 2);
#endif
        }

        private void OnMultiplayerEnabled(bool enabled)
        {
			MainController.Instance.IsMultiplayer = enabled;
			UpdateLocalTexts();
        }

        private void OnEditionNarration(bool enabled)
        {
			GameLevelData.Instance.SetDeveloperMode(enabled);
			string titleWarning = LanguageController.Instance.GetText("text.warning");
			string textInformAboutExit = LanguageController.Instance.GetText("screen.main.inform.about.exit.for.developer");
			ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleWarning, textInformAboutExit, GameStateMenu.SubEventExitAppConfirmation);
        }

        private void OnButtonSettings()
        {
			SoundsController.Instance.PlaySoundFX(GameSounds.FxSelection, false, 1);				
			UIEventController.Instance.DispatchUIEvent(EventScreenMainMenuViewSettings);
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(LanguageController.EventLanguageControllerChangedCodeLanguage))
			{
				UpdateLocalTexts();
			}
        }

		void Update()
		{
			if (Input.GetKeyDown(KeyCode.P))
			{
				OnButtonPlayMultiPlayer();
			}
		}
	}
}
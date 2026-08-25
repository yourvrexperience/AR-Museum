using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.UserManagement;
using yourvrexperience.Utils;
using yourvrexperience.Narration;
#if ENABLE_GOOGLE || ENABLE_FACEBOOK || ENABLE_GOOGLE_SIGNIN
using yourvrexperience.Social;
#endif

namespace yourvrexperience.template6dof
{
	public class ScreenSettingsView : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenSettingsView";

		public const string EVENT_REGISTER_PLATFORM_MESSAGE_DELAYED_REPORT_NEW_USER = "EVENT_REGISTER_PLATFORM_MESSAGE_DELAYED_REPORT_NEW_USER";

		public const string EventRegisterPlatformFinallyLogin = "EventRegisterPlatformFinallyLogin";

		public const string EventScreenSettingsViewBack = "EventScreenSettingsViewBack";

		public const string SubEventRemoveConfirmationResponse = "SubEventRemoveConfirmationResponse";
		public const string SubEventLogoutConfirmationResponse = "SubEventLogoutConfirmationResponse";		
		public const string SubEventShowOptionsAfterRemoveAccount = "SubEventShowOptionsAfterRemoveAccount";
		public const string SubEventRemoveInfoGoogleError = "SubEventRemoveInfoGoogleError";
		public const string SubEventRegisterForPlatformConfirmation = "SubEventRegisterForPlatformConfirmation";

		private enum ConfigSettings { Loading, Options, LoggedIn }

		[SerializeField] private TextMeshProUGUI titleScreen;
		[SerializeField] private GameObject contentLoading;
		[SerializeField] private GameObject contentOptions;
		[SerializeField] private GameObject contentLoggedIn;
		[SerializeField] private Button buttonBack;

		[SerializeField] private TextMeshProUGUI loadingTitle;

		[SerializeField] private Button buttonRegisterMail;
		[SerializeField] private Button buttonRegisterGoogle;
		[SerializeField] private Button buttonRegisterApple;

		[SerializeField] private TextMeshProUGUI emailValue;
		[SerializeField] private TextMeshProUGUI progressDescription1;
		[SerializeField] private TextMeshProUGUI progressDescription2;
		[SerializeField] private TextMeshProUGUI progressDescription3;
		[SerializeField] private TextMeshProUGUI progressNumber1;
		[SerializeField] private TextMeshProUGUI progressNumber2;
		[SerializeField] private TextMeshProUGUI progressNumber3;
		[SerializeField] private Image progressBar1;
		[SerializeField] private Image progressBar2;
		[SerializeField] private Image progressBar3;
		[SerializeField] private Button buttonLogout;
		[SerializeField] private Button buttonRemove;
		[SerializeField] private Button buttonAISelector;

		private ConfigSettings _config;
		private int _iterationsRequest = 0;
		private string _emailToCheck = "";
		private string _passwordToCheck = "";

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			titleScreen.text = LanguageController.Instance.GetText("screen.settings.title");

			buttonRegisterMail.onClick.AddListener(OnRegisterMail);
			buttonRegisterMail.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.settings.register.email.option"); 
			buttonRegisterGoogle.onClick.AddListener(OnRegisterGoogle);
			buttonRegisterGoogle.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.settings.register.google.option"); 
			buttonRegisterApple.onClick.AddListener(OnRegisterApple);
			buttonRegisterApple.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.settings.register.apple.option"); 

			buttonRegisterGoogle.gameObject.SetActive(false);
			buttonRegisterApple.gameObject.SetActive(false);

#if ENABLE_GOOGLE_SIGNIN && !UNITY_WEBGL
			buttonRegisterGoogle.gameObject.SetActive(true);
#endif
#if ENABLE_APPLE
			buttonRegisterApple.gameObject.SetActive(true);
#endif

			loadingTitle.text = LanguageController.Instance.GetText("screen.settings.loading.info");

			buttonLogout.onClick.AddListener(OnLogout);
			buttonLogout.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.settings.loggedin.logout"); 
			buttonRemove.onClick.AddListener(OnRemoveAccount);
			buttonRemove.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.settings.loggedin.remove.account"); 

			buttonBack.onClick.AddListener(OnButtonBack);

#if !ENABLE_AI_OPERATIONS
			buttonAISelector.gameObject.SetActive(false);
#else			
			buttonAISelector.onClick.AddListener(OnAISelector);
			buttonAISelector.gameObject.SetActive(false);
#endif
			SystemEventController.Instance.Event += OnSystemEvent;
			UIEventController.Instance.Event += OnUIEvent;

			ChangeConfiguration(ConfigSettings.Loading);
			if (!LogIn())
			{
				ChangeConfiguration(ConfigSettings.Options);
			}
		}

        public override void Destroy()
		{
			base.Destroy();

			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}

		private bool LogIn()
		{
			if (UsersController.Instance.CurrentUser == null)
			{
				return false;
			}
			else
			{
				if ((UsersController.Instance.CurrentUser.Email.Length > 0) && (UsersController.Instance.CurrentUser.Password.Length > 0))
				{
					UserModel.LoginWithStoredLogin();
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		private void ChangeConfiguration(ConfigSettings newConfig)
		{
			_config = newConfig;

			switch (_config)
			{
				case ConfigSettings.Loading:
					contentLoading.SetActive(true);
					contentOptions.SetActive(false);
					contentLoggedIn.SetActive(false);
					break;

				case ConfigSettings.Options:
					contentLoading.SetActive(false);
					contentOptions.SetActive(true);
					contentLoggedIn.SetActive(false);
					break;					

				case ConfigSettings.LoggedIn:
					contentLoading.SetActive(false);
					contentOptions.SetActive(false);
					contentLoggedIn.SetActive(true);

					emailValue.text = UsersController.Instance.CurrentUser.Email;

					progressDescription1.text = LanguageController.Instance.GetText("screen.pause.stairs.1");
					progressDescription2.text = LanguageController.Instance.GetText("screen.pause.stairs.2");
					progressDescription3.text = LanguageController.Instance.GetText("screen.pause.stairs.3");

					RefreshProgressBecauseAgeChanged();

					UIEventController.Instance.DelayUIEvent(UsersController.EVENT_USER_UPDATE_PROFILE_DATA_REQUEST, 1, GameLevelData.Instance.PackMapDataContent());
					break;					
			}
		}

		private void RefreshProgressBecauseAgeChanged()
		{
			float progress1 = GameLevelData.Instance.GetTotalProgress(0);
			float progress2 = GameLevelData.Instance.GetTotalProgress(1);
			float progress3 = GameLevelData.Instance.GetTotalProgress(2);
			
			progressNumber1.text = (int)progress1 + "%";
			progressNumber2.text = (int)progress2 + "%";
			progressNumber3.text = (int)progress3 + "%";

			progressBar1.fillAmount = ((float)progress1/100);
			progressBar2.fillAmount = ((float)progress2/100);
			progressBar3.fillAmount = ((float)progress3/100);

#if ENABLE_ONE_FLOOR
			progressBar1.transform.parent.gameObject.SetActive(false);
			progressBar2.transform.parent.gameObject.SetActive(false);
			progressDescription1.gameObject.SetActive(false);
			progressDescription2.gameObject.SetActive(false);
#endif			
		}

        private void OnRegisterGoogle()
        {
#if ENABLE_GOOGLE_SIGNIN
			_iterationsRequest = 0;
            ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, this.gameObject, LanguageController.Instance.GetText("message.info"), LanguageController.Instance.GetText("message.please.wait"));
			GoogleAuth.Instance.Initialize();
			GoogleAuth.Instance.OnSignedIn += OnGoogleSignedInSuccess;
			GoogleAuth.Instance.OnSignInError += OnGoogleSignedInError;
			GoogleAuth.Instance.OnSignedOut += OnGoogleSignedOut;
			GoogleAuth.Instance.SignIn();
#endif
        }

#if ENABLE_GOOGLE_SIGNIN
        private void OnGoogleSignedInSuccess(string value)
        {
			string passwordGoogle = yourvrexperience.Utils.Utilities.RandomCodeGeneration(6);
			_iterationsRequest = 0;
			UIEventController.Instance.DispatchUIEvent(UsersController.EVENT_USER_REGISTER_REQUEST, GoogleAuth.Instance.Profile.email, passwordGoogle, LoginPlatforms.Email, "GOOGLE");
        }

        private void OnGoogleSignedInError(string value)
        {
			string titleInfoError = LanguageController.Instance.GetText("text.error");
			string descriptionInfoError = LanguageController.Instance.GetText("screen.register.login.google.error");
			UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
			ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleInfoError, descriptionInfoError, SubEventRemoveInfoGoogleError);
        }

        private void OnGoogleSignedOut()
        {
            
        }
#endif
        private void OnRegisterApple()
		{
#if ENABLE_APPLE
			UsersController.Instance.LoginRequested = false;
            ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, this.gameObject, LanguageController.Instance.GetText("message.info"), LanguageController.Instance.GetText("message.please.wait"));
            AppleController.Instance.Initialitzation();
#endif
		}

        private void OnRegisterMail()
        {
			_iterationsRequest = 0;
			ScreenController.Instance.CreateScreen(ScreenLoginUserView.ScreenName, false, true);
        }

        private void OnAISelector()
        {
			ScreenController.Instance.CreateScreen(ScreenAISelectorView.ScreenName, false, true);
        }

        private void OnRemoveAccount()
        {
			string titleRemoveConfirmation = LanguageController.Instance.GetText("message.warning");
			string descriptionRemoveConfirmation = LanguageController.Instance.GetText("screen.settings.loggedin.question.confirmation.delete.account");
			ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenConfirmation, this.gameObject, titleRemoveConfirmation, descriptionRemoveConfirmation, SubEventRemoveConfirmationResponse);
        }

        private void OnLogout()
        {
			string titleRemoveConfirmation = LanguageController.Instance.GetText("message.warning");
			string descriptionRemoveConfirmation = LanguageController.Instance.GetText("screen.settings.loggedin.question.confirmation.logout.account");
			ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenConfirmation, this.gameObject, titleRemoveConfirmation, descriptionRemoveConfirmation, SubEventLogoutConfirmationResponse);
        }

        private void OnButtonBack()
        {
			SoundsController.Instance.PlaySoundFX(GameSounds.FxSelection, false, 1);
			UIEventController.Instance.DispatchUIEvent(EventScreenSettingsViewBack);	
        }

        private void OnUIEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(PanelAgeView.EventPanelAgeViewChanged))
			{
				RefreshProgressBecauseAgeChanged();
			}
            if (nameEvent.Equals(SubEventShowOptionsAfterRemoveAccount))
			{
				ChangeConfiguration(ConfigSettings.Options);
			}
			if (nameEvent.Equals(SubEventRemoveConfirmationResponse))
			{
				if ((ScreenInformationResponses)parameters[1] == ScreenInformationResponses.Confirm)
				{
					string titleWait = LanguageController.Instance.GetText("screen.wait.register.title");
					string descriptionWait = LanguageController.Instance.GetText("screen.settings.loggedin.removing.account");
					ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, this.gameObject, titleWait, descriptionWait);
					UIEventController.Instance.DelayUIEvent(UsersController.EVENT_USER_REMOVE_SINGLE_RECORD, 0.2f, UsersController.Instance.CurrentUser.Id);
				}
			}
			if (nameEvent.Equals(SubEventLogoutConfirmationResponse))
			{
				if ((ScreenInformationResponses)parameters[1] == ScreenInformationResponses.Confirm)
				{
					UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
					SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_RESET_LOCAL_DATA);
					ChangeConfiguration(ConfigSettings.Options);
				}
			}
 			if (nameEvent.Equals(SubEventRegisterForPlatformConfirmation))
            {
                UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
                ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, this.gameObject, LanguageController.Instance.GetText("message.info"), LanguageController.Instance.GetText("message.please.wait"));
                SystemEventController.Instance.DelaySystemEvent(EventRegisterPlatformFinallyLogin, 2);
            }			
#if ENABLE_GOOGLE
            if (nameEvent == GoogleController.EVENT_GOOGLE_CONTROLLER_AUTHENTICATED)
            {
                if (!(bool)parameters[0])
                {
                    string titleInfoError = LanguageController.Instance.GetText("message.error");
                    string descriptionInfoError = LanguageController.Instance.GetText("screen.register.login.google.error");
                    UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleInfoError, descriptionInfoError, SubEventRemoveInfoGoogleError);
                }
            }			
#endif
#if ENABLE_APPLE
            if (nameEvent == AppleController.EVENT_APPLE_CONTROLLER_AUTHENTICATED)
            {
                if (!(bool)parameters[0])
                {
                    string titleInfoError = LanguageController.Instance.GetText("message.error");
                    string descriptionInfoError = LanguageController.Instance.GetText("screen.register.login.apple.error");
                    UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleInfoError, descriptionInfoError, SubEventRemoveInfoGoogleError);
                }
            }
#endif
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(UsersController.EVENT_USER_CONFIRMATION_REMOVED_RECORD))
            {
                if ((bool)parameters[0])
                {
					SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_RESET_LOCAL_DATA);
					UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
					string titleWait = LanguageController.Instance.GetText("message.info");
					string descriptionWait = LanguageController.Instance.GetText("screen.settings.loggedin.removed.success");
					ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleWait, descriptionWait, SubEventShowOptionsAfterRemoveAccount);
                }
            }
			if (nameEvent.Equals(UsersController.EVENT_USER_REGISTER_RESULT))
            {
                bool success = (bool)parameters[0];
                if (success)
                {
#if ENABLE_FIREBASE
                    SystemEventController.Instance.DelaySystemEvent(EVENT_REGISTER_PLATFORM_MESSAGE_DELAYED_REPORT_NEW_USER, 2);
#else                    
                    UIEventController.Instance.DelayUIEvent(UsersController.EVENT_USER_LOGIN_REQUEST, 0.1f, _emailToCheck, _passwordToCheck, LoginPlatforms.Email);
#endif
                }
                else
                {
                    UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
                    string titleInfoError = LanguageController.Instance.GetText("message.error");
                    string descriptionInfoError = LanguageController.Instance.GetText("screen.register.wrong.register");
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleInfoError, descriptionInfoError);
                }
            }		
 			if (nameEvent.Equals(EVENT_REGISTER_PLATFORM_MESSAGE_DELAYED_REPORT_NEW_USER))
            {
                UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
                string titleInfoComplete = LanguageController.Instance.GetText("message.info");
                string descriptionInfoComplete = LanguageController.Instance.GetText("screen.register.check.email");
                ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, titleInfoComplete, descriptionInfoComplete, SubEventRegisterForPlatformConfirmation);
            }	
 			if (nameEvent.Equals(EventRegisterPlatformFinallyLogin))
            {
                UIEventController.Instance.DelayUIEvent(UsersController.EVENT_USER_LOGIN_REQUEST, 0.1f, UsersController.Instance.CurrentRegisterEmail, UsersController.Instance.CurrentRegisterPassword, UsersController.Instance.CurrentRegisterPlatform, UsersController.Instance.CurrentAccessToken);
            }			
			if (nameEvent.Equals(UsersController.EVENT_USER_LOGIN_FORMATTED))		
			{
				if ((bool)parameters[0])
				{
					ChangeConfiguration(ConfigSettings.LoggedIn);
					UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
					UIEventController.Instance.ClearUIEvents(UsersController.EVENT_USER_LOGIN_REQUEST);
					if (UsersController.Instance.CurrentUser.Admin && (UsersController.Instance.CurrentUser.AdminCode == 2))
					{
#if ENABLE_AI_OPERATIONS
						buttonAISelector.gameObject.SetActive(true);
#else
						buttonAISelector.gameObject.SetActive(false);
#endif	
					}					
				}
				else
				{
					if (this._content.gameObject.activeSelf)
					{
						_iterationsRequest++;
						if (_iterationsRequest < 4)
						{
							if ((UsersController.Instance.CurrentRegisterEmail != null) && (UsersController.Instance.CurrentRegisterEmail.Length > 0))
							{
								UIEventController.Instance.DelayUIEvent(UsersController.EVENT_USER_LOGIN_REQUEST, 3f, UsersController.Instance.CurrentRegisterEmail, UsersController.Instance.CurrentRegisterPassword, UsersController.Instance.CurrentRegisterPlatform, UsersController.Instance.CurrentAccessToken);
							}
							else
							{
								UIEventController.Instance.DelayUIEvent(UsersController.EVENT_USER_LOGIN_REQUEST, 3f, UsersController.Instance.CurrentUser.Email, UsersController.Instance.CurrentUser.Password, UsersController.Instance.CurrentRegisterPlatform, UsersController.Instance.CurrentAccessToken);
							}
						}
						else
						{
							UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
							string titleInfoComplete = LanguageController.Instance.GetText("message.error");
							string descriptionInfoComplete = LanguageController.Instance.GetText("screen.register.login.with.platform.has.failed");
							ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, titleInfoComplete, descriptionInfoComplete);
							ChangeConfiguration(ConfigSettings.Options);
						}
					}
				}
			}
        }
	}
}
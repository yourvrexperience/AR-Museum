using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.UserManagement;
using yourvrexperience.Utils;
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
using yourvrexperience.VR;
#endif
using static TMPro.TMP_InputField;

namespace yourvrexperience.template6dof
{
   public class ScreenRegisterUserView : BaseScreenView, IScreenView
    {
        public const string ScreenName = "ScreenRegisterUserView";

        public const string EVENT_REGISTER_MESSAGE_FINALLY_LOGIN = "EVENT_REGISTER_MESSAGE_FINALLY_LOGIN";
        public const string EVENT_REGISTER_MESSAGE_DELAYED_REPORT_NEW_USER = "EVENT_REGISTER_MESSAGE_DELAYED_REPORT_NEW_USER";        
        public const string SUBEVENT_REGISTER_NEW_USER_ACKNOWLEDGE = "SUBEVENT_REGISTER_NEW_USER_ACKNOWLEDGE";
        public const string SUBEVENT_REGISTER_MESSAGE_CHECK_EMAIL = "SUBEVENT_REGISTER_MESSAGE_CHECK_EMAIL";

        private GameObject _root;
        private Transform _container;

        private string _emailToCheck;
        private string _passwordToCheck;

        private CustomInput _emailToRegister;
        private CustomInput _passwordRegister;
        private CustomInput _passwordRepeat;

        public override void Initialize(params object[] parameters)
        {
            base.Initialize(parameters);

            _root = this.gameObject;
            _container = _root.transform.Find("Content");

            _container.Find("Button_Apply").GetComponent<Button>().onClick.AddListener(ApplyRegisterPressed);
            if (_container.Find("ButtonBack") != null) _container.Find("ButtonBack").GetComponent<Button>().onClick.AddListener(BackPressed);

            _container.Find("Title").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.register.word");
            _container.Find("EmailTitle").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.register.email");
            _container.Find("PasswordTitle").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.register.password");
            _container.Find("PasswordConfirmationTitle").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.register.confirm");
            _container.Find("Button_Apply/Title").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.register.word");

            _emailToRegister = _container.Find("EmailValue").GetComponent<CustomInput>();
            _passwordRegister = _container.Find("PasswordValue").GetComponent<CustomInput>();
            _passwordRepeat = _container.Find("PasswordConfirmationValue").GetComponent<CustomInput>();

#if UNITY_EDITOR
            _emailToRegister.text = "info@yourvrexperience.com";
            _passwordRegister.text = "password";
            _passwordRepeat.text = "password";
#endif
            UIEventController.Instance.Event += OnUIEvent;
            SystemEventController.Instance.Event += OnSystemEvent;

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
            _passwordRegister.contentType = ContentType.Standard;
            _passwordRepeat.gameObject.SetActive(false);
            _emailToRegister.OnFocusEvent += OnFocusEmailRegisterInputValue;
            _passwordRegister.OnFocusEvent += OnFocusPasswordRegisterInputValue;
#endif
        }

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
        private void OnFocusEmailRegisterInputValue()
        {
			_content.gameObject.SetActive(false);
			ScreenController.Instance.CreateScreen(ScreenVRKeyboardView.ScreenName, false, true,  _emailToRegister.gameObject, _emailToRegister, 40);
        }

        private void OnFocusPasswordRegisterInputValue()
        {
			_content.gameObject.SetActive(false);
			ScreenController.Instance.CreateScreen(ScreenVRKeyboardView.ScreenName, false, true,  _passwordRegister.gameObject, _passwordRegister, 10, ContentType.Standard);
        }
#endif

        public override void Destroy()
        {
            base.Destroy();

            if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
        }

        private void ApplyRegisterPressed()
        {
            _emailToCheck = _container.Find("EmailValue").GetComponent<TMP_InputField>().text.ToLower();
            _passwordToCheck = _container.Find("PasswordValue").GetComponent<TMP_InputField>().text.ToLower();
            string confirmationToCheck = _container.Find("PasswordConfirmationValue").GetComponent<TMP_InputField>().text.ToLower();

            if ((_emailToCheck.Length == 0) || (_passwordToCheck.Length == 0) || (confirmationToCheck.Length == 0))
            {
                string titleInfoError = LanguageController.Instance.GetText("message.error");
                string descriptionInfoError = LanguageController.Instance.GetText("screen.message.logging.error");
                ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleInfoError, descriptionInfoError);
            }
            else
            {
                if (_passwordToCheck != confirmationToCheck)
                {
                    string titleInfoError = LanguageController.Instance.GetText("message.error");
                    string descriptionInfoError = LanguageController.Instance.GetText("screen.register.mistmatch.password");
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleInfoError, descriptionInfoError);
                }
                else
                {
                    string titleWait = LanguageController.Instance.GetText("screen.wait.register.title");
                    string descriptionWait = LanguageController.Instance.GetText("screen.wait.register.description");
                    UIEventController.Instance.DelayUIEvent(UsersController.EVENT_USER_REGISTER_REQUEST, 0.1f, _emailToCheck, _passwordToCheck, LoginPlatforms.Email, "EMAIL");
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, this.gameObject, titleWait, descriptionWait);
                }
            }
        }

        private void BackPressed()
        {
            UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
        }

        protected void OnUIEvent(string _nameEvent, params object[] _list)
        {
            if (_nameEvent == SUBEVENT_REGISTER_NEW_USER_ACKNOWLEDGE)
            {
                Invoke("BackPressed", 0.1f);
            }
            if (_nameEvent == SUBEVENT_REGISTER_MESSAGE_CHECK_EMAIL)
            {
                UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
                ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, this.gameObject, LanguageController.Instance.GetText("message.info"), LanguageController.Instance.GetText("message.please.wait"));
                SystemEventController.Instance.DelaySystemEvent(EVENT_REGISTER_MESSAGE_FINALLY_LOGIN, 2);
            }
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL           
            if (_nameEvent.Equals(ScreenVRKeyboardView.EventScreenVRKeyboardSetNewText))
			{
				if (_emailToRegister.gameObject == (GameObject)_list[0])
				{
					_content.gameObject.SetActive(true);
					_emailToRegister.text = (string)_list[1];
				}
				if (_passwordRegister.gameObject == (GameObject)_list[0])
				{
					_content.gameObject.SetActive(true);
					_passwordRegister.text = (string)_list[1];
                    _passwordRepeat.text = _passwordRegister.text;
				}
			}            
#endif            
        }

        private void OnSystemEvent(string _nameEvent, object[] _list)
        {
            if (_nameEvent == UsersController.EVENT_USER_REGISTER_RESULT)
            {
                bool success = (bool)_list[0];
                if (success)
                {
#if ENABLE_FIREBASE
                    SystemEventController.Instance.DelaySystemEvent(EVENT_REGISTER_MESSAGE_DELAYED_REPORT_NEW_USER, 2);
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
            if (_nameEvent == EVENT_REGISTER_MESSAGE_DELAYED_REPORT_NEW_USER)
            {
                UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
                string titleInfoComplete = LanguageController.Instance.GetText("message.info");
                string descriptionInfoComplete = LanguageController.Instance.GetText("screen.register.check.email");
                ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, titleInfoComplete, descriptionInfoComplete, SUBEVENT_REGISTER_MESSAGE_CHECK_EMAIL);
            }
            if (_nameEvent == EVENT_REGISTER_MESSAGE_FINALLY_LOGIN)
            {
                UIEventController.Instance.DelayUIEvent(UsersController.EVENT_USER_LOGIN_REQUEST, 0.1f, _emailToCheck, _passwordToCheck, LoginPlatforms.Email);
            }
            if (_nameEvent == UsersController.EVENT_USER_LOGIN_FORMATTED)
            {
                UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
                if ((bool)_list[0])
                {
                    string titleInfoSuccess = LanguageController.Instance.GetText("message.success");
                    string descriptionInfoSuccess = LanguageController.Instance.GetText("screen.register.success.register");
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleInfoSuccess, descriptionInfoSuccess, SUBEVENT_REGISTER_NEW_USER_ACKNOWLEDGE);
                }
            }
        }
    }
}
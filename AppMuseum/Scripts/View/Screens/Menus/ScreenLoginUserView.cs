using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.UserManagement;
using yourvrexperience.Utils;
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
using yourvrexperience.VR;
using static TMPro.TMP_InputField;
#endif

namespace yourvrexperience.template6dof
{
    public class ScreenLoginUserView : BaseScreenView, IScreenView
    {
        public const string ScreenName = "ScreenLoginUserView";

        public const string SUB_EVENT_SCREENLOGIN_ACKNOWLEDGE_LOGIN = "SUB_EVENT_SCREENLOGIN_ACKNOWLEDGE_LOGIN";

        private GameObject _root;
        private Transform _container;

        private CustomInput _emailValue;
        private CustomInput _passwordValue;


        public override void Initialize(params object[] parameters)
        {
            base.Initialize(parameters);

            _root = this.gameObject;
            _container = _root.transform.Find("Content");

            _container.Find("Button_OK").GetComponent<Button>().onClick.AddListener(OkPressed);
            if (_container.Find("ButtonBack")!=null) _container.Find("ButtonBack").GetComponent<Button>().onClick.AddListener(BackPressed);
            _container.Find("Button_Forget").GetComponent<Button>().onClick.AddListener(ForgetPressed);
            _container.Find("Button_Register").GetComponent<Button>().onClick.AddListener(RegisterPressed);

            _container.Find("Title").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.login.title");
            _container.Find("EmailTitle").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.login.email");
            _container.Find("PasswordTitle").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.login.password");
            _container.Find("Button_Forget/Title").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.login.forget.mail");
            _container.Find("Button_Register/Title").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.login.register.mail");

            _emailValue = _container.Find("EmailValue").GetComponent<CustomInput>();
            _passwordValue = _container.Find("PasswordValue").GetComponent<CustomInput>();

#if UNITY_EDITOR
            _emailValue.text = "esteban@yourvrexperience.com";
            _passwordValue.text = "12345";
#endif

            UIEventController.Instance.Event += OnUIEvent;
            SystemEventController.Instance.Event += OnSystemEvent;

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
            _passwordValue.contentType = ContentType.Standard;
            _emailValue.OnFocusEvent += OnFocusEmailInputValue;
            _passwordValue.OnFocusEvent += OnFocusPasswordInputValue;
#endif
        }

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
        private void OnFocusEmailInputValue()
        {
			_content.gameObject.SetActive(false);
			ScreenController.Instance.CreateScreen(ScreenVRKeyboardView.ScreenName, false, true,  _emailValue.gameObject, _emailValue, 40);
        }

        private void OnFocusPasswordInputValue()
        {
			_content.gameObject.SetActive(false);
			ScreenController.Instance.CreateScreen(ScreenVRKeyboardView.ScreenName, false, true,  _passwordValue.gameObject, _passwordValue, 10, ContentType.Standard);
        }
#endif

        void OnDestroy()
        {
            Destroy();
        }

        public override void Destroy()
        {
            base.Destroy();

            if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
        }
        
        private void OkPressed()
        {
            string emailToCheck = _container.Find("EmailValue").GetComponent<TMP_InputField>().text.ToLower();
            string passwordToCheck = _container.Find("PasswordValue").GetComponent<TMP_InputField>().text.ToLower();

            if ((emailToCheck.Length == 0) || (passwordToCheck.Length == 0))
            {
                string titleInfoError = LanguageController.Instance.GetText("message.error");
                string descriptionInfoError = LanguageController.Instance.GetText("screen.message.logging.error");
                ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleInfoError, descriptionInfoError);
            }
            else
            {
                string titleWait = LanguageController.Instance.GetText("screen.wait.logging.title");
                string descriptionWait = LanguageController.Instance.GetText("screen.wait.logging.description");
                UIEventController.Instance.DispatchUIEvent(UsersController.EVENT_USER_LOGIN_REQUEST, emailToCheck, passwordToCheck, LoginPlatforms.Email, "EMAIL");
                ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, this.gameObject, titleWait, descriptionWait);
            }
        }
        
        private void BackPressed()
        {
            UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
        }

        private void ForgetPressed()
        {
            string emailToCheck = _container.Find("EmailValue").GetComponent<TMP_InputField>().text.ToLower();
            if (emailToCheck.Length == 0)
            {
                string titleInfoError = LanguageController.Instance.GetText("message.error");
                string descriptionInfoError = LanguageController.Instance.GetText("screen.login.email");
                ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleInfoError, descriptionInfoError);
            }
            else
            {
                string titleWait = LanguageController.Instance.GetText("message.info");
                string descriptionWait = LanguageController.Instance.GetText("message.please.wait");
                ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, this.gameObject, titleWait, descriptionWait);
                UIEventController.Instance.DispatchUIEvent(UsersController.EVENT_USER_REQUEST_RESET_PASSWORD, emailToCheck);
            }
        }

        private void RegisterPressed()
        {
            UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
            ScreenController.Instance.CreateScreen(ScreenRegisterUserView.ScreenName, false, true);
        }

        protected void OnUIEvent(string _nameEvent, params object[] _list)
        {
            if (_nameEvent == SUB_EVENT_SCREENLOGIN_ACKNOWLEDGE_LOGIN)
            {
                BackPressed();
            }
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL        
            if (_nameEvent.Equals(ScreenVRKeyboardView.EventScreenVRKeyboardSetNewText))
			{
				if (_emailValue.gameObject == (GameObject)_list[0])
				{
					_content.gameObject.SetActive(true);
					_emailValue.text = (string)_list[1];
				}
				if (_passwordValue.gameObject == (GameObject)_list[0])
				{
					_content.gameObject.SetActive(true);
					_passwordValue.text = (string)_list[1];
				}
			}            
#endif            
        }

        private void OnSystemEvent(string _nameEvent, object[] _list)
        {
            if (_nameEvent == UsersController.EVENT_USER_LOGIN_FORMATTED)
            {
                UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
                if ((bool)_list[0])
                {
                    UserModel userData = (UserModel)_list[1];
                    string titleInfoSuccess = LanguageController.Instance.GetText("message.success");
                    string descriptionInfoSuccess = LanguageController.Instance.GetText("screen.login.successfull.by.email");
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleInfoSuccess, descriptionInfoSuccess, SUB_EVENT_SCREENLOGIN_ACKNOWLEDGE_LOGIN);
                }
                else
                {
                    string titleInfoError = LanguageController.Instance.GetText("message.error");
                    string descriptionInfoError = LanguageController.Instance.GetText("screen.message.logging.wrong.user");
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleInfoError, descriptionInfoError);
                }
            }
            if (_nameEvent == UsersController.EVENT_USER_RESPONSE_RESET_PASSWORD)
            {
                UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
                if ((bool)_list[0])
                {
                    string titleInfoReset = LanguageController.Instance.GetText("message.info");
                    string descriptionInfoReset = LanguageController.Instance.GetText("screen.login.email.reseted.check.email");
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleInfoReset, descriptionInfoReset);
                }
                else
                {
                    string titleInfoError = LanguageController.Instance.GetText("message.error");
                    string descriptionInfoError = LanguageController.Instance.GetText("screen.login.email.error.reset");
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleInfoError, descriptionInfoError);
                }
            }
        }
    }
}
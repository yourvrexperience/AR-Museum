using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;

namespace yourvrexperience.UserManagement
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

        public override void Initialize(params object[] parameters)
        {
            base.Initialize(parameters);

            _root = this.gameObject;
            _container = _root.transform.Find("Content");

            _container.Find("Button_Apply").GetComponent<Button>().onClick.AddListener(ApplyRegisterPressed);
            if (_container.Find("Button_GoBack") != null) _container.Find("Button_GoBack").GetComponent<Button>().onClick.AddListener(BackPressed);

            _container.Find("Title").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.register.word");
            _container.Find("EmailTitle").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.register.email");
            _container.Find("PasswordTitle").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.register.password");
            _container.Find("PasswordConfirmationTitle").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.register.confirm");
            _container.Find("Button_Apply/Title").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.register.word");

#if UNITY_EDITOR
            _container.Find("EmailValue").GetComponent<InputField>().text = "info@yourvrexperience.com";
            _container.Find("PasswordValue").GetComponent<InputField>().text = "basura";
            _container.Find("PasswordConfirmationValue").GetComponent<InputField>().text = "basura";
#endif
            UIEventController.Instance.Event += OnUIEvent;
            SystemEventController.Instance.Event += OnSystemEvent;
        }

        public override void Destroy()
        {
            base.Destroy();

            if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
        }

        private void ApplyRegisterPressed()
        {
            _emailToCheck = _container.Find("EmailValue").GetComponent<InputField>().text.ToLower();
            _passwordToCheck = _container.Find("PasswordValue").GetComponent<InputField>().text.ToLower();
            string confirmationToCheck = _container.Find("PasswordConfirmationValue").GetComponent<InputField>().text.ToLower();

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
                    UIEventController.Instance.DelayUIEvent(UsersController.EVENT_USER_REGISTER_REQUEST, 0.1f, _emailToCheck, _passwordToCheck, LoginPlatforms.Email, UserModel.ACCOUNT_DATA_EMAIL);
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, this.gameObject, titleWait, descriptionWait);
                }
            }
        }

        private void BackPressed()
        {
            ScreenController.Instance.CreateScreen(ScreenRegisterOptionsView.ScreenName, true, false);
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
        }

        private void OnSystemEvent(string _nameEvent, object[] _list)
        {
            if (_nameEvent == UsersController.EVENT_USER_REGISTER_CONFIRMATION)
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
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_FACEBOOK || ENABLE_GOOGLE
using yourvrexperience.Social;
#endif
using yourvrexperience.Utils;

namespace yourvrexperience.UserManagement
{
    public class ScreenRegisterOptionsView : BaseScreenView, IScreenView
    {
        public const string ScreenName = "ScreenRegisterOptionsView";

        public const string SUBEVENT_REGISTER_NEW_FACEBOOK_ACKNOWLEDGE = "SUBEVENT_REGISTER_NEW_FACEBOOK_ACKNOWLEDGE";

        private GameObject _root;
        private Transform _container;

        public override void Initialize(params object[] parameters)
        {
            base.Initialize(parameters);

            _root = this.gameObject;
            _container = _root.transform.Find("Content");

            _container.Find("Title").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.register.options.title");

            GameObject registerWithEmail = _container.Find("Register_Mail").gameObject;
            registerWithEmail.transform.Find("Text").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.register.options.with.email");
            registerWithEmail.GetComponent<Button>().onClick.AddListener(RegisterByEmail);

            GameObject registerByFacebook = _container.Find("Register_Facebook").gameObject;
            registerByFacebook.transform.Find("Text").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.register.options.with.facebook");
            registerByFacebook.GetComponent<Button>().onClick.AddListener(RegisterByFacebook);
            registerByFacebook.SetActive(false);

            GameObject registerByGoogle = _container.Find("Register_Google").gameObject;
            registerByGoogle.transform.Find("Text").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.register.options.with.google");
            registerByGoogle.GetComponent<Button>().onClick.AddListener(RegisterByGoogle);
            registerByGoogle.SetActive(false);

#if ENABLE_FACEBOOK
            registerByFacebook.SetActive(true);
#endif

#if ENABLE_FACEBOOK
            registerByGoogle.SetActive(true);
#endif

            if (_container.Find("Back") != null) _container.Find("Back").GetComponent<Button>().onClick.AddListener(BackPressed);

            UIEventController.Instance.Event += OnUIEvent;
            SystemEventController.Instance.Event += OnSystemEvent;
        }

        public GameObject GetGameObject()
        {
            return this.gameObject;
        }

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

        private void RegisterByEmail()
        {
            ScreenController.Instance.CreateScreen(ScreenRegisterUserView.ScreenName, true, false);
        }

        private void RegisterByFacebook()
        {
#if ENABLE_FACEBOOK
            ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, this.gameObject, LanguageController.Instance.GetText("message.info"), LanguageController.Instance.GetText("message.please.wait"));
            FacebookController.Instance.Initialitzation();
#endif
        }

        private void RegisterByGoogle()
        {
#if ENABLE_GOOGLE
            ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, this.gameObject, LanguageController.Instance.GetText("message.info"), LanguageController.Instance.GetText("message.please.wait"));
            GoogleController.Instance.Initialitzation();
#endif
        }


        private void BackPressed()
        {
            ScreenController.Instance.CreateScreen(ScreenMainUserView.ScreenName, true, false);
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
        }

        protected void OnUIEvent(string nameEvent, params object[] parameters)
        {
            if (nameEvent == SUBEVENT_REGISTER_NEW_FACEBOOK_ACKNOWLEDGE)
            {
                BackPressed();
            }
#if ENABLE_FACEBOOK
            if (nameEvent == FacebookController.EVENT_FACEBOOK_COMPLETE_INITIALITZATION)
            {
                
            }
#endif
#if ENABLE_GOOGLE
            if (nameEvent == GoogleController.EVENT_GOOGLE_CONTROLLER_AUTHENTICATED)
            {
                if (!(bool)parameters[0])
                {
                    string titleInfoError = LanguageController.Instance.GetText("message.error");
                    string descriptionInfoError = LanguageController.Instance.GetText("screen.resgister.login.google.error");
                    UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleInfoError, descriptionInfoError, SUBEVENT_REGISTER_NEW_FACEBOOK_ACKNOWLEDGE);
                }
            }
#endif
        }
    }
}

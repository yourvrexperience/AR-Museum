using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;

namespace yourvrexperience.UserManagement
{
    public class ScreenConnectionView : BaseScreenView, IScreenView
    {
        public const string ScreenName = "ScreenConnectionView";

        public const string EVENT_CONNECTION_SCREEN_TIMEOUT = "EVENT_CONNECTION_SCREEN_TIMEOUT";
        public const string EVENT_CONNECTION_DELAYED_INIT = "EVENT_CONNECTION_DELAYED_INIT";

        private GameObject _root;
        private Transform _container;

        public override void Initialize(params object[] parameters)
        {
            base.Initialize(parameters);

            _root = this.gameObject;
            _container = _root.transform.Find("Content");

            _container.Find("Title").GetComponent<Text>().text = LanguageController.Instance.GetText("message.user.registration.title");

            _container.Find("Description").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.connecting.please.wait");

            SystemEventController.Instance.Event += OnSystemEvent;

            UIEventController.Instance.DispatchUIEvent(UsersController.EVENT_CONFIGURATION_DATA_REQUESTED);

#if !ENABLE_FIREBASE
            SystemEventController.Instance.DelaySystemEvent(EVENT_CONNECTION_SCREEN_TIMEOUT, 5);
#endif
        }

        public GameObject GetGameObject()
        {
            return this.gameObject;
        }

        public override void Destroy()
        {
            base.Destroy();

            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
        }

        private void LoadMainScreen()
        {
            if (!UserModel.LoginWithStoredLogin())
            {
                ScreenController.Instance.CreateScreen(ScreenMainUserView.ScreenName, true, false);			
            }
        }

        private void OnSystemEvent(string _nameEvent, object[] _list)
        {
            if (_nameEvent == UsersController.EVENT_CONFIGURATION_DATA_RECEIVED)
            {
                SystemEventController.Instance.ClearSystemEvents();
                bool success = (bool)_list[0];                
                if (success)
                {
                    SystemEventController.Instance.DelaySystemEvent(EVENT_CONNECTION_DELAYED_INIT, 0.2f);
                }
                else
                {
                    _container.Find("Description").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.connecting.server.not.enabled");
                }
            }
            if (_nameEvent == EVENT_CONNECTION_DELAYED_INIT)
            {
                LoadMainScreen();
            }
            if (_nameEvent ==  EVENT_CONNECTION_SCREEN_TIMEOUT)
            {
                _container.Find("Description").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.connecting.failed");
            }
            if (_nameEvent == UsersController.EVENT_USER_LOGIN_FORMATTED)
            {
                UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
                ScreenController.Instance.CreateScreen(ScreenMainUserView.ScreenName, true, false);	
            }
        }
    }
}

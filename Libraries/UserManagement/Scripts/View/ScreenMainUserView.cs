using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;

namespace yourvrexperience.UserManagement
{
    public class ScreenMainUserView : BaseScreenView, IScreenView
    {
        public const string ScreenName = "ScreenMainUserView";

        private const string SUB_EVENT_SCREENMAIN_CONFIRMATION_EXIT_APP = "SUB_EVENT_SCREENMAIN_CONFIRMATION_EXIT_APP";

        private GameObject _root;
        private Transform _container;

        public override void Initialize(params object[] parameters)
        {
            base.Initialize(parameters);

            _root = this.gameObject;
            _container = _root.transform.Find("Content");

            _container.Find("Title").GetComponent<Text>().text = LanguageController.Instance.GetText("message.user.registration.title");

            GameObject registerUser = _container.Find("Register_User").gameObject;
            registerUser.transform.Find("Text").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.main.user.register");
            registerUser.GetComponent<Button>().onClick.AddListener(RegisterNewUser);
            
            GameObject loginCurrentUser = _container.Find("Load_User").gameObject;
            loginCurrentUser.transform.Find("Text").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.main.user.consult");
            loginCurrentUser.GetComponent<Button>().onClick.AddListener(LoadNewUser);

            GameObject manageUserList = _container.Find("Manage_Users").gameObject;
            manageUserList.transform.Find("Text").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.main.user.management");
            manageUserList.GetComponent<Button>().onClick.AddListener(ManagementUsers);

            GameObject inputFormUser = _container.Find("Input_Form").gameObject;
            inputFormUser.transform.Find("Text").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.main.user.input.form");
            inputFormUser.GetComponent<Button>().onClick.AddListener(InputFormUser);

            if (UsersController.Instance.CurrentUser.Email.Length == 0)
            {
                _container.transform.Find("CurrentUser").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.main.no.current.user");
            }
            else
            {
                _container.transform.Find("CurrentUser").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.main.current.user") + " " + UsersController.Instance.CurrentUser.Email;
            }

            _container.Find("Exit").GetComponent<Button>().onClick.AddListener(BackPressed);

            UIEventController.Instance.Event += OnUIEvent;
        }

        public GameObject GetGameObject()
        {
            return this.gameObject;
        }

        public override void Destroy()
        {
            base.Destroy();

            if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
        }

        private void RegisterNewUser()
        {
            ScreenController.Instance.CreateScreen(ScreenRegisterOptionsView.ScreenName, true, false);
        }

        private void LoadNewUser()
        {
            ScreenController.Instance.CreateScreen(ScreenLoginUserView.ScreenName, true, false);
        }

        private void InputFormUser()
        {
            UserManagementApplication.Instance.FormData.Initialize();
            ScreenController.Instance.CreateScreen(ScreenInputFormView.ScreenName, true, false);
        }

        private void ManagementUsers()
        {
            if (UsersController.Instance.CurrentUser.Id == -1)
            {
                string warning = LanguageController.Instance.GetText("message.error");
                string description = LanguageController.Instance.GetText("screen.main.user.must.login");
                ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, warning, description);
            }
            else
            {
                if (UsersController.Instance.CurrentUser.Admin)
                {
                    ScreenController.Instance.CreateScreen(ScreenListUsersView.ScreenName, true, false);
                }
                else
                {
                    string warning = LanguageController.Instance.GetText("message.error");
                    string description = LanguageController.Instance.GetText("screen.main.user.no.access");
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, warning, description);
                }
            }
        }

        private void BackPressed()
        {
            string warning = LanguageController.Instance.GetText("message.warning");
            string description = LanguageController.Instance.GetText("message.do.you.want.exit");
            ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenConfirmation, this.gameObject, warning, description, SUB_EVENT_SCREENMAIN_CONFIRMATION_EXIT_APP);
        }

        protected void OnUIEvent(string _nameEvent, params object[] _list)
        {
            if (_nameEvent == SUB_EVENT_SCREENMAIN_CONFIRMATION_EXIT_APP)
            {
                if (this.gameObject == (GameObject)_list[0])
                {
                    if ((ScreenInformationResponses)_list[1] == ScreenInformationResponses.Confirm)
                    {
                        Application.Quit();
                    }
                }
            }
        }
    }
}

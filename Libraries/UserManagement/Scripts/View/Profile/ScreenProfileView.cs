using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;

namespace yourvrexperience.UserManagement
{
    public class ScreenProfileView : BaseScreenView, IScreenView
    {
        public const string ScreenNameDisplay = "ScreenProfileDisplay";
        public const string ScreenNameEdit    = "ScreenProfileEdit";

        public const string SUB_EVENT_SCREENPROVIDERPROFILE_EXIT_WITHOUT_SAVING = "SUB_EVENT_SCREENPROVIDERPROFILE_EXIT_WITHOUT_SAVING";

        public const string SUB_EVENT_SCREENPROVIDERPROFILE_CONFIRMATION_UPDATED = "SUB_EVENT_SCREENPROVIDERPROFILE_CONFIRMATION_UPDATED";

        private GameObject _root;
        private Transform _container;
        private UserModel _userData;
        private bool _isDisplayInfo;

        private Transform _btnExit;
        private Transform _btnSave;
        private bool _hasBeenModified = false;
        private bool _hasBeenUpdated = false;

        public bool HasBeenModified
        {
            get { return _hasBeenModified; }
            set
            {
                _hasBeenModified = value;
                if (_hasBeenModified)
                {
                    if (_btnSave != null) _btnSave.gameObject.SetActive(true);
                }
                else
                {
                    if (_btnSave != null) _btnSave.gameObject.SetActive(false);
                }
            }
        }

        public override void Initialize(params object[] parameters)
        {
            base.Initialize(parameters);

            _userData = (UserModel)parameters[0];

            _root = this.gameObject;
            _container = _root.transform.Find("Content/ScrollPage/Page");

            Transform buttonEdit = _root.transform.Find("Content/Button_Edit");
            if (buttonEdit != null)
            {
                buttonEdit.gameObject.SetActive(true);
                buttonEdit.GetComponent<Button>().onClick.AddListener(EditPressed);
                _isDisplayInfo = true;
            }
            else
            {
                _isDisplayInfo = false;
            }

            _btnExit = _root.transform.Find("Content/Button_Exit");
            if (_btnExit != null)
            {
                if (_isDisplayInfo)
                {
                    _btnExit.GetComponent<Button>().onClick.AddListener(ExitPressed);
                }
                else
                {
                    _btnExit.GetComponent<Button>().onClick.AddListener(ExitEditionPressed);
                    HasBeenModified = false;
                }
            }
            else
            {
                if (!_isDisplayInfo)
                {
                    HasBeenModified = false;
                }
            }

            _btnSave = _root.transform.Find("Content/Button_Save");
            if (_btnSave != null)
            {
                _btnSave.GetComponent<Button>().onClick.AddListener(SavePressed);
            }

            UIEventController.Instance.Event += OnUIEvent;
            SystemEventController.Instance.Event += OnSystemEvent;

            LoadUserData();
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

        public void LoadUserData()
        {
            if (_isDisplayInfo)
            {
                _container.Find("Title").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.profile.title.display");

                // NAME TEXT
                _container.Find("Name").GetComponent<Text>().text = _userData.Profile.Name;

                // ADDRESS TEXT
                _container.Find("Address/Title").GetComponent<Text>().text = _userData.Profile.Address;

                // DESCRIPTION TEXT
                _container.Find("Description/Title").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.profile.description.title.display");
                _container.Find("Description/Scroll/Value").GetComponent<Text>().text = _userData.Profile.Description;
            }
            else
            {
                _container.Find("Title").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.profile.title.edit");

                // NAME INPUT
                _container.Find("Name").GetComponent<InputField>().text = _userData.Profile.Name;

                // ADDRESS INPUT
                _container.Find("Address/Title").GetComponent<Text>().text = _userData.Profile.Address;

                // DESCRIPTION INPUT
                _container.Find("Description/Title").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.profile.description.title.edit");
                _container.Find("Description/Value").GetComponent<InputField>().text = _userData.Profile.Description;
                _container.Find("Description/Value").GetComponent<InputField>().onEndEdit.AddListener(OnDescriptionProfileEdited);
            }
        }

        protected void GoBackPressed()
        {
            if (_isDisplayInfo)
            {
                UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
            }
            else
            {
                if (!_hasBeenModified)
                {
                    UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
                }
                else
                {
                    string warning = LanguageController.Instance.GetText("message.warning");
                    string description = LanguageController.Instance.GetText("message.profile.proovider.exit.without.saving");
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenConfirmation, this.gameObject, warning, description, SUB_EVENT_SCREENPROVIDERPROFILE_EXIT_WITHOUT_SAVING);
                }
            }
        }

        private void ExitPressed()
        {
            UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
        }

        private void ExitEditionPressed()
        {
            if (!_hasBeenModified)
            {
                UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
            }
            else
            {
                string warning = LanguageController.Instance.GetText("message.warning");
                string description = LanguageController.Instance.GetText("message.profile.proovider.exit.without.saving");
                ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenConfirmation, this.gameObject, warning, description, SUB_EVENT_SCREENPROVIDERPROFILE_EXIT_WITHOUT_SAVING);
            }
        }

        private void SavePressed()
        {
            string nameProfile = _container.Find("Name").GetComponent<InputField>().text;
            string addressProfile = _container.Find("Address/Title").GetComponent<Text>().text;
            string descriptionProfile = _container.Find("Description/Value").GetComponent<InputField>().text;

            UIEventController.Instance.DelayUIEvent(UsersController.EVENT_USER_UPDATE_PROFILE_REQUEST, 0.2f, _userData.Id.ToString(), nameProfile, addressProfile, descriptionProfile, "", "", "", "", "");
            UIEventController.Instance.DelayUIEvent(ScreenController.EventScreenControllerCreateInformationScreen, 0.1f, ScreenInformationView.ScreenLoading, this.gameObject, LanguageController.Instance.GetText("message.info"), LanguageController.Instance.GetText("message.please.wait"));

            _hasBeenModified = false;
            _hasBeenUpdated = true;
        }

        private void EditPressed()
        {
            ScreenController.Instance.CreateScreen(ScreenProfileView.ScreenNameEdit, false, true, _userData);
        }

        private void OnDescriptionProfileEdited(string _newValue)
        {
            _userData.Profile.Description = _newValue;
            HasBeenModified = true;
        }

        protected void OnUIEvent(string nameEvent, params object[] parameters)
        {
            if (!this.gameObject.activeSelf) return;

            if (nameEvent == SUB_EVENT_SCREENPROVIDERPROFILE_EXIT_WITHOUT_SAVING)
            {
                GameObject origin = (GameObject)parameters[0];
                UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
                if (origin == this.gameObject)
                {
                    if ((ScreenInformationResponses)parameters[1] == ScreenInformationResponses.Confirm)
                    {
                        UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
                    }
                }
            }
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent == UsersController.EVENT_USER_RESULT_FORMATTED_SINGLE_RECORD)
            {
                UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
                if ((parameters == null) || (parameters.Length == 0))
                {

                }
                else
                {
                    UserModel sUser = (UserModel)parameters[0];
                    if (sUser != null)
                    {
                        _userData.Copy(sUser);
                        LoadUserData();
                    }
                }
                if (_hasBeenUpdated)
                {
                    _hasBeenUpdated = false;
                    GoBackPressed();
                }
            }
        }
    }
}
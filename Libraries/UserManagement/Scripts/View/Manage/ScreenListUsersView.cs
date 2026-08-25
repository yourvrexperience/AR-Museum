using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;

namespace yourvrexperience.UserManagement
{
    public class ScreenListUsersView : BaseScreenView, IScreenView
    {
        public const string ScreenName = "ScreenListUsersView";

        [SerializeField] private GameObject UserItemPrefab;

        private GameObject _root;
        private Transform _container;
        private SlotManagerView _slotmanager;
        private UserModel _selectedData;

        private Button _buttonBack;

        public override void Initialize(params object[] parameters)
        {
            base.Initialize(parameters);

            _root = this.gameObject;
            _container = _root.transform.Find("Content");

            _container.Find("Title").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.list.user.title");

            if (_container.Find("Button_Back") != null)
            {
                _buttonBack = _container.Find("Button_Back").GetComponent<Button>();
                _buttonBack.onClick.AddListener(BackPressed);
            }

            _slotmanager = _container.Find("ListItems").GetComponent<SlotManagerView>();

            UIEventController.Instance.Event += OnUIEvent;
            SystemEventController.Instance.Event += OnSystemEvent;

            Invoke("ShowLoadingScreen", 0.1f);
        }

        public void ShowLoadingScreen()
        {
            ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, this.gameObject, LanguageController.Instance.GetText("message.info"), LanguageController.Instance.GetText("message.please.wait"));
            UIEventController.Instance.DispatchUIEvent(UsersController.EVENT_USER_CALL_CONSULT_ALL_RECORDS);
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

            if (_slotmanager != null)
            {
                _slotmanager.Destroy();
                _slotmanager = null;
            }

            if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
        }

        private void BackPressed()
        {
            ScreenController.Instance.CreateScreen(ScreenMainUserView.ScreenName, true, false);
        }

        protected void OnUIEvent(string nameEvent, params object[] parameters)
        {
            if (nameEvent == ItemUserView.EVENT_ITEM_USER_SELECTED)
            {
                GameObject parentObject = (GameObject)parameters[0];
                if (this.gameObject == parentObject)
                {
                    if ((int)parameters[2] != -1)
                    {
                        _selectedData = (UserModel)parameters[3];
                        ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, this.gameObject, LanguageController.Instance.GetText("message.info"), LanguageController.Instance.GetText("message.please.wait"));
                        UIEventController.Instance.DelayUIEvent(UsersController.EVENT_USER_CALL_CONSULT_SINGLE_RECORD, 0.2f, _selectedData.Id);
                    }
                    else
                    {
                        _selectedData = null;
                    }
                }
            }
        }

        private void OnSystemEvent(string _nameEvent, object[] _list)
        {
            if (!Content.gameObject.activeSelf)
            {
                return;
            }

            if (_nameEvent == UsersController.EVENT_USER_RESULT_FORMATTED_ALL_RECORDS)
            {
                UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);

                _slotmanager.ClearCurrentGameObject((bool)_list[0]);

                Dictionary<long, UserModel> users = (Dictionary<long, UserModel>)_list[1];

                List<ItemMultiObjectEntry> userItems = new List<ItemMultiObjectEntry>();
                int counter = 0;
                foreach (KeyValuePair<long, UserModel> user in users)
                {
                    userItems.Add(new ItemMultiObjectEntry(this.gameObject, counter, user.Value));
                    counter++;
                }
                
                _slotmanager.Initialize(10, userItems, UserItemPrefab);
            }
            if (_nameEvent == UsersController.EVENT_USER_RESULT_CONSULT_ALL_RECORDS)
            {
                if (!(bool)_list[0])
                {
                    UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);

                    string titleInfoError = LanguageController.Instance.GetText("message.error");
                    string descriptionInfoError = LanguageController.Instance.GetText("screen.list.user.error.retrieving");
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleInfoError, descriptionInfoError);
                }
            }
            if (_nameEvent == UsersController.EVENT_USER_CONFIRMATION_REMOVED_RECORD)
            {
                UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
                if ((bool)_list[0])
                {
                    string titleInfoSuccess = LanguageController.Instance.GetText("message.success");
                    string descriptionInfoSuccess = LanguageController.Instance.GetText("screen.list.user.delete.success");
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleInfoSuccess, descriptionInfoSuccess);
                }
                else
                {
                    string titleInfoError = LanguageController.Instance.GetText("message.error");
                    string descriptionInfoError = LanguageController.Instance.GetText("screen.list.user.error.to.delete");
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleInfoError, descriptionInfoError);
                }
            }
            if (_nameEvent ==  UsersController.EVENT_USER_RESULT_FORMATTED_SINGLE_RECORD)
            {
                UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
                if ((_list == null) || (_list.Length == 0))
                {
                    string titleInfoError = LanguageController.Instance.GetText("message.error");
                    string descriptionInfoError = LanguageController.Instance.GetText("screen.list.user.error.retrieving");
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleInfoError, descriptionInfoError);
                    return;
                }

                ScreenController.Instance.CreateScreen(ScreenProfileView.ScreenNameDisplay, false, true, (UserModel)_list[0]);
            }
        }
    }
}
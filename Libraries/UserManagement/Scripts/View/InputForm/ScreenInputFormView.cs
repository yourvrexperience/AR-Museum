using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;

namespace yourvrexperience.UserManagement
{
    public class ScreenInputFormView : BaseScreenView, IScreenView
    {
        public const string ScreenName = "ScreenInputFormView";

        public const string EventScreenInputFormViewConfirmedRegister = "EventScreenInputFormViewConfirmedRegister";

        private GameObject _root;
        private Transform _container;

        private TextMeshProUGUI _description;
        private TextMeshProUGUI _nextQuestion;
        private TMP_InputField _input;
        private Button _buttonNextQuestion;

        public override void Initialize(params object[] parameters)
        {
            base.Initialize(parameters);

            _root = this.gameObject;
            _container = _root.transform.Find("Content");

            _container.Find("Title").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.register.options.title");
            _description = _container.Find("Description").GetComponent<TextMeshProUGUI>();
            _input = _container.Find("InputField").GetComponent<TMP_InputField>();

            if (_container.Find("Button_Back") != null) _container.Find("Button_Back").GetComponent<Button>().onClick.AddListener(BackPressed);

            _buttonNextQuestion = _container.Find("ButtonNext").gameObject.GetComponent<Button>();
            _nextQuestion = _buttonNextQuestion.transform.Find("Text").GetComponent<TextMeshProUGUI>();
            _buttonNextQuestion.onClick.AddListener(OnNextPage);

            SystemEventController.Instance.Event += OnSystemEvent;
            UIEventController.Instance.Event += OnUIEvent;

            LoadQuestion();
        }

        void OnDestroy()
        {
            Destroy();
        }

        public override void Destroy()
        {
            base.Destroy();

            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
            if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
        }

        private void LoadQuestion()
        {
            _description.text = LanguageController.Instance.GetText(UserManagementApplication.Instance.FormData.Questions[UserManagementApplication.Instance.FormData.IndexQuestion]);
            _input.text = "";
            if (UserManagementApplication.Instance.FormData.AreMoreQuestions())
            {
                _nextQuestion.text = LanguageController.Instance.GetText("screen.register.options.next.question");
            }
            else
            {
                _nextQuestion.text = LanguageController.Instance.GetText("screen.register.options.complete.form");
            }
        }

        private void OnNextPage()
        {
            if (UserManagementApplication.Instance.FormData.AreMoreQuestions())
            {
                UserManagementApplication.Instance.FormData.RegisterResponse(_input.text);
                LoadQuestion();
            }
            else
            {
                _input.interactable = false;
                _buttonNextQuestion.interactable = false;

                UserManagementApplication.Instance.FormData.RegisterResponse(_input.text);
                string jsonFormData = UserManagementApplication.Instance.FormData.PackJSONData();
                UIEventController.Instance.DelayUIEvent(UsersController.EVENT_USER_INPUT_FORM_REQUEST, 0.2f, jsonFormData);
                ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, "", LanguageController.Instance.GetText("message.please.wait"));
            }
        }

        public GameObject GetGameObject()
        {
            return this.gameObject;
        }

        private void BackPressed()
        {
            ScreenController.Instance.CreateScreen(ScreenMainUserView.ScreenName, true, false);
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(UsersController.EVENT_USER_INPUT_FORM_FORMATTED))
            {
                bool success = (bool)parameters[0];
                UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
                ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, LanguageController.Instance.GetText("message.info"), LanguageController.Instance.GetText("screen.register.options.input.form.completed"), EventScreenInputFormViewConfirmedRegister);
            }
        }

        private void OnUIEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(EventScreenInputFormViewConfirmedRegister))
            {
                BackPressed();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;
using static TMPro.TMP_InputField;
#if ENABLE_SPEECH
using yourvrexperience.speech;
#endif

namespace yourvrexperience.VR
{
    public class ScreenVRKeyboardView : BaseScreenView, IScreenView
    {
        public const string ScreenName = "ScreenVRKeyboardView";

        public const string EventScreenVRKeyboardSetNewText = "EventScreenVRKeyboardSetNewText";
        public const string EventScreenVRKeyboardConfirmInput = "EventScreenVRKeyboardConfirmInput";
        public const string EventScreenVRKeyboardSpeechRecognized = "EventScreenVRKeyboardSpeechRecognized";

        [SerializeField] private GameObject contentKeyboard;
        [SerializeField] private Button btnRecordVoice;
        [SerializeField] private GameObject contentProcessing;
        [SerializeField] private Button btnStopRecording;
        [SerializeField] private TextMeshProUGUI textProcessing;

		private GameObject _target;
        private KeyboardManager _keyboardManager;
        private TMP_InputField _inputField;
        private ContentType _typeContent = ContentType.Standard;

        private float _timeToRecord = -1;

        public override void Initialize(params object[] parameters)
        {
            base.Initialize(parameters);

            _keyboardManager = _content.GetComponentInChildren<KeyboardManager>();
			_target = (GameObject)parameters[0];
            if (parameters[1] is TMP_InputField)
            {
                _inputField = (TMP_InputField)parameters[1];
                _keyboardManager.inputText.text = _inputField.text;
                _typeContent = _inputField.contentType;
                if (parameters.Length > 3)
                {
                    _typeContent = (ContentType)parameters[3];
                } 
            }
            else
            {
                _keyboardManager.inputText.text = (string)parameters[1];
                _typeContent = ContentType.Standard;
                if (parameters.Length > 3)
                {
                    _typeContent = (ContentType)parameters[3];
                }                
            }
            _keyboardManager.maxInputLength = (int)parameters[2];
            _keyboardManager.Initialize(_typeContent);

            UIEventController.Instance.Event += OnUIEvent;
            SystemEventController.Instance.Event += OnSystemEvent;

#if ENABLE_SPEECH
            btnRecordVoice.onClick.AddListener(OnRecordMessage);
            btnStopRecording.onClick.AddListener(OnStopRecording);            
            contentProcessing.gameObject.SetActive(false);
#else
            btnRecordVoice.gameObject.SetActive(false);
            contentProcessing.gameObject.SetActive(false);
#endif            
		}

        void OnDestroy()
        {
            Destroy();
        }

        public override void Destroy()
        {
            if (_keyboardManager != null)
            {
                _keyboardManager.Destroy();
                _keyboardManager = null;

                base.Destroy();

                if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
                if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;

				UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
            }
        }

#if ENABLE_SPEECH
        private void OnStopRecording()
        {
            btnRecordVoice.gameObject.SetActive(false);
            btnStopRecording.gameObject.SetActive(false);
            contentProcessing.gameObject.SetActive(true);
            _timeToRecord = -1;
            textProcessing.text = LanguageController.Instance.GetText("vr.keyboard.now.processing");
            SpeechRecognitionController.Instance.ProcessSpeech(EventScreenVRKeyboardSpeechRecognized);
        }

        private void OnRecordMessage()
        {
            contentKeyboard.SetActive(false);
            btnRecordVoice.gameObject.SetActive(false);
            contentProcessing.gameObject.SetActive(true);
            _timeToRecord = 10;
            textProcessing.text = LanguageController.Instance.GetText("vr.keyboard.speak.now") + (int)_timeToRecord + "...";
            SpeechRecognitionController.Instance.StartRecording();
        }
#endif        

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
#if ENABLE_SPEECH            
            if (nameEvent.Equals(SpeechRecognitionController.EventTextToSpeechControllerTimeoutProcessing))
            {
                btnRecordVoice.gameObject.SetActive(true);
                contentProcessing.gameObject.SetActive(false);
                contentKeyboard.SetActive(true);
            }
            if (nameEvent.Equals(EventScreenVRKeyboardSpeechRecognized))
            {
                bool isOk = (bool)parameters[0];
                string resultText = (string)parameters[1];
                GameObject target = (GameObject)parameters[2];
                btnRecordVoice.gameObject.SetActive(true);
                contentProcessing.gameObject.SetActive(false);
                contentKeyboard.SetActive(true);
                _timeToRecord = -1;
                SystemEventController.Instance.ClearSystemEvents(SpeechRecognitionController.EventTextToSpeechControllerTimeoutProcessing);
                if (this.gameObject == target)
                {
                    if (isOk)
                    {
                        _keyboardManager.inputText.text = resultText;
                    }
                }
            }
#endif                    
        }

        private void OnUIEvent(string nameEvent, params object[] parameters)
		{
            if (nameEvent.Equals(EventScreenVRKeyboardConfirmInput))
            {
                string finalText = _keyboardManager.inputText.text;
                Destroy();
                GameObject.Destroy(this.gameObject);
                UIEventController.Instance.DispatchUIEvent(EventScreenVRKeyboardSetNewText, _target, finalText);
            }
		}

        void Update()
        {
#if ENABLE_SPEECH
            if (_timeToRecord > 0)
            {
                _timeToRecord -= Time.deltaTime;
                textProcessing.text = LanguageController.Instance.GetText("vr.keyboard.speak.now") + (int)_timeToRecord + "...";
                if (_timeToRecord < 0)
                {
                    _timeToRecord = -1;                    
                    OnStopRecording();
                }
            }
#endif
        }

    }
}
using yourvrexperience.Utils;
using UnityEngine;
using System;
using System.Collections.Generic;
#if ENABLE_GOOGLE_SPEECH
using FrostweepGames.Plugins.GoogleCloud.SpeechRecognition;
using FrostweepGames.Plugins.GoogleCloud.SpeechRecognition.V1;
using FrostweepGames.Plugins.GoogleCloud.SpeechRecognition.Tools;
using FrostweepGames.Plugins.GoogleCloud.TextToSpeech;
#endif

namespace yourvrexperience.speech
{
    public enum GenderVoice { MALE, FEMALE }

    public enum AgeVoice { KID, TEEN, ADULT, OLD }

    public enum SpeedVoice { SLOW, NORMAL, FAST }

	public class SpeechRecognitionController : MonoBehaviour
	{
        public const bool DEBUG = false;

        public const float TIMEOUT_TO_CANCEL = 8f;

        public const string EventSpeechControllerRecognitionCompleted = "EventSpeechControllerRecognitionCompleted";
        public const string EventTextToSpeechControllerTextPlayed = "EventTextToSpeechControllerTextPlayed";

        public const string EventSpeechControllerSynthesisCompleted = "EventSpeechControllerSynthesisCompleted";

        public const string EventTextToSpeechControllerTimeoutProcessing = "EventTextToSpeechControllerTimeoutProcessing";

        private static SpeechRecognitionController _instance;

        public static SpeechRecognitionController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(SpeechRecognitionController)) as SpeechRecognitionController;
                }
				return _instance;
            }
        }

        public event Action<string> SpeechPlayedCompletedEvent;

        [SerializeField] private bool activated = true;

        private bool _shouldProcessSpeech = true;
        private string _customEventSynthesis = null;
        private string _customEventRecognition = null;

#if ENABLE_GOOGLE_SPEECH
        private GCSpeechRecognition _speechRecognition;
        private GCTextToSpeech _textToSpeech;

        private FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Enumerators.LanguageCode _languageCode;
        private FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Enumerators.VoiceType _voiceType;
        private FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Voice[] _voices;
        private FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Voice _currentVoice;
        private FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Voice _currentVoiceMan;
        private FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Voice _currentVoiceWoman;
#endif
        private string _textToSay;

        void Start()
        {            
            SetUpReferences();

            SystemEventController.Instance.Event += OnSystemEvent;
        }

        private void SetUpReferences()
        {
#if ENABLE_GOOGLE_SPEECH
            if ((_speechRecognition == null) && (_textToSpeech == null))
            {
                // SPEECH TO TEXT
                _speechRecognition = GCSpeechRecognition.Instance;
                _speechRecognition.RecognizeSuccessEvent += RecognizeSuccessEventHandler;
                _speechRecognition.RecognizeFailedEvent += RecognizeFailedEventHandler; 
                _speechRecognition.LongRunningRecognizeSuccessEvent += LongRunningRecognizeSuccessEventHandler;
                _speechRecognition.LongRunningRecognizeFailedEvent += LongRunningRecognizeFailedEventHandler;
                _speechRecognition.FinishedRecordEvent += FinishedRecordEventHandler;
                _speechRecognition.StartedRecordEvent += StartedRecordEventHandler;
                _speechRecognition.RecordFailedEvent += RecordFailedEventHandler;

                _speechRecognition.RequestMicrophonePermission(null);
                MicrophoneDevicesDropdownOnValueChangedEventHandler(0);

                // TEXT TO SPEECH
                _textToSpeech = GCTextToSpeech.Instance;
                _textToSpeech.GetVoicesSuccessEvent += OnGetVoicesSuccessEvent;
                _textToSpeech.SynthesizeSuccessEvent += OnSynthesizeSuccessEvent;
                _textToSpeech.GetVoicesFailedEvent += OnGetVoicesFailedEvent;
                _textToSpeech.SynthesizeFailedEvent += OnSynthesizeFailedEvent;
            }
#endif            
        }

        public void Initialize()
        {
            SetUpReferences();

#if ENABLE_GOOGLE_SPEECH
            InitRecognitionLanguage(LanguageController.Instance.CodeLanguage);

            GetVoicesButtonOnClickHandler(_languageCode);
#endif            
        }

        //////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////
        // SPEECH TO TEXT
        //////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////

        public void InitRecognitionLanguage(string languageCode)
        {
#if ENABLE_GOOGLE_SPEECH            
            switch (languageCode)
            {
                case LanguageController.CodeLanguageEnglish:
                    _languageCode = FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Enumerators.LanguageCode.en_US;
                    _voiceType = FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Enumerators.VoiceType.WAVENET;
                    break;
                case LanguageController.CodeLanguageSpanish:
                    _languageCode = FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Enumerators.LanguageCode.es_ES;
                    _voiceType = FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Enumerators.VoiceType.WAVENET;
                    break;
                case LanguageController.CodeLanguageCatalan:
                    _languageCode = FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Enumerators.LanguageCode.ca_ES;
                    _voiceType = FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Enumerators.VoiceType.STANDARD;
                    break;
                case LanguageController.CodeLanguageFrench:
                    _languageCode = FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Enumerators.LanguageCode.fr_FR;
                    _voiceType = FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Enumerators.VoiceType.WAVENET;
                    break;
            }
#endif            
        }

        public void SetVoiceByLanguage(string languageCode, string voiceManEnglish = "en-US-Wavenet-I", string voiceWomanEnglish = "en-US-Wavenet-H",
                                        string voiceManSpanish = "es-ES-Wavenet-F", string voiceWomanSpanish = "es-ES-Wavenet-H",
                                        string voiceManCatalan = "ca-ES-Standard-B", string voiceWomanCatalan = "ca-ES-Standard-B",
                                        string voiceManFrench = "fr-FR-Wavenet-G", string voiceWomanFrench = "fr-FR-Wavenet-F")
        {
#if ENABLE_GOOGLE_SPEECH                     
            if (_voices != null)
            {
                switch (languageCode)
                {
                    case LanguageController.CodeLanguageEnglish:
                        _currentVoiceMan = GetVoiceByName(voiceManEnglish);
                        _currentVoiceWoman = GetVoiceByName(voiceWomanEnglish);
                        break;
                    case LanguageController.CodeLanguageSpanish:
                        _currentVoiceMan = GetVoiceByName(voiceManSpanish);
                        _currentVoiceWoman = GetVoiceByName(voiceWomanSpanish);
                        break;
                    case LanguageController.CodeLanguageCatalan:
                        _currentVoiceMan = GetVoiceByName(voiceManCatalan);
                        _currentVoiceWoman = GetVoiceByName(voiceWomanCatalan);
                        break;
                    case LanguageController.CodeLanguageFrench:
                        _currentVoiceMan = GetVoiceByName(voiceManFrench);
                        _currentVoiceWoman = GetVoiceByName(voiceWomanFrench);
                        break;
                }
            }
#endif
        }


        void OnDestroy()
        {
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
#if ENABLE_GOOGLE_SPEECH
            if (_speechRecognition != null)
            {
                _speechRecognition.RecognizeSuccessEvent -= RecognizeSuccessEventHandler;
                _speechRecognition.RecognizeFailedEvent -= RecognizeFailedEventHandler;            
                _speechRecognition.LongRunningRecognizeSuccessEvent -= LongRunningRecognizeSuccessEventHandler;
                _speechRecognition.LongRunningRecognizeFailedEvent -= LongRunningRecognizeFailedEventHandler;
                _speechRecognition.FinishedRecordEvent -= FinishedRecordEventHandler;
                _speechRecognition.StartedRecordEvent -= StartedRecordEventHandler;
                _speechRecognition.RecordFailedEvent -= RecordFailedEventHandler;
            }
#endif            
        }

		private void MicrophoneDevicesDropdownOnValueChangedEventHandler(int value)
		{
#if ENABLE_GOOGLE_SPEECH            
			if (!_speechRecognition.HasConnectedMicrophoneDevices())
				return;
			_speechRecognition.SetMicrophoneDevice(_speechRecognition.GetMicrophoneDevices()[value]);
#endif            
		}

        public void StartRecording()
        {
            if (!activated) return;

            yourvrexperience.Utils.Utilities.DebugLogColor("Start recording", Color.red);
#if ENABLE_GOOGLE_SPEECH            
            _speechRecognition.StartRecord(false);
            _customEventRecognition = null;
#endif            
        }

        public void ProcessSpeech(string customEvent = null)
        {
            if (!activated) return;

            yourvrexperience.Utils.Utilities.DebugLogColor("Process speech", Color.red);
#if ENABLE_GOOGLE_SPEECH            
            _customEventRecognition = customEvent;
            _shouldProcessSpeech = true;
            _speechRecognition.StopRecord();
            SystemEventController.Instance.DelaySystemEvent(EventTextToSpeechControllerTimeoutProcessing, TIMEOUT_TO_CANCEL);
#endif            
        }

        public void CancelSpeech()
        {
            yourvrexperience.Utils.Utilities.DebugLogColor("Cancel speech", Color.red);
#if ENABLE_GOOGLE_SPEECH            
            _shouldProcessSpeech = false;
            _speechRecognition.StopRecord();
#endif            
        }

#if ENABLE_GOOGLE_SPEECH
        private void RecognizeFailedEventHandler(string value)
        {
            string resultError = "Speech Recognition Failed["+value+"]";
            Debug.LogError(resultError);
            UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
            if (_customEventRecognition == null)
            {
                SystemEventController.Instance.DispatchSystemEvent(EventSpeechControllerRecognitionCompleted, false, resultError);
            }
            else
            {
                SystemEventController.Instance.DispatchSystemEvent(_customEventRecognition, false, resultError);
            }
        }

        private void RecognizeSuccessEventHandler(RecognitionResponse recognitionResponse)
        {
            if (_shouldProcessSpeech)
            {
                _shouldProcessSpeech = false;
                yourvrexperience.Utils.Utilities.DebugLogColor("Speech Recognition Success", Color.red);
                UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
                if (recognitionResponse == null || recognitionResponse.results.Length == 0)
                {
                    Debug.LogError("Words not detected.");
                    if (_customEventRecognition == null)
                    {
                        SystemEventController.Instance.DispatchSystemEvent(EventSpeechControllerRecognitionCompleted, false, LanguageController.Instance.GetText("screen.speech.recognition.error"));
                    }
                    else
                    {
                        SystemEventController.Instance.DispatchSystemEvent(_customEventRecognition, false, LanguageController.Instance.GetText("screen.speech.recognition.error"));
                    }                    
                    return;
                }

                string speechToTextResult = recognitionResponse.results[0].alternatives[0].transcript;
                if (_customEventRecognition == null)
                {
                    SystemEventController.Instance.DispatchSystemEvent(EventSpeechControllerRecognitionCompleted, true, speechToTextResult);
                }
                else
                {
                    SystemEventController.Instance.DispatchSystemEvent(_customEventRecognition, true, speechToTextResult);
                }                    
            }
        }

        private void LongRunningRecognizeSuccessEventHandler(Operation operation)
        {
            if (_shouldProcessSpeech)
            {
                _shouldProcessSpeech = false;

                if (operation.error != null && !string.IsNullOrEmpty(operation.error.message))
                {
                    Debug.LogError("Long Running Recognize Failed: " + operation.error.message + "; operation: " + operation.name);
                    return;
                }

                yourvrexperience.Utils.Utilities.DebugLogColor("Long Running Recognize Success.\n Operation name: " + operation.name, Color.red);

                if (operation.done)
                {
                    if (operation.response != null && operation.response.results.Length > 0)
                    {
                        string speechToTextResult = operation.response.results[0].alternatives[0].transcript;
                        if (_customEventRecognition == null)
                        {
                            SystemEventController.Instance.DispatchSystemEvent(EventSpeechControllerRecognitionCompleted, true, speechToTextResult);
                        }
                        else
                        {
                            SystemEventController.Instance.DispatchSystemEvent(_customEventRecognition, true, speechToTextResult);
                        }                    
                    }
                }
            }
        }

		private void LongRunningRecognizeFailedEventHandler(string error)
		{
			Debug.LogError("Long Running Recognize Failed: " + error);
		}

        private void RecordFailedEventHandler()
        {
            Debug.LogError("Record failed");
        }

        private void StartedRecordEventHandler()
        {
            yourvrexperience.Utils.Utilities.DebugLogColor("Record started", Color.red);
        }

        private void FinishedRecordEventHandler(AudioClip clip, float[] raw)
        {
            yourvrexperience.Utils.Utilities.DebugLogColor("Record finished", Color.red);
			if (clip == null) return;
            if (!_shouldProcessSpeech) return;

			RecognitionConfig config = RecognitionConfig.GetDefault();
            switch (LanguageController.Instance.CodeLanguage)
            {
                case LanguageController.CodeLanguageEnglish:
                    config.languageCode = "en-US";
                    break;
                case LanguageController.CodeLanguageSpanish:
                    config.languageCode = "es-ES";
                    break;
                case LanguageController.CodeLanguageCatalan:
                    config.languageCode = "ca-ES";
                    break;
                case LanguageController.CodeLanguageFrench:
                    config.languageCode = "fr-FR";
                    break;
            }
			config.audioChannelCount = clip.channels;

            // GeneralRecognitionRequest recognitionRequest = new LongRunningRecognitionRequest();
            GeneralRecognitionRequest recognitionRequest = new GeneralRecognitionRequest();

			recognitionRequest.audio = new RecognitionAudioContent()
            {
				content = raw.ToBase64(channels: clip.channels)
			};
			recognitionRequest.config = config;

            // _speechRecognition.LongRunningRecognize(recognitionRequest);
            _speechRecognition.Recognize(recognitionRequest);
        }
#endif
        //////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////
        // TEXT TO SPEECH
        //////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////

#if ENABLE_GOOGLE_SPEECH
        private void GetVoicesButtonOnClickHandler(FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Enumerators.LanguageCode languageCode)
        {
            _textToSpeech.GetVoices(new GetVoicesRequest()
            {
                languageCode = _textToSpeech.PrepareLanguage(languageCode)
            });            
        }
#endif
        public void Synthetize(string text, GenderVoice gender, AgeVoice age, SpeedVoice speed, string customEvent = null)
        {
            if (!activated) return;
            
            if (DEBUG) Debug.LogError("Synthetize::TEXT="+text);
            
            _customEventSynthesis = customEvent;
            _textToSay = text;
            bool ssml = false;
            double pitch = 0.0; 
            double speakingRate = 1.0; 
#if ENABLE_GOOGLE_SPEECH            
            double sampleRateHertz = FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Constants.DEFAULT_SAMPLE_RATE;
            switch (gender)
            {
                case GenderVoice.MALE:
                    _currentVoice = _currentVoiceMan;
                    _currentVoice.ssmlGender = FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Enumerators.SsmlVoiceGender.MALE;
                    break;

                case GenderVoice.FEMALE:
                    _currentVoice = _currentVoiceWoman;
                    _currentVoice.ssmlGender = FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Enumerators.SsmlVoiceGender.FEMALE;
                    break;
            }
            switch (age)
            {
                case AgeVoice.KID:
                    sampleRateHertz = 30000;
                    break;

                case AgeVoice.TEEN:
                    switch (gender)
                    {
                        case GenderVoice.MALE:
                            sampleRateHertz = 14000;
                            break;

                        case GenderVoice.FEMALE:
                            sampleRateHertz = 22000;
                            break;                        
                    }
                    break;

                case AgeVoice.ADULT:
                    switch (gender)
                    {
                        case GenderVoice.MALE:
                            sampleRateHertz = 12500;
                            break;

                        case GenderVoice.FEMALE:
                            sampleRateHertz = 20000;
                            break;                        
                    }
                    break;

                case AgeVoice.OLD:
                    switch (gender)
                    {
                        case GenderVoice.MALE:
                            sampleRateHertz = 11500;
                            break;

                        case GenderVoice.FEMALE:
                            sampleRateHertz = 18000;
                            break;
                    }
                    break;
            }
            switch (speed)
            {
                case SpeedVoice.SLOW:
                    speakingRate = 0.8f; 
                    break;

                case SpeedVoice.NORMAL:
                    speakingRate = 1.0f; 
                    break;

                case SpeedVoice.FAST:
                    speakingRate = 1.2f; 
                    break;
            }
            _textToSpeech.Synthesize(_textToSay, new VoiceConfig()
            {
                gender = _currentVoice.ssmlGender,
                languageCode = _currentVoice.languageCodes[0],
                name = _currentVoice.name
            },
            ssml, pitch, speakingRate, sampleRateHertz, new FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Enumerators.EffectsProfileId[] { });
#endif            
        }

#if ENABLE_GOOGLE_SPEECH
        private void OnSynthesizeFailedEvent(string message, long code)
        {
            if (DEBUG) Debug.LogError("OnSynthesizeFailedEvent::FAILED TO SYNTHESIZE::TEXT="+_textToSay);
            if (_customEventSynthesis == null)
            {
                SystemEventController.Instance.DispatchSystemEvent(EventSpeechControllerSynthesisCompleted, false);
            }
            else
            {
                SystemEventController.Instance.DispatchSystemEvent(_customEventSynthesis, false);
            }
        }

        private void OnGetVoicesFailedEvent(string message, long code)
        {
            if (DEBUG) Debug.LogError("OnGetVoicesFailedEvent::FAILED TO RETRIEVE THE VOICES::message="+message);
        }

        private void OnSynthesizeSuccessEvent(PostSynthesizeResponse response, long code)
        {
            AudioClip result = _textToSpeech.GetAudioClipFromBase64(response.audioContent, FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Constants.DEFAULT_AUDIO_ENCODING);
            if (DEBUG) Debug.LogError("OnSynthesizeSuccessEvent::SUCCESS::AUDIO["+result.length+"]::TEXT="+_textToSay);
            if (_customEventSynthesis == null)
            {
                SystemEventController.Instance.DispatchSystemEvent(EventSpeechControllerSynthesisCompleted, true, _textToSay, result);
            }
            else
            {
                SystemEventController.Instance.DispatchSystemEvent(_customEventSynthesis, true, _textToSay, result);
            }            
        }

        private void OnGetVoicesSuccessEvent(GetVoicesResponse response, long code)
        {
            _voices = response.voices;
            if (DEBUG) 
            {
                foreach (FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Voice item in _voices) Debug.LogError("NAME VOICE="+item.name);
            }            
            SetVoiceByLanguage(LanguageController.Instance.CodeLanguage);
        }

        private FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Voice GetVoiceByName(string nameVoice)
        {
            foreach (FrostweepGames.Plugins.GoogleCloud.TextToSpeech.Voice voice in _voices)
            {
                if (voice.name.Equals(nameVoice))
                {
                    return voice;
                }
            }
            return null;
        }
#endif

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(EventTextToSpeechControllerTimeoutProcessing))
            {
                if (_shouldProcessSpeech)
                {
                    _shouldProcessSpeech = false;
                    if (_customEventRecognition == null)
                    {
                        SystemEventController.Instance.DispatchSystemEvent(EventSpeechControllerRecognitionCompleted, false, LanguageController.Instance.GetText("screen.speech.recognition.error"));
                    }
                    else
                    {
                        SystemEventController.Instance.DispatchSystemEvent(_customEventRecognition, false, LanguageController.Instance.GetText("screen.speech.recognition.error"));
                    }                    
                }
            }
        }
	}
}
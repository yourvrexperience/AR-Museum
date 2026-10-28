using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.ai;
using yourvrexperience.Narration;
using yourvrexperience.speech;
using yourvrexperience.Utils;
using static yourvrexperience.Narration.NarrationCreator;
using static yourvrexperience.Utils.SoundsController;

namespace yourvrexperience.template6dof
{
    public class ItemNarrationSegment : MonoBehaviour, ISlotView
    {
  		[SerializeField] private TextMeshProUGUI languageTitle;
		[SerializeField] private CustomInput languageInput;

		[SerializeField] private Button languageVoiceRecognition;
		[SerializeField] private Button languageAITranslation;
		[SerializeField] private Button languageAISpeech;
		[SerializeField] private Button languageAIUpload;
		
		[SerializeField] private GameObject iconPlay;
		[SerializeField] private GameObject iconStop;
		
		private bool _speechProcessingRequested = false;
		private bool _askedForTranslation = false;
		private int _speechIdRequested = -1;
		private string _languageCode;
		private NarrationCreatorToken _selectedEntry;
		private NarrationCreatorData _narrationPOI;
		private ScreenXMLEditSegmentView _screenSegments;
        private string _textRequested;
        private string _voiceRequested;
        private bool _isBaseLanguage = false;
        private int _secretId = -1;

        public ItemMultiObjectEntry Data => throw new NotImplementedException();

        public void Initialize(params object[] parameters)
        {
            _screenSegments = (ScreenXMLEditSegmentView)((ItemMultiObjectEntry)parameters[0]).Objects[0];
            _languageCode = (string)((ItemMultiObjectEntry)parameters[0]).Objects[1];
            _selectedEntry = (NarrationCreatorToken)((ItemMultiObjectEntry)parameters[0]).Objects[2];
            _narrationPOI = (NarrationCreatorData)((ItemMultiObjectEntry)parameters[0]).Objects[3];
            _secretId = (int)((ItemMultiObjectEntry)parameters[0]).Objects[4];
            
			languageTitle.text = LanguageController.Instance.GetText("screen.edit.segment.title.language."+_languageCode);
			languageInput.text = _selectedEntry.GetMessage().GetText(_languageCode);
            languageInput.onValueChanged.AddListener(OnInputTextChanged);
			
			languageVoiceRecognition.onClick.AddListener(OnVoiceRecognition);
			languageAITranslation.onClick.AddListener(OnAILanguageTranslation);
			languageAISpeech.onClick.AddListener(OnAILanguageSpeech);
			languageAIUpload.onClick.AddListener(OnAILanguageUpload);
			
#if UNITY_WEBGL || UNITY_EDITOR
			languageAIUpload.gameObject.SetActive(true);
#else
			languageAIUpload.gameObject.SetActive(false);
#endif
			StopAllPlayButton();

			_isBaseLanguage = _languageCode == LanguageController.Instance.CodeLanguage;
            if (_isBaseLanguage)
			{
                _screenSegments.SetBaseText(languageInput.text);
                _screenSegments.SetBaseLanguage(languageTitle.text);
				languageVoiceRecognition.gameObject.SetActive(true);
			}
			else
			{
				languageVoiceRecognition.gameObject.SetActive(false);			
			}
			
#if UNITY_WEBGL && !UNITY_EDITOR
			languageVoiceRecognition.gameObject.SetActive(false);
#endif

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL || ENABLE_NIANTICXR || ENABLE_QR_XR || ENABLE_QR_META
			languageInput.OnFocusDownEvent += OnFocusLanguageInput;
			if (languageInput != null) languageInput.OnFocusEvent -= OnFocusLanguageInput;
			
#endif

			if (_selectedEntry.IsTitle())
			{
				languageAISpeech.gameObject.SetActive(false);
				languageAIUpload.gameObject.SetActive(false);
				if (_isBaseLanguage)
				{
					languageVoiceRecognition.gameObject.SetActive(false);
					languageAITranslation.gameObject.SetActive(false);
				}
			}
			else
			{
				languageAISpeech.gameObject.SetActive(true);			
				languageAIUpload.gameObject.SetActive(true);				
			}

            SystemEventController.Instance.Event += OnSystemEvent;

            LanguageController.Instance.SetFontAsset(this.transform);
            if (_languageCode == LanguageController.CodeLanguageChinese)
            {                
                LanguageController.Instance.SetFontAsset(this.transform, LanguageController.CodeLanguageChinese);
                languageInput.text = _selectedEntry.GetMessage().GetText(_languageCode);
            }
            if (_languageCode == LanguageController.CodeLanguageJapanese)
            {                
                LanguageController.Instance.SetFontAsset(this.transform, LanguageController.CodeLanguageJapanese);
                languageInput.text = _selectedEntry.GetMessage().GetText(_languageCode);
            }                        
            if (_languageCode == LanguageController.CodeLanguageArabic)
            {                
                LanguageController.Instance.SetFontAsset(this.transform, LanguageController.CodeLanguageArabic);
                languageInput.text = _selectedEntry.GetMessage().GetText(_languageCode);
            }                        
			if (_languageCode == LanguageController.CodeLanguageUrdu)
			{
                LanguageController.Instance.SetFontAsset(this.transform, LanguageController.CodeLanguageUrdu);
                languageInput.text = _selectedEntry.GetMessage().GetText(_languageCode);
			}
            if (_languageCode == LanguageController.CodeLanguageKorean)
            {                
                LanguageController.Instance.SetFontAsset(this.transform, LanguageController.CodeLanguageKorean);
                languageInput.text = _selectedEntry.GetMessage().GetText(_languageCode);
            }                        
        }

        void OnDestroy()
        {
            Destroy();
        }

        public bool Destroy()
        {
            if (_screenSegments != null)
            {
                _screenSegments = null;
                _selectedEntry = null;
                _narrationPOI = null;
                if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
                return true;
            }
            else
            {
                return false;
            }
        }

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL || ENABLE_NIANTICXR || ENABLE_QR_XR || ENABLE_QR_META
		private void OnFocusLanguageInput()
		{
			PanelInputTextAction inputActionText = MainController.Instance.CreateInputActionEditText();
			if (inputActionText != null) inputActionText.InputDescriptionObject = languageInput;
		}
#endif
		
		private void StopAllPlayButton()
		{
			iconPlay.SetActive(true);
			iconStop.SetActive(false);
		}
		
        private void OnInputTextChanged(string value)
        {
        	_selectedEntry.GetMessage().SetText(_languageCode, value);	
            if (_isBaseLanguage)
            {
                _screenSegments.SetBaseText(value);
            }
        }
		
		private void ActivatePlayButton(bool isPlaying)
		{
			iconPlay.SetActive(!isPlaying);
			iconStop.SetActive(isPlaying);
		}
		
		private bool GetStatePlayButton()
		{
			return iconStop.activeSelf;
		}
				
        private void OnAILanguageSpeech()
        {
			if (GetStatePlayButton())
			{
				SystemEventController.Instance.DispatchSystemEvent(ScreenXMLEditSegmentView.EventScreenXMLEditSegmentViewStopPlaying);
				SoundsController.Instance.StopAllSounds();
				return;
			}
			SystemEventController.Instance.DispatchSystemEvent(ScreenXMLEditSegmentView.EventScreenXMLEditSegmentViewStopPlaying);
			SoundsController.Instance.StopAllSounds();
			ActivatePlayButton(true);
			OnAIGetSpeech(_languageCode, languageInput.text);
        }
				
		private void OnAIGetSpeech(string languageCode, string textfield)
		{
			AudioClip speech = null;
			SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerStopNarrations);
			if (textfield.Length > 1)
			{
				bool isTextDifferentFromSpeech = false;
				if (!_selectedEntry.GetMessage().GetModified(languageCode))
				{					
					speech = SpeechDatabaseController.Instance.GetSpeechDataByID(_secretId, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id, languageCode);
				}
				else
				{
					isTextDifferentFromSpeech = true;
				}				
				if (speech == null)
				{
					if (_screenSegments.CheckAIAdminOperationAllowed())
					{
						_speechIdRequested = SpeechDatabaseController.Instance.GetSpeechID(_secretId, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id, languageCode);
						_voiceRequested = LanguageController.Instance.GetNarrationVoice(languageCode);
						_textRequested = textfield;
						ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, "", LanguageController.Instance.GetText("message.please.wait"));
						if (isTextDifferentFromSpeech)
						{
							SpeechDatabaseController.Instance.RegisterNewSpeech(_textRequested, new ItemMultiObjectEntry(_secretId, _textRequested, _voiceRequested, _languageCode, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id));
						}
						else
						{
							SystemEventController.Instance.DispatchSystemEvent(SpeechDatabaseController.EventSpeechDatabaseControllerDownloadSpeech, _secretId, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id, languageCode);
						}					
					}
				}
				else
				{
					SystemEventController.Instance.DelaySystemEvent(ScreenXMLEditSegmentView.EventScreenXMLEditSegmentViewStopPlaying, speech.length);					
					SoundsController.Instance.PlaySoundClipFx(ChannelsAudio.FX1, speech, false, 1);
				}
			}
		}
		
        private void OnAILanguageUpload()
        {
			if (languageInput.text.Length == 0)
			{
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, LanguageController.Instance.GetText("text.error"), LanguageController.Instance.GetText("screen.edit.segment.provide.text"));				
			}
			else
			{
			    SystemEventController.Instance.DispatchSystemEvent(ScreenXMLEditSegmentView.EventScreenXMLEditSegmentViewStopPlaying);
				SoundsController.Instance.StopAllSounds();
				SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerStopNarrations);
				_speechIdRequested = SpeechDatabaseController.Instance.GetSpeechID(_secretId, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id, LanguageController.CodeLanguageEnglish);
				_textRequested = languageInput.text;
				SpeechDatabaseController.Instance.OnUploadSpeech(languageInput.text, _secretId, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id, LanguageController.CodeLanguageEnglish);
			}            
        }

        private void OnAILanguageTranslation()
        {
			if (_screenSegments.CheckAIAdminOperationAllowed())
			{
				string sourceTextToTranslate = _screenSegments.GetBaseText();
				if (sourceTextToTranslate.Length > 10)
				{
					string textRequest = "Please, translate the next text from "+_screenSegments.GetBaseLanguage()+" to "+languageTitle.text+". Give me only the translated text: \n" + sourceTextToTranslate;
					_askedForTranslation = true;
					ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, "", LanguageController.Instance.GetText("message.please.wait"));
					AskForTranslation(textRequest);
				}
			}
        }

        public void OnVoiceRecognition()
        {
			if (_screenSegments.CheckAIAdminOperationAllowed())
			{
				SystemEventController.Instance.DispatchSystemEvent(ScreenXMLEditSegmentView.EventScreenXMLEditSegmentViewStopPlaying);
				SoundsController.Instance.StopAllSounds();
                _speechProcessingRequested = true;
				_screenSegments.OnStartRecording();
			}
        }

        private void AskForTranslation(string text)
		{
			GameAIData.Instance.AskGenericQuestionAI("", text, false, ScreenXMLEditSegmentView.EventScreenXMLEditSegmentViewTranslationCompleted);
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
#if ENABLE_SPEECH
            if (_speechProcessingRequested)
            {
                if (nameEvent.Equals(SpeechRecognitionController.EventTextToSpeechControllerTimeoutProcessing))
                {
                    _speechProcessingRequested = false;
                    _screenSegments.ShowContentProcessing(false);
                }
                if (nameEvent.Equals(ScreenXMLEditSegmentView.EventScreenXMLEditSegmentViewSpeechRecognized))
                {
                    _speechProcessingRequested = false;
                    bool isOk = (bool)parameters[0];
                    string resultText = (string)parameters[1];
                    _screenSegments.ResetContentProcessing();
                    _screenSegments.ShowContentProcessing(false);
                    if (isOk)
                    {
                        languageInput.text = resultText;
                    }
                }
            }
#endif
			if (nameEvent.Equals(ScreenXMLEditSegmentView.EventScreenXMLEditSegmentViewTranslationCompleted))
			{
				if ((bool)parameters[0])
				{
					string translatedText = (string)parameters[1];
					if (_askedForTranslation)
					{
						_askedForTranslation = false;
						_screenSegments.ResetContentProcessing();
						_screenSegments.ShowContentProcessing(false);
						UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
						languageInput.text = translatedText;						
					}
				}
			}
            if (nameEvent.Equals(SpeechDatabaseController.EventSpeechDatabaseControllerAvailableSpeech))
            {
                if (_speechIdRequested != -1)
                {					
                    int speechId = (int)parameters[0];
                    if (_speechIdRequested == speechId)
                    {
                        if ((bool)parameters[1])
                        {
                            _speechIdRequested = -1;
                            _selectedEntry.GetMessage().ResetModified(_languageCode);
                            UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
                            AudioClip speechDownloaded = SpeechDatabaseController.Instance.GetSpeechDataByID(_secretId, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id, _languageCode);
                            if (speechDownloaded != null)
                            {								
                                SystemEventController.Instance.DelaySystemEvent(ScreenXMLEditSegmentView.EventScreenXMLEditSegmentViewStopPlaying, speechDownloaded.length);
                                SoundsController.Instance.PlaySoundClipFx(ChannelsAudio.FX1, speechDownloaded, false, 1);
                                _selectedEntry.GetAudio().SetText(_languageCode, speechDownloaded.length.ToString());
                            }
                            return;
                        }
                    }
                    SpeechDatabaseController.Instance.RegisterNewSpeech(_textRequested, new ItemMultiObjectEntry(_secretId, _textRequested, _voiceRequested, _languageCode, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id));
                }
            }
            if (nameEvent.Equals(ScreenXMLEditSegmentView.EventScreenXMLEditSegmentViewStopPlaying))
            {
                StopAllPlayButton();
            }
            if (nameEvent.Equals(ScreenXMLEditSegmentView.EventScreenXMLEditSegmentViewForcePlaying))
            {
                string languageCode = (string)parameters[0];
                ActivatePlayButton(true);
            }            
		}

        public void ItemSelected(bool dispatchEvent = true)
        {
            
        }

        public void ApplyGenericAction(params object[] parameters)
        {
            
        }
    }
}
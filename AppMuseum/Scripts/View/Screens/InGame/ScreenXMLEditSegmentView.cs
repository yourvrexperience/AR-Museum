using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Narration;
using yourvrexperience.Networking;
using yourvrexperience.Utils;
using yourvrexperience.VR;
using static yourvrexperience.Narration.NarrationCreator;
using yourvrexperience.ai;
using static yourvrexperience.ai.NewConversationChatGPTHTTP;
using static yourvrexperience.Utils.SoundsController;
using static yourvrexperience.template6dof.LevelView;

#if ENABLE_SPEECH
using yourvrexperience.speech;
#endif

namespace yourvrexperience.template6dof
{
	public class ScreenXMLEditSegmentView : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenXMLEditSegmentView";
		public const string EventScreenXMLEditSegmentViewRequestAI = "EventScreenXMLEditSegmentViewRequestAI";
		public const string EventScreenXMLEditSegmentViewSpeechRecognized = "EventScreenXMLEditSegmentViewSpeechRecognized";
		public const string EventScreenXMLEditSegmentViewStopPlaying = "EventScreenXMLEditSegmentViewStopPlaying";
		public const string EventScreenXMLEditSegmentViewForcePlaying = "EventScreenXMLEditSegmentViewForcePlaying";

		public const string EventScreenXMLEditSegmentViewTranslationCompleted = "EventScreenXMLEditSegmentViewTranslationCompleted";

		public const int TOTAL_CHARACTERS_SECTION = 300;
		public const int TOTAL_TIMEOUT_AI_PROCESS = 20;

		[SerializeField] private Button buttonExit;

		[SerializeField] private TextMeshProUGUI eventStartTitle;
		[SerializeField] private CustomInput eventStartInput;
		[SerializeField] private TextMeshProUGUI eventEndTitle;
		[SerializeField] private CustomInput eventEndInput;

		[SerializeField] private TextMeshProUGUI englishTitle;
		[SerializeField] private CustomInput englishInput;
		
		[SerializeField] private TextMeshProUGUI spanishTitle;
		[SerializeField] private CustomInput spanishInput;

		[SerializeField] private TextMeshProUGUI frenchTitle;
		[SerializeField] private CustomInput frenchInput;
		
		[SerializeField] private TextMeshProUGUI catalanTitle;
		[SerializeField] private CustomInput catalanInput;

		[SerializeField] private TextMeshProUGUI addImageTitle;
		[SerializeField] private TextMeshProUGUI addVideoTitle;
		[SerializeField] private TextMeshProUGUI add3DTitle;
		[SerializeField] private TextMeshProUGUI addMusicTitle;
		[SerializeField] private TextMeshProUGUI addInteractionTitle;
		[SerializeField] private TextMeshProUGUI addWaypointsTitle;

		[SerializeField] private Button addImageButton;
		[SerializeField] private Button addVideoButton;
		[SerializeField] private Button add3DButton;
		[SerializeField] private Button addMusicButton;
		[SerializeField] private Button addInteractionButton;
		[SerializeField] private Button addWaypointsButton;

		[SerializeField] private Toggle toggleHideGuide;
		[SerializeField] private Toggle toggleDestroyPrevious;
		[SerializeField] private Toggle togglePauseNarration;

		[SerializeField] private Button englishVoiceRecognition;
		[SerializeField] private Button englishAITranslation;
		[SerializeField] private Button englishAISpeech;
		[SerializeField] private Button englishAIUpload;

		[SerializeField] private Button spanishVoiceRecognition;
		[SerializeField] private Button spanishAITranslation;
		[SerializeField] private Button spanishAISpeech;
		[SerializeField] private Button spanishAIUpload;

		[SerializeField] private Button catalanVoiceRecognition;
		[SerializeField] private Button catalanAITranslation;
		[SerializeField] private Button catalanAISpeech;
		[SerializeField] private Button catalanAIUpload;

		[SerializeField] private Button frenchVoiceRecognition;
		[SerializeField] private Button frenchAITranslation;
		[SerializeField] private Button frenchAISpeech;
		[SerializeField] private Button frenchAIUpload;

		[SerializeField] private GameObject contentProcessing;
		[SerializeField] private TextMeshProUGUI infoTextProcessing;
		[SerializeField] private Button stopRecording;

		private NarrationCreatorToken _selectedEntry;
		private NarrationCreatorData _narrationPOI;

		private float _timeToRecord = -1;
		private float _timeToProcess = -1;
		private bool _askedForTranslation = false;
		private string _codeLanguageTranlation = "";
		private bool _speechProcessingRequested = false;
		private int _speechIdRequested = -1;
		private string _textRequested = "";
		private string _languageRequested = "";
		private string _voiceRequested = "";
		private EasterEgg _narrationSecret = null;
		private int _secretId = -1;
		private Dictionary<string, List<GameObject>> _audioPlayers = new Dictionary<string, List<GameObject>>();
		private bool _aiOperationPerformed = false;
		
		public override string NameScreen
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			_selectedEntry = (NarrationCreatorToken)parameters[0];
			_narrationPOI = (NarrationCreatorData)parameters[1];
			if (parameters.Length > 2)
			{
				_narrationSecret = (EasterEgg)parameters[2];
				_secretId = _narrationSecret.Index;
			}
			else
			{
				_secretId = -1;
			}

			buttonExit.onClick.AddListener(OnButtonExit);

			eventStartTitle.text = LanguageController.Instance.GetText("screen.edit.segment.start.event");
			eventEndTitle.text = LanguageController.Instance.GetText("screen.edit.segment.end.event");

			eventStartInput.text = _selectedEntry.StartEvent;
			eventEndInput.text = _selectedEntry.EndEvent;

			eventStartInput.onValueChanged.AddListener(OnStartEventChanged);
			eventEndInput.onValueChanged.AddListener(OnEndEventChanged);

			englishTitle.text = LanguageController.Instance.GetText("screen.edit.segment.title.language.english");
			spanishTitle.text = LanguageController.Instance.GetText("screen.edit.segment.title.language.spanish");
			catalanTitle.text = LanguageController.Instance.GetText("screen.edit.segment.title.language.catalan");
			frenchTitle.text = LanguageController.Instance.GetText("screen.edit.segment.title.language.french");

			englishInput.text = _selectedEntry.GetMessage().GetText(LanguageController.CodeLanguageEnglish);
			spanishInput.text = _selectedEntry.GetMessage().GetText(LanguageController.CodeLanguageSpanish);
			catalanInput.text = _selectedEntry.GetMessage().GetText(LanguageController.CodeLanguageCatalan);
			frenchInput.text = _selectedEntry.GetMessage().GetText(LanguageController.CodeLanguageFrench);

			englishVoiceRecognition.gameObject.SetActive(false);
			englishAITranslation.gameObject.SetActive(false);
			spanishVoiceRecognition.gameObject.SetActive(false);
			spanishAITranslation.gameObject.SetActive(false);
			catalanVoiceRecognition.gameObject.SetActive(false);
			catalanAITranslation.gameObject.SetActive(false);
			frenchVoiceRecognition.gameObject.SetActive(false);
			frenchAITranslation.gameObject.SetActive(false);

			englishVoiceRecognition.onClick.AddListener(OnVoiceRecognition);
			englishAITranslation.onClick.AddListener(OnAIEnglishTranslation);
			englishAISpeech.onClick.AddListener(OnAIEnglishSpeech);
			englishAIUpload.onClick.AddListener(OnAIEnglishUpload);			

			spanishVoiceRecognition.onClick.AddListener(OnVoiceRecognition);
			spanishAITranslation.onClick.AddListener(OnAISpanishTranslation);
			spanishAISpeech.onClick.AddListener(OnAISpanishSpeech);
			spanishAIUpload.onClick.AddListener(OnAISpanishUpload);

			catalanVoiceRecognition.onClick.AddListener(OnVoiceRecognition);
			catalanAITranslation.onClick.AddListener(OnAICatalanTranslation);
			catalanAISpeech.onClick.AddListener(OnAICatalanSpeech);
			catalanAIUpload.onClick.AddListener(OnAICatalanUpload);

			frenchVoiceRecognition.onClick.AddListener(OnVoiceRecognition);
			frenchAITranslation.onClick.AddListener(OnAIFrenchTranslation);
			frenchAISpeech.onClick.AddListener(OnAIFrenchSpeech);
			frenchAIUpload.onClick.AddListener(OnAIFrenchUpload);

#if UNITY_WEBGL || UNITY_EDITOR
			englishAIUpload.gameObject.SetActive(true);
			spanishAIUpload.gameObject.SetActive(true);
			catalanAIUpload.gameObject.SetActive(true);
			frenchAIUpload.gameObject.SetActive(true);
#else
			englishAIUpload.gameObject.SetActive(false);
			spanishAIUpload.gameObject.SetActive(false);
			catalanAIUpload.gameObject.SetActive(false);
			frenchAIUpload.gameObject.SetActive(false);			
#endif

			SetUpPlayButton(englishAISpeech, LanguageController.CodeLanguageEnglish);
			SetUpPlayButton(spanishAISpeech, LanguageController.CodeLanguageSpanish);
			SetUpPlayButton(catalanAISpeech, LanguageController.CodeLanguageCatalan);
			SetUpPlayButton(frenchAISpeech, LanguageController.CodeLanguageFrench);
			StopAllPlayButtons();

			switch (LanguageController.Instance.CodeLanguage)
			{
				case LanguageController.CodeLanguageEnglish:
#if ENABLE_SPEECH				
					englishVoiceRecognition.gameObject.SetActive(true);
#endif					
					spanishAITranslation.gameObject.SetActive(true);
					catalanAITranslation.gameObject.SetActive(true);
					frenchAITranslation.gameObject.SetActive(true);
					break;
				case LanguageController.CodeLanguageSpanish:
#if ENABLE_SPEECH								
					spanishVoiceRecognition.gameObject.SetActive(true);
#endif					
					englishAITranslation.gameObject.SetActive(true);
					catalanAITranslation.gameObject.SetActive(true);
					frenchAITranslation.gameObject.SetActive(true);
					break;
				case LanguageController.CodeLanguageCatalan:
#if ENABLE_SPEECH								
					catalanVoiceRecognition.gameObject.SetActive(true);
#endif					
					spanishAITranslation.gameObject.SetActive(true);
					englishAITranslation.gameObject.SetActive(true);
					frenchAITranslation.gameObject.SetActive(true);
					break;
				case LanguageController.CodeLanguageFrench:
#if ENABLE_SPEECH								
					frenchVoiceRecognition.gameObject.SetActive(true);
#endif					
					spanishAITranslation.gameObject.SetActive(true);
					englishAITranslation.gameObject.SetActive(true);
					catalanAITranslation.gameObject.SetActive(true);
					break;
			}

#if UNITY_WEBGL && !UNITY_EDITOR
			englishVoiceRecognition.gameObject.SetActive(false);
			spanishVoiceRecognition.gameObject.SetActive(false);
			catalanVoiceRecognition.gameObject.SetActive(false);
			frenchVoiceRecognition.gameObject.SetActive(false);
#endif

			englishInput.onValueChanged.AddListener(OnEnglishChanged);
			spanishInput.onValueChanged.AddListener(OnSpanishChanged);
			catalanInput.onValueChanged.AddListener(OnCatalanChanged);
			frenchInput.onValueChanged.AddListener(OnFrenchChanged);

			UIEventController.Instance.Event += OnUIEvent;
			SystemEventController.Instance.Event += OnSystemEvent;

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL		
			eventStartInput.OnFocusEvent += OnFocusEventStartInput;
			eventEndInput.OnFocusEvent += OnFocusEventEndInput;
			englishInput.OnFocusDownEvent += OnFocusEnglishInput;
			spanishInput.OnFocusDownEvent += OnFocusSpanishInput;
			catalanInput.OnFocusDownEvent += OnFocusCatalanInput;
			frenchInput.OnFocusDownEvent += OnFocusFrenchInput;
#endif
			if (_selectedEntry.IsTitle())
			{
				addImageTitle.gameObject.SetActive(false);
				addVideoTitle.gameObject.SetActive(false);
				add3DTitle.gameObject.SetActive(false);
				addMusicTitle.gameObject.SetActive(false);
				addInteractionTitle.gameObject.SetActive(false);
				addWaypointsTitle.gameObject.SetActive(false);

				addImageButton.gameObject.SetActive(false);
				addVideoButton.gameObject.SetActive(false);
				add3DButton.gameObject.SetActive(false);
				addMusicButton.gameObject.SetActive(false);
				addInteractionButton.gameObject.SetActive(false);
				addWaypointsButton.gameObject.SetActive(false);

				toggleHideGuide.gameObject.SetActive(false);
				toggleDestroyPrevious.gameObject.SetActive(false);
				togglePauseNarration.gameObject.SetActive(false);

				englishAISpeech.gameObject.SetActive(false);
				spanishAISpeech.gameObject.SetActive(false);
				catalanAISpeech.gameObject.SetActive(false);
				frenchAISpeech.gameObject.SetActive(false);

				englishAIUpload.gameObject.SetActive(false);
				spanishAIUpload.gameObject.SetActive(false);
				catalanAIUpload.gameObject.SetActive(false);
				frenchAIUpload.gameObject.SetActive(false);
			}
			else
			{
				addImageTitle.text = LanguageController.Instance.GetText("screen.edit.segment.title.add.image");
				addVideoTitle.text = LanguageController.Instance.GetText("screen.edit.segment.title.add.video");
				add3DTitle.text = LanguageController.Instance.GetText("screen.edit.segment.title.add.3d.model");
				addMusicTitle.text = LanguageController.Instance.GetText("screen.edit.segment.title.add.music");
				addInteractionTitle.text = LanguageController.Instance.GetText("screen.edit.segment.title.add.interactable");
				addWaypointsTitle.text = LanguageController.Instance.GetText("screen.edit.segment.title.add.waypoints");

				addImageButton.onClick.AddListener(OnAddImage);
				addVideoButton.onClick.AddListener(OnAddVideo);
				add3DButton.onClick.AddListener(OnAdd3DModel);
				addMusicButton.onClick.AddListener(OnAddMusic);
				addInteractionButton.onClick.AddListener(OnAddInteractable);
				addWaypointsButton.onClick.AddListener(OnAddWaypoints);

				toggleHideGuide.isOn = _selectedEntry.ShouldHideGuide;
				toggleDestroyPrevious.isOn = _selectedEntry.ShouldDestroy;
				togglePauseNarration.isOn = _selectedEntry.ShouldPause;

				toggleHideGuide.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.edit.segment.toggle.hide.guide");
				toggleDestroyPrevious.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.edit.segment.toggle.destroy.previous");
				togglePauseNarration.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.edit.segment.toggle.pause.narration");

				toggleHideGuide.onValueChanged.AddListener(OnHideGuide);
				toggleDestroyPrevious.onValueChanged.AddListener(OnDestroyPrevious);
				togglePauseNarration.onValueChanged.AddListener(OnPauseNarration);
			}

			contentProcessing.gameObject.SetActive(false);	
			infoTextProcessing.text = "";
			stopRecording.onClick.AddListener(OnStopRecording);
		}

        public override void Destroy()
		{
			base.Destroy();

			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;

			if (_aiOperationPerformed)
			{
				_aiOperationPerformed = false;
				GameLevelData.Instance.SaveGameProgressLocally();
			}

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			UIEventController.Instance.DispatchUIEvent(PanelInputTextAction.EventPanelInputExternalClose);
			if (eventStartInput != null) eventStartInput.OnFocusEvent -= OnFocusEventStartInput;
			if (eventEndInput != null) eventEndInput.OnFocusEvent -= OnFocusEventEndInput;
			if (englishInput != null) englishInput.OnFocusEvent -= OnFocusEnglishInput;
			if (spanishInput != null) spanishInput.OnFocusEvent -= OnFocusSpanishInput;
			if (catalanInput != null) catalanInput.OnFocusEvent -= OnFocusCatalanInput;
#endif
		}

		private bool CheckAIAdminOperationAllowed()
		{
#if !ENABLE_AI_OPERATIONS
			string information = LanguageController.Instance.GetText("text.error");
			string aiOperationNotEnabled = LanguageController.Instance.GetText("message.ai.operation.not.enabled");
			UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
			ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, information, aiOperationNotEnabled);
			return false;
#else					
			if (GameLevelData.Instance.AllowAIAdminOperation(1))
			{
				_aiOperationPerformed = true;				
				return true;		
			}
			else
			{
				if (_aiOperationPerformed)
				{
					_aiOperationPerformed = false;
					GameLevelData.Instance.SaveGameProgressLocally();
				}
				string information = LanguageController.Instance.GetText("text.info");
				string limitAIOperationReached = LanguageController.Instance.GetText("message.limit.ai.operation.for.admin");
				UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, information, limitAIOperationReached);
				return false;
			}	
#endif			
		}

		private void SetUpPlayButton(Button button, string languageCode)
		{
			GameObject iconPlay = button.gameObject.transform.Find("Icon_Play").gameObject;
			GameObject iconStop = button.gameObject.transform.Find("Icon_Stop").gameObject;
			
			_audioPlayers.Add(languageCode, new List<GameObject>() { iconPlay, iconStop });			
		}

		private bool GetStatePlayButton(string languageCode)
		{
			if (_audioPlayers.ContainsKey(languageCode))
			{
				return _audioPlayers[languageCode][1].activeSelf;
			}
			else
			{
				return false;
			}
		}

		private void StopAllPlayButtons()
		{
			SystemEventController.Instance.ClearSystemEvents(ScreenXMLEditSegmentView.EventScreenXMLEditSegmentViewStopPlaying);

			foreach (var item in _audioPlayers)
			{
				item.Value[0].SetActive(true);
				item.Value[1].SetActive(false);
			}
		}

		private void ActivatePlayButton(string languageCode, bool isPlaying)
		{
			if (_audioPlayers.ContainsKey(languageCode))
			{
				_audioPlayers[languageCode][0].SetActive(!isPlaying);
				_audioPlayers[languageCode][1].SetActive(isPlaying);
			}
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
#if ENABLE_SPEECH
			if (_speechProcessingRequested)
			{
				if (nameEvent.Equals(SpeechRecognitionController.EventTextToSpeechControllerTimeoutProcessing))
				{
					_speechProcessingRequested = false;
					contentProcessing.gameObject.SetActive(false);
				}
				if (nameEvent.Equals(EventScreenXMLEditSegmentViewSpeechRecognized))
				{
					_speechProcessingRequested = false;
					bool isOk = (bool)parameters[0];
					string resultText = (string)parameters[1];
					_timeToRecord = -1;
					_timeToProcess = -1;
					contentProcessing.gameObject.SetActive(false);
					if (isOk)
					{
						switch (LanguageController.Instance.CodeLanguage)
						{
							case LanguageController.CodeLanguageEnglish:
								englishInput.text = resultText;
								break;
							case LanguageController.CodeLanguageSpanish:
								spanishInput.text = resultText;
								break;
							case LanguageController.CodeLanguageCatalan:
								catalanInput.text = resultText;
								break;
							case LanguageController.CodeLanguageFrench:
								frenchInput.text = resultText;
								break;
						}
					}
				}
			}
#endif      			
			if (nameEvent.Equals(EventScreenXMLEditSegmentViewTranslationCompleted))
			{
				if ((bool)parameters[0])
				{
					string translatedText = (string)parameters[1];
					if (_askedForTranslation)
					{
						_askedForTranslation = false;
						_timeToProcess = -1;
						contentProcessing.gameObject.SetActive(false);
						UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
						switch (_codeLanguageTranlation)
						{
							case LanguageController.CodeLanguageEnglish:
								englishInput.text = translatedText;
								break;
							case LanguageController.CodeLanguageSpanish:
								spanishInput.text = translatedText;
								break;
							case LanguageController.CodeLanguageCatalan:
								catalanInput.text = translatedText;
								break;
							case LanguageController.CodeLanguageFrench:
								frenchInput.text = translatedText;
								break;
						}
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
							_selectedEntry.GetMessage().ResetModified(_languageRequested);
							UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
							AudioClip speechDownloaded = SpeechDatabaseController.Instance.GetSpeechDataByID(_secretId, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id, _languageRequested);
							if (speechDownloaded != null)
							{								
								SystemEventController.Instance.DelaySystemEvent(ScreenXMLEditSegmentView.EventScreenXMLEditSegmentViewStopPlaying, speechDownloaded.length);
								SoundsController.Instance.PlaySoundClipFx(ChannelsAudio.FX1, speechDownloaded, false, 1);
								_selectedEntry.GetAudio().SetText(_languageRequested, speechDownloaded.length.ToString());
							}
							return;
						}
					}
					SpeechDatabaseController.Instance.RegisterNewSpeech(_textRequested, new ItemMultiObjectEntry(_secretId, _textRequested, _voiceRequested, _languageRequested, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id));
				}
			}
			if (nameEvent.Equals(EventScreenXMLEditSegmentViewStopPlaying))
			{
				StopAllPlayButtons();
			}
			if (nameEvent.Equals(EventScreenXMLEditSegmentViewForcePlaying))
			{
				string languageCode = (string)parameters[0];
				ActivatePlayButton(languageCode, true);
			}
        }

        private void OnUIEvent(string nameEvent, object[] parameters)
        {
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL        
            if (nameEvent.Equals(ScreenVRKeyboardView.EventScreenVRKeyboardSetNewText))
			{
				if (eventStartInput.gameObject == (GameObject)parameters[0])
				{
					Content.gameObject.SetActive(true);
					eventStartInput.text = (string)parameters[1];
				}
				if (eventEndInput.gameObject == (GameObject)parameters[0])
				{
					Content.gameObject.SetActive(true);
					eventEndInput.text = (string)parameters[1];
				}
			}
#endif
        }

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
        private void OnFocusEventStartInput()
		{
			ScreenController.Instance.CreateScreen(ScreenVRKeyboardView.ScreenName, false, true,  eventStartInput.gameObject, eventStartInput, TOTAL_CHARACTERS_SECTION);
		}
		private void OnFocusEventEndInput()
		{
			ScreenController.Instance.CreateScreen(ScreenVRKeyboardView.ScreenName, false, true,  eventEndInput.gameObject, eventEndInput, TOTAL_CHARACTERS_SECTION);
		}
		private void OnFocusEnglishInput()
		{
			PanelInputTextAction inputActionText = MainController.Instance.CreateInputActionEditText();
			if (inputActionText != null) inputActionText.InputDescriptionObject = englishInput;
		}
		private void OnFocusSpanishInput()
		{
			PanelInputTextAction inputActionText = MainController.Instance.CreateInputActionEditText();
			if (inputActionText != null) inputActionText.InputDescriptionObject = spanishInput;
		}
		private void OnFocusCatalanInput()
		{
			PanelInputTextAction inputActionText = MainController.Instance.CreateInputActionEditText();
			if (inputActionText != null) inputActionText.InputDescriptionObject = catalanInput;
		}
		private void OnFocusFrenchInput()
		{
			PanelInputTextAction inputActionText = MainController.Instance.CreateInputActionEditText();
			if (inputActionText != null) inputActionText.InputDescriptionObject = frenchInput;
		}
#endif					

        private void OnStartEventChanged(string value)
        {
            _selectedEntry.StartEvent = value;
        }

        private void OnEndEventChanged(string value)
        {
			_selectedEntry.EndEvent = value;
        }

        private void OnButtonExit()
        {
			SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerStopNarrations);
			SystemEventController.Instance.DispatchSystemEvent(ScreenXMLNarrationNodesView.EventScreenXMLNarrationNodesViewRefresh);
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);			
        }

        private void OnCatalanChanged(string value)
        {
        	_selectedEntry.GetMessage().SetText(LanguageController.CodeLanguageCatalan, value);
        }

        private void OnSpanishChanged(string value)
        {
        	_selectedEntry.GetMessage().SetText(LanguageController.CodeLanguageSpanish, value);
        }

        private void OnEnglishChanged(string value)
        {
        	_selectedEntry.GetMessage().SetText(LanguageController.CodeLanguageEnglish, value);	
        }

        private void OnFrenchChanged(string value)
        {
        	_selectedEntry.GetMessage().SetText(LanguageController.CodeLanguageFrench, value);
        }

		private void OnAILanguageSpeech(string languageCode, string textfield)
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
					if (CheckAIAdminOperationAllowed())
					{
						_speechIdRequested = SpeechDatabaseController.Instance.GetSpeechID(_secretId, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id, languageCode);
						_languageRequested = languageCode;
						_voiceRequested = LanguageController.Instance.GetNarrationVoice(languageCode);
						_textRequested = textfield;
						ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, "", LanguageController.Instance.GetText("message.please.wait"));
						if (isTextDifferentFromSpeech)
						{
							SpeechDatabaseController.Instance.RegisterNewSpeech(_textRequested, new ItemMultiObjectEntry(_secretId, _textRequested, _voiceRequested, _languageRequested, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id));
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

        private void OnAIEnglishSpeech()
        {
			if (GetStatePlayButton(LanguageController.CodeLanguageEnglish))
			{
				StopAllPlayButtons();
				SoundsController.Instance.StopAllSounds();
				return;
			}
			StopAllPlayButtons();
			SoundsController.Instance.StopAllSounds();
			ActivatePlayButton(LanguageController.CodeLanguageEnglish, true);
			OnAILanguageSpeech(LanguageController.CodeLanguageEnglish, englishInput.text);
        }

        private void OnAISpanishSpeech()
        {
			if (GetStatePlayButton(LanguageController.CodeLanguageSpanish))
			{
				StopAllPlayButtons();
				SoundsController.Instance.StopAllSounds();
				return;
			}
			StopAllPlayButtons();
			SoundsController.Instance.StopAllSounds();
			ActivatePlayButton(LanguageController.CodeLanguageSpanish, true);
			OnAILanguageSpeech(LanguageController.CodeLanguageSpanish, spanishInput.text);
        }

        private void OnAICatalanSpeech()
        {
			if (GetStatePlayButton(LanguageController.CodeLanguageCatalan))
			{
				StopAllPlayButtons();
				SoundsController.Instance.StopAllSounds();
				return;
			}
			StopAllPlayButtons();
			SoundsController.Instance.StopAllSounds();
			ActivatePlayButton(LanguageController.CodeLanguageCatalan, true);
			OnAILanguageSpeech(LanguageController.CodeLanguageCatalan, catalanInput.text);
        }


        private void OnAIFrenchSpeech()
        {
			if (GetStatePlayButton(LanguageController.CodeLanguageFrench))
			{
				StopAllPlayButtons();
				SoundsController.Instance.StopAllSounds();
				return;
			}
			StopAllPlayButtons();
			SoundsController.Instance.StopAllSounds();
			ActivatePlayButton(LanguageController.CodeLanguageFrench, true);
			OnAILanguageSpeech(LanguageController.CodeLanguageFrench, frenchInput.text);
        }

        private void OnAdd3DModel()
        {			
			StopAllPlayButtons();
			SoundsController.Instance.StopAllSounds();
			ScreenController.Instance.CreateScreen(ScreenXMLPOIObjectsView.ScreenName, false, true, TypeObjectNarration.Model3D, _selectedEntry);
        }

        private void OnAddVideo()
        {
			StopAllPlayButtons();
			SoundsController.Instance.StopAllSounds();
			ScreenController.Instance.CreateScreen(ScreenXMLPOIObjectsView.ScreenName, false, true, TypeObjectNarration.Video, _selectedEntry);
        }

        private void OnAddImage()
        {
			StopAllPlayButtons();
			SoundsController.Instance.StopAllSounds();
			ScreenController.Instance.CreateScreen(ScreenXMLPOIObjectsView.ScreenName, false, true, TypeObjectNarration.Image, _selectedEntry);
        }

        private void OnAddMusic()
        {
			StopAllPlayButtons();
			SoundsController.Instance.StopAllSounds();
			ScreenController.Instance.CreateScreen(ScreenXMLPOIObjectsView.ScreenName, false, true, TypeObjectNarration.Sound, _selectedEntry);
        }

        private void OnAddInteractable()
        {
			StopAllPlayButtons();
			SoundsController.Instance.StopAllSounds();
			ScreenController.Instance.CreateScreen(ScreenXMLPOIObjectsView.ScreenName, false, true, TypeObjectNarration.Interaction, _selectedEntry);
        }
        
		private void OnAddWaypoints()
        {
			StopAllPlayButtons();
			SoundsController.Instance.StopAllSounds();
			ScreenController.Instance.CreateScreen(ScreenXMLPOIObjectsView.ScreenName, false, true, TypeObjectNarration.Waypoints, _selectedEntry);
        }

        private void OnHideGuide(bool value)
        {
			_selectedEntry.ShouldHideGuide = value;            
        }

        private void OnPauseNarration(bool value)
        {
			_selectedEntry.ShouldPause = value;            
        }

        private void OnDestroyPrevious(bool value)
        {
			_selectedEntry.ShouldDestroy = value;
        }

		private string GetSourceLanguage()
		{
			switch (LanguageController.Instance.CodeLanguage)
			{
				case LanguageController.CodeLanguageEnglish:
					return "English";
				case LanguageController.CodeLanguageSpanish:
					return "Spanish";
				case LanguageController.CodeLanguageCatalan:
					return "Catalan";
				case LanguageController.CodeLanguageFrench:
					return "French";
			}			
			return "English";
		}

		private string GetSourceText()
		{
			switch (LanguageController.Instance.CodeLanguage)
			{
				case LanguageController.CodeLanguageEnglish:
					return englishInput.text;
				case LanguageController.CodeLanguageSpanish:
					return spanishInput.text;
				case LanguageController.CodeLanguageCatalan:
					return catalanInput.text;
				case LanguageController.CodeLanguageFrench:
					return frenchInput.text;
			}			
			return englishInput.text;
		}

        private void OnAIFrenchTranslation()
        {
			if (CheckAIAdminOperationAllowed())
			{
				string sourceTextToTranslate = GetSourceText();
				if (sourceTextToTranslate.Length > 10)
				{
					string textRequest = "Please, translate the next text from "+GetSourceLanguage()+" to French. Give me only the translated text: \n" + sourceTextToTranslate;
					_codeLanguageTranlation = LanguageController.CodeLanguageFrench;
					_askedForTranslation = true;
					ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, "", LanguageController.Instance.GetText("message.please.wait"));
					AskForTranslation(textRequest);
				}
			}
        }

        private void OnAIFrenchUpload()
        {
			if (frenchInput.text.Length == 0)
			{
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, LanguageController.Instance.GetText("text.error"), LanguageController.Instance.GetText("screen.edit.segment.provide.text"));				
			}
			else
			{
				StopAllPlayButtons();
				SoundsController.Instance.StopAllSounds();
				SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerStopNarrations);
				_speechIdRequested = SpeechDatabaseController.Instance.GetSpeechID(_secretId, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id, LanguageController.CodeLanguageFrench);
				_languageRequested = LanguageController.CodeLanguageFrench;
				_textRequested = frenchInput.text;
				SpeechDatabaseController.Instance.OnUploadSpeech(frenchInput.text, _secretId, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id, LanguageController.CodeLanguageFrench);
			}
        }

        private void OnAICatalanTranslation()
        {
			if (CheckAIAdminOperationAllowed())
			{
				string sourceTextToTranslate = GetSourceText();
				if (sourceTextToTranslate.Length > 10)
				{
					string textRequest = "Please, translate the next text from "+GetSourceLanguage()+" to Catalan. Give me only the translated text: \n" + sourceTextToTranslate;
					_codeLanguageTranlation = LanguageController.CodeLanguageCatalan;
					_askedForTranslation = true;
					ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, "", LanguageController.Instance.GetText("message.please.wait"));
					AskForTranslation(textRequest);
				}
			}
        }

		private void OnAICatalanUpload()
        {
			if (catalanInput.text.Length == 0)
			{
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, LanguageController.Instance.GetText("text.error"), LanguageController.Instance.GetText("screen.edit.segment.provide.text"));				
			}
			else
			{
				StopAllPlayButtons();
				SoundsController.Instance.StopAllSounds();
				SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerStopNarrations);
				_speechIdRequested = SpeechDatabaseController.Instance.GetSpeechID(_secretId, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id, LanguageController.CodeLanguageCatalan);
				_languageRequested = LanguageController.CodeLanguageCatalan;
				_textRequested = catalanInput.text;
				SpeechDatabaseController.Instance.OnUploadSpeech(catalanInput.text, _secretId, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id, LanguageController.CodeLanguageCatalan);
			}
        }

        private void OnAISpanishTranslation()
        {
			if (CheckAIAdminOperationAllowed())
			{
				string sourceTextToTranslate = GetSourceText();
				if (sourceTextToTranslate.Length > 10)
				{
					string textRequest = "Please, translate the next text from "+GetSourceLanguage()+" to Spanish. Give me only the translated text: \n" + sourceTextToTranslate;
					_codeLanguageTranlation = LanguageController.CodeLanguageSpanish;
					_askedForTranslation = true;
					ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, "", LanguageController.Instance.GetText("message.please.wait"));
					AskForTranslation(textRequest);
				}
			}
        }

        private void OnAISpanishUpload()
        {
			if (spanishInput.text.Length == 0)
			{
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, LanguageController.Instance.GetText("text.error"), LanguageController.Instance.GetText("screen.edit.segment.provide.text"));
			}
			else
			{
				StopAllPlayButtons();
				SoundsController.Instance.StopAllSounds();
				SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerStopNarrations);
				_speechIdRequested = SpeechDatabaseController.Instance.GetSpeechID(_secretId, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id, LanguageController.CodeLanguageSpanish);
				_languageRequested = LanguageController.CodeLanguageSpanish;
				_textRequested = spanishInput.text;
				SpeechDatabaseController.Instance.OnUploadSpeech(spanishInput.text, _secretId, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id, LanguageController.CodeLanguageSpanish);
			}            
        }

        private void OnAIEnglishTranslation()
        {
			if (CheckAIAdminOperationAllowed())
			{
				string sourceTextToTranslate = GetSourceText();
				if (sourceTextToTranslate.Length > 10)
				{
					string textRequest = "Please, translate the next text from "+GetSourceLanguage()+" to English. Give me only the translated text: \n" + sourceTextToTranslate;
					_codeLanguageTranlation = LanguageController.CodeLanguageEnglish;
					_askedForTranslation = true;
					ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, "", LanguageController.Instance.GetText("message.please.wait"));
					AskForTranslation(textRequest);
				}
			}
        }
        private void OnAIEnglishUpload()
        {
			if (englishInput.text.Length == 0)
			{
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, LanguageController.Instance.GetText("text.error"), LanguageController.Instance.GetText("screen.edit.segment.provide.text"));				
			}
			else
			{
				StopAllPlayButtons();
				SoundsController.Instance.StopAllSounds();
				SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerStopNarrations);
				_speechIdRequested = SpeechDatabaseController.Instance.GetSpeechID(_secretId, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id, LanguageController.CodeLanguageEnglish);
				_languageRequested = LanguageController.CodeLanguageEnglish;
				_textRequested = englishInput.text;
				SpeechDatabaseController.Instance.OnUploadSpeech(englishInput.text, _secretId, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationPOI.Id, _selectedEntry.Id, LanguageController.CodeLanguageEnglish);
			}            
        }

        private void OnVoiceRecognition()
        {
			if (CheckAIAdminOperationAllowed())
			{
				StopAllPlayButtons();
				SoundsController.Instance.StopAllSounds();
#if ENABLE_SPEECH
				contentProcessing.gameObject.SetActive(true);
				stopRecording.gameObject.SetActive(true);
				_timeToRecord = 30;
				_timeToProcess = -1;
				_speechProcessingRequested = true;
				infoTextProcessing.text = LanguageController.Instance.GetText("vr.keyboard.speak.now") + (int)_timeToRecord + "...";
				SpeechRecognitionController.Instance.StartRecording();
#endif			
			}
        }

        private void OnStopRecording()
        {
#if ENABLE_SPEECH		
			stopRecording.gameObject.SetActive(false);
			contentProcessing.gameObject.SetActive(true);
			_timeToRecord = -1;
			_timeToProcess = 0;
			infoTextProcessing.text = LanguageController.Instance.GetText("vr.keyboard.now.processing");
			SpeechRecognitionController.Instance.ProcessSpeech(EventScreenXMLEditSegmentViewSpeechRecognized);
#endif			
        }

		private void AskForTranslation(string text)
		{
			GameAIData.Instance.AskGenericQuestionAI("", text, false, EventScreenXMLEditSegmentViewTranslationCompleted);
		}

        void Update()
        {
			if (contentProcessing.gameObject.activeSelf)
			{
#if ENABLE_SPEECH
				if (_timeToRecord > 0)
				{
					_timeToRecord -= Time.deltaTime;
					infoTextProcessing.text = LanguageController.Instance.GetText("vr.keyboard.speak.now") + (int)_timeToRecord + "...";
					if (_timeToRecord < 0)
					{
						_timeToRecord = -1;
						OnStopRecording();
					}
				}
#endif
				if (_timeToProcess >= 0)
				{
					_timeToProcess += Time.deltaTime;
					infoTextProcessing.text = LanguageController.Instance.GetText("vr.keyboard.now.processing") + (int)_timeToProcess + "...";
					if (_timeToProcess > TOTAL_TIMEOUT_AI_PROCESS)
					{
						_timeToProcess = -1;
						contentProcessing.gameObject.SetActive(false);
					}
				}
			}
        }		
	}
}
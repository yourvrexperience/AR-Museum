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
		[SerializeField] private SlotManagerView SlotManagerSegments;
		[SerializeField] private GameObject NarrationSegmentPrefab;		

		[SerializeField] private TextMeshProUGUI addQRTitle;
		[SerializeField] private TextMeshProUGUI addImageTitle;
		[SerializeField] private TextMeshProUGUI addVideoTitle;
		[SerializeField] private TextMeshProUGUI add3DTitle;
		[SerializeField] private TextMeshProUGUI addMusicTitle;
		[SerializeField] private TextMeshProUGUI addInteractionTitle;
		[SerializeField] private TextMeshProUGUI addWaypointsTitle;

		[SerializeField] private Button addQRButton;
		[SerializeField] private Button addImageButton;
		[SerializeField] private Button addVideoButton;
		[SerializeField] private Button add3DButton;
		[SerializeField] private Button addMusicButton;
		[SerializeField] private Button addInteractionButton;
		[SerializeField] private Button addWaypointsButton;

		[SerializeField] private Toggle toggleHideGuide;
		[SerializeField] private Toggle toggleDestroyPrevious;
		[SerializeField] private Toggle togglePauseNarration;

		[SerializeField] private GameObject contentProcessing;
		[SerializeField] private TextMeshProUGUI infoTextProcessing;
		[SerializeField] private Button stopRecording;
		[SerializeField] private TextMeshProUGUI geoLocationTitle;
		[SerializeField] private TextMeshProUGUI geoLocationValue;

		private NarrationCreatorToken _selectedEntry;
		private NarrationCreatorData _narrationPOI;

		private float _timeToRecord = -1;
		private float _timeToProcess = -1;				
		private EasterEgg _narrationSecret = null;
		private int _secretId = -1;
		private bool _aiOperationPerformed = false;
		private string _baseText;
		private string _baseLanguage;
		
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
			
			StopAllPlayButtons();

			UIEventController.Instance.Event += OnUIEvent;
			SystemEventController.Instance.Event += OnSystemEvent;

			SlotManagerSegments.ClearCurrentGameObject(true);
            List<ItemMultiObjectEntry> itemsLanguageSegments = new List<ItemMultiObjectEntry>();
			for (int i = 0; i < LanguageController.Instance.SupportedLanguages.Length; i++)
			{
				string currLanguageCode = LanguageController.Instance.SupportedLanguages[i];
				itemsLanguageSegments.Add(new ItemMultiObjectEntry(this, currLanguageCode, _selectedEntry, _narrationPOI, _secretId));
			}
            SlotManagerSegments.Initialize(itemsLanguageSegments.Count, itemsLanguageSegments, NarrationSegmentPrefab);						

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL || ENABLE_NIANTICXR || ENABLE_QR_XR || ENABLE_QR_META
			eventStartInput.OnFocusEvent += OnFocusEventStartInput;
			eventEndInput.OnFocusEvent += OnFocusEventEndInput;
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


				addQRButton.onClick.AddListener(OnAddQR);
				addQRTitle.text = LanguageController.Instance.GetText("screen.edit.segment.title.add.qr");

				addQRButton.gameObject.SetActive(true);
				addQRTitle.gameObject.SetActive(true);

				geoLocationTitle.gameObject.SetActive(true);
				geoLocationValue.gameObject.SetActive(true);

				GeoLocation geoLocation;
				if (_secretId == -1)
				{
					geoLocation = MainController.Instance.POISelected.GeoPosition;
				}
				else
				{
					geoLocation = MainController.Instance.SecretSelected.GeoPosition;
				}

				if (geoLocation.IsNull())
				{
					geoLocationTitle.gameObject.SetActive(false);
					geoLocationValue.gameObject.SetActive(false);
				}
				else
				{
					geoLocationTitle.text = LanguageController.Instance.GetText("screen.synchronization.geolocation.only");
					geoLocationValue.text = geoLocation.ToString();
				}
			}
			else
			{
				addQRButton.gameObject.SetActive(false);
				addQRTitle.gameObject.SetActive(false);

				geoLocationTitle.gameObject.SetActive(false);
				geoLocationValue.gameObject.SetActive(false);

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
			_narrationPOI = null;
			_selectedEntry = null;

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL || ENABLE_NIANTICXR || ENABLE_QR_XR || ENABLE_QR_META
			UIEventController.Instance.DispatchUIEvent(PanelInputTextAction.EventPanelInputExternalClose);
			if (eventStartInput != null) eventStartInput.OnFocusEvent -= OnFocusEventStartInput;
			if (eventEndInput != null) eventEndInput.OnFocusEvent -= OnFocusEventEndInput;
#endif
		}

		public void SetBaseText(string text)
		{
			_baseText = text;
		}
		
		public string GetBaseText()
		{
			return _baseText;
		}

		public void SetBaseLanguage(string language)
		{
			_baseLanguage = language;
		}

		public string GetBaseLanguage()
		{
			return _baseLanguage;
		}

		public bool CheckAIAdminOperationAllowed()
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

		private void StopAllPlayButtons()
		{
			SystemEventController.Instance.ClearSystemEvents(ScreenXMLEditSegmentView.EventScreenXMLEditSegmentViewStopPlaying);
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
#if UNITY_EDITOR || ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || UNITY_WEBGL || ENABLE_QR_XR || ENABLE_QR_META			
			if (nameEvent.Equals(VRInputController.EventVRInputControllerMenuTriggered))
			{
				if (contentProcessing.gameObject.activeSelf)
				{
					OnButtonExit();
				}				
			}
#endif				
        }

        private void OnUIEvent(string nameEvent, object[] parameters)
        {
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL || ENABLE_NIANTICXR || ENABLE_QR_XR || ENABLE_QR_META 
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

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL || ENABLE_NIANTICXR || ENABLE_QR_XR || ENABLE_QR_META
        private void OnFocusEventStartInput()
		{
			ScreenController.Instance.CreateScreen(ScreenVRKeyboardView.ScreenName, false, true,  eventStartInput.gameObject, eventStartInput, TOTAL_CHARACTERS_SECTION);
		}
		private void OnFocusEventEndInput()
		{
			ScreenController.Instance.CreateScreen(ScreenVRKeyboardView.ScreenName, false, true,  eventEndInput.gameObject, eventEndInput, TOTAL_CHARACTERS_SECTION);
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

        private void OnAddQR()
        {
			StopAllPlayButtons();
			SoundsController.Instance.StopAllSounds();
			ScreenController.Instance.CreateScreen(ScreenXMLPOIObjectsView.ScreenName, false, true, TypeObjectNarration.QR, _selectedEntry);
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

		public void ShowContentProcessing(bool visible)
		{
			contentProcessing.gameObject.SetActive(visible);
		}

		public void ShowStopRecording(bool visible)
		{
			stopRecording.gameObject.SetActive(visible);
		}

		public void ResetContentProcessing()
		{
			_timeToRecord = -1;
			_timeToProcess = -1;
			ShowContentProcessing(false);
		}
                    
		public void OnStartRecording()
		{
#if ENABLE_SPEECH
			ShowContentProcessing(true);
			ShowStopRecording(true);
			_timeToRecord = 30;
			_timeToProcess = -1;
			infoTextProcessing.text = LanguageController.Instance.GetText("vr.keyboard.speak.now") + (int)_timeToRecord + "...";
			SpeechRecognitionController.Instance.StartRecording();
#endif			
		}

        public void OnStopRecording()
        {
#if ENABLE_SPEECH		
			ShowStopRecording(false);
			ShowContentProcessing(true);
			_timeToRecord = -1;
			_timeToProcess = 0;
			infoTextProcessing.text = LanguageController.Instance.GetText("vr.keyboard.now.processing");
			SpeechRecognitionController.Instance.ProcessSpeech(EventScreenXMLEditSegmentViewSpeechRecognized);
#endif			
        }

        void Update()
        {
			if (contentProcessing.gameObject.activeSelf)
			{
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NIANTICXR || ENABLE_QR_XR || ENABLE_QR_META
				if (MainController.Instance.GameInputController.ActionMenuPressed())
				{
					OnButtonExit();
				}
#endif

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
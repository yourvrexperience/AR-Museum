using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
using yourvrexperience.VR;
#endif

namespace yourvrexperience.Utils
{
	public enum ScreenInformationResponses { None = 0, Confirm, Cancel }

    public class ScreenInformationView : BaseScreenView, IScreenView
    {
		public const string EventScreenInformationResponse = "EventScreenInformationResponse";
		public const string EventScreenInformationDestroy = "EventScreenInformationDestroy";
		public const string EventScreenInformationByNameDestroy = "EventScreenInformationByNameDestroy";
		public const string EventScreenInformationInited = "EventScreenInformationInited";
		public const string EventScreenInformationInstantiated = "EventScreenInformationInstantiated";
		public const string EventScreenInformationDestroyed = "EventScreenInformationDestroyed";
		public const string EventScreenInformationRequestAllScreensDestroyed = "EventScreenInformationRequestAllScreensDestroyed";
		public const string EventScreenInformationUpdateInformation = "EventScreenInformationUpdateInformation";
		public const string EventScreenInformationAddInformation = "EventScreenInformationAddInformation";
		public const string EventScreenInformationSetInputText = "EventScreenInformationSetInputText";
		public const string EventScreenInformationAddInputText = "EventScreenInformationAddInputText";
		public const string EventScreenInformationAddTimer = "EventScreenInformationAddTimer";
		public const string EventScreenInformationIgnoreDestruction = "EventScreenInformationIgnoreDestruction";
		public const string EventScreenInformationDestroyAllEvenIgnored = "EventScreenInformationDestroyAllEvenIgnored";
		public const string EventScreenInformationSetColor = "EventScreenInformationSetColor";
		public const string EventScreenInformationSetFeedbackText = "EventScreenInformationSetFeedbackText";
		public const string EventScreenInformationSetAlignment = "EventScreenInformationSetAlignment";
		public const string EventScreenInformationEnableInputText = "EventScreenInformationEnableInputText";

		public const string ScreenInformation = "ScreenInformation";
		public const string ScreenInformationBig = "ScreenInformationBig";
		public const string ScreenConfirmation = "ScreenConfirmation";
		public const string ScreenConfirmationTop = "ScreenConfirmationTop";
		public const string ScreenConfirmationBig = "ScreenConfirmationBig";
		public const string ScreenConfirmationInput = "ScreenConfirmationInput";
		public const string ScreenInformationImage = "ScreenInformationImage";
		public const string ScreenConfirmationImage = "ScreenConfirmationImage";
		public const string ScreenLoading = "ScreenLoading";
		public const string ScreenLoadingImage = "ScreenLoadingImage";
		public const string ScreenInput = "ScreenInformationInput";
		public const string ScreenMediumInput = "ScreenInformationMediumInput";
		public const string ScreenLongInput = "ScreenInformationLongInput";

		protected GameObject _origin;
		protected string _customOutputEvent = "";
		protected CustomInput _inputValue;
		protected string _nameScreen;
		protected ICheckInput _checkInput = null;

		protected bool _enableTimer = false;
		protected string _codeXMLLanguage;
		protected int _totalTime;
		protected float _timeAcumSec;
		protected bool _ignoreDestruction = false;

		public static GameObject CreateScreenInformation(string screenName, GameObject origin, string title, string description, string customEvent = "", string ok = "", string cancel = "", Sprite infoImage = null, TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard, ICheckInput checkInput = null)
		{
			string okText = ok;
			if (okText.Length == 0)
			{
				okText = LanguageController.Instance.GetText("text.ok");
			}
			string cancelText = cancel;
			if (cancelText.Length == 0)
			{
				cancelText = LanguageController.Instance.GetText("text.cancel");
			}
			bool shouldHidePrevious = true;
#if !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
			shouldHidePrevious = false;
#endif			
			return ScreenController.Instance.CreateScreen(screenName, false, shouldHidePrevious, origin, customEvent, title, description, okText, cancelText, infoImage, contentType, checkInput);
		}

        public override void Initialize(params object[] parameters)
        {
            base.Initialize(parameters);

			_origin = (GameObject)parameters[0];
			_nameScreen = this.gameObject.name;
            _customOutputEvent = (string)parameters[1];
			UpdateTitle((string)parameters[2]);
			UpdateDescription((string)parameters[3]);
			UpdateFeedback("");
			string textOk = (string)parameters[4];
			string textCancel = (string)parameters[5];

			_inputValue = _content.GetComponentInChildren<CustomInput>();
			_checkInput = null;
			if (_inputValue != null)
			{
				_inputValue.OnFocusEvent += OnFocusInputValue;
				_inputValue.text = "";
				_inputValue.contentType = TMP_InputField.ContentType.Standard;
				if (parameters.Length > 7)
				{
					_inputValue.contentType = (TMP_InputField.ContentType)parameters[7];
				}
				if (parameters.Length > 8)
                {
					_checkInput = (ICheckInput)parameters[8];
				}
			}

			Transform contentImage = yourvrexperience.Utils.Utilities.FindNameInChildren(_content, "Image");
			if (contentImage != null)
			{
				Image imageScreen = contentImage.GetComponent<Image>();
				if (parameters.Length > 6)
				{
					if (parameters[6] != null)
					{
						imageScreen.sprite = (Sprite)parameters[6];
					}					
				}				
			}

			Transform contentButtonOk = yourvrexperience.Utils.Utilities.FindNameInChildren(_content, "ButtonOk");
            if (contentButtonOk != null)
            {
                contentButtonOk.GetComponent<Button>().onClick.AddListener(OnConfirmation);
				if (contentButtonOk.GetComponentInChildren<TextMeshProUGUI>() != null)
				{
					contentButtonOk.GetComponentInChildren<TextMeshProUGUI>().text = textOk;
				}
				else
				{
					if (contentButtonOk.GetComponentInChildren<Text>() != null)
					{
						contentButtonOk.GetComponentInChildren<Text>().text = textOk;
					}
				}
            }
			Transform contentButtonDeny = yourvrexperience.Utils.Utilities.FindNameInChildren(_content, "ButtonDeny");
            if (contentButtonDeny != null)
            {
                contentButtonDeny.GetComponent<Button>().onClick.AddListener(OnCancel);
				if (contentButtonDeny.GetComponentInChildren<TextMeshProUGUI>() != null)
				{
					contentButtonDeny.GetComponentInChildren<TextMeshProUGUI>().text = textCancel;
				}
				else
				{
					if (contentButtonDeny.GetComponentInChildren<Text>() != null)
					{
						contentButtonDeny.GetComponentInChildren<Text>().text = textCancel;
					}
				}
            }
			Transform contentButtonCancel = yourvrexperience.Utils.Utilities.FindNameInChildren(_content, "ButtonCancel");
            if (contentButtonCancel != null)
            {
                contentButtonCancel.GetComponent<Button>().onClick.AddListener(OnCancel);
            }

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			if (_background != null)
			{
				_background.gameObject.SetActive(false);
			}
#else
			this.GetComponent<Canvas>().sortingOrder = 1000;			
#endif

			UIEventController.Instance.Event += OnUIEvent;
			UIEventController.Instance.DispatchUIEvent(EventScreenInformationInstantiated, _nameScreen, this.gameObject);
			UIEventController.Instance.DelayUIEvent(EventScreenInformationInited, 0.1f, _nameScreen, (_inputValue != null));
        }

		void OnDestroy()
		{
			Destroy();
		}

		public override void Destroy()
        {
			if (_content != null)
			{
				_content = null;
				_origin = null;
				_checkInput = null;
				bool wasInput = (_inputValue != null);
				if (_inputValue != null)
				{
					_inputValue.OnFocusEvent -= OnFocusInputValue;
				}				
				_inputValue = null;
				if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
				base.Destroy();

				UIEventController.Instance.DispatchUIEvent(EventScreenInformationDestroyed, _nameScreen, wasInput);
			}
        }
		

		private void UpdateTitle(string title)
		{
			Transform contentTitle = yourvrexperience.Utils.Utilities.FindNameInChildren(_content, "Title");

			if (contentTitle != null)
			{
				if (contentTitle.GetComponent<TextMeshProUGUI>() != null)
				{
					contentTitle.GetComponent<TextMeshProUGUI>().text = title;
				}
				else
				{
					if (contentTitle.GetComponent<Text>() != null)
					{
						contentTitle.GetComponent<Text>().text = title;
					}
				}
			} 
		}

		protected void UpdateFeedback(string feedback)
		{
			Transform contentTitle = yourvrexperience.Utils.Utilities.FindNameInChildren(_content, "Feedback");
			if (contentTitle != null)
			{
				if (contentTitle.GetComponent<TextMeshProUGUI>() != null)
				{
					contentTitle.GetComponent<TextMeshProUGUI>().text = feedback;
				}
				else
				{
					if (contentTitle.GetComponent<Text>() != null)
					{
						contentTitle.GetComponent<Text>().text = feedback;
					}
				}
			}			
		}

		private void UpdateDescription(string description)
		{
			Transform contentDescription = yourvrexperience.Utils.Utilities.FindNameInChildren(_content, "Description");
			if (contentDescription != null)
			{
				if (contentDescription.GetComponent<TextMeshProUGUI>() != null)
				{
					contentDescription.GetComponent<TextMeshProUGUI>().text = description;
				}
				else
				{
					if (contentDescription.GetComponent<Text>() != null)
					{
						contentDescription.GetComponent<Text>().text = description;
					}
				}
			} 
		}

		private void AddToDescription(string description)
		{
			Transform contentDescription = yourvrexperience.Utils.Utilities.FindNameInChildren(_content, "Description");
			if (contentDescription != null)
			{
				if (contentDescription.GetComponent<TextMeshProUGUI>() != null)
				{
					contentDescription.GetComponent<TextMeshProUGUI>().text += description;
				}
				else
				{
					if (contentDescription.GetComponent<Text>() != null)
					{
						contentDescription.GetComponent<Text>().text += description;
					}
				}
			}
		}

		private void OnFocusInputValue()
        {
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			_content.gameObject.SetActive(false);
			ScreenController.Instance.CreateScreen(ScreenVRKeyboardView.ScreenName, false, true,  _inputValue.gameObject, _inputValue, 200);
#endif			
        }

        protected virtual void OnConfirmation()
        {
			try
			{
				if (_inputValue != null)
				{
					if (_checkInput != null)
                    {
						int codeInput = _checkInput.IsTextValid(_inputValue.text);
						if (codeInput > 0)
						{
							UpdateFeedback(_checkInput.GetMessageFeedback(codeInput));
							return;
						}
					}
					if (_customOutputEvent != null)
					{
						if (_customOutputEvent.Length > 0)
						{
							UIEventController.Instance.DispatchUIEvent(_customOutputEvent, _origin, ScreenInformationResponses.Confirm, _inputValue.text);
						}
					}
					UIEventController.Instance.DispatchUIEvent(EventScreenInformationResponse, _origin, ScreenInformationResponses.Confirm, _inputValue.text);
				}
				else
				{
					if (_customOutputEvent != null)
					{
						if (_customOutputEvent.Length > 0)
						{
							UIEventController.Instance.DispatchUIEvent(_customOutputEvent, _origin, ScreenInformationResponses.Confirm);
						}
					}
					UIEventController.Instance.DispatchUIEvent(EventScreenInformationResponse, _origin, ScreenInformationResponses.Confirm);
				}
				UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
			}
			catch (Exception err) { };
		}

		protected virtual void OnCancel()
        {
            if (_customOutputEvent != null)
            {
                if (_customOutputEvent.Length > 0)
                {
					if (_inputValue == null)
                    {
						UIEventController.Instance.DispatchUIEvent(_customOutputEvent, _origin, ScreenInformationResponses.Cancel);
					}
					else
                    {
						UIEventController.Instance.DispatchUIEvent(_customOutputEvent, _origin, ScreenInformationResponses.Cancel, _inputValue.text);
					}					
                }
            }
			if (_inputValue == null)
			{
				UIEventController.Instance.DispatchUIEvent(EventScreenInformationResponse, _origin, ScreenInformationResponses.Cancel);
			}
			else
			{
				UIEventController.Instance.DispatchUIEvent(EventScreenInformationResponse, _origin, ScreenInformationResponses.Cancel, _inputValue.text);
			}
            UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
        }

		protected virtual void OnUIEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(EventScreenInformationSetFeedbackText))
            {
				if (parameters.Length > 1)
                {
					if (this.gameObject == (GameObject)parameters[0])
					{
						UpdateFeedback((string)parameters[1]);
					}
				}
				else
                {
					UpdateFeedback((string)parameters[0]);
				}
			}
			if (nameEvent.Equals(EventScreenInformationIgnoreDestruction))
            {
				if (this.gameObject == (GameObject)parameters[0])
                {
					_ignoreDestruction = (bool)parameters[1];
				}
            }
			if (nameEvent.Equals(EventScreenInformationSetColor))
            {
				if (this.gameObject == (GameObject)parameters[0])
				{
					_content.GetComponent<Image>().color = (Color)parameters[1];
				}
			}
			if (nameEvent.Equals(EventScreenInformationAddTimer))
            {
				_enableTimer = true;
				_codeXMLLanguage = (string)parameters[0];
				_totalTime = (int)parameters[1];
				_timeAcumSec = 0;
			}
			if (nameEvent.Equals(EventScreenInformationDestroy))
			{
				if (_ignoreDestruction) return;

				bool shouldDestroy = true;
				if (parameters.Length > 0)
				{
					GameObject target = (GameObject)parameters[0];
					shouldDestroy = (target == this.gameObject);
				}
				if (shouldDestroy)
				{
					UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
				}
			}
			if (nameEvent.Equals(EventScreenInformationByNameDestroy))
            {
				if (_ignoreDestruction) return;

				if (NameScreen.IndexOf((string)parameters[0]) != -1)
                {
					UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
				}
            }
			if (nameEvent.Equals(EventScreenInformationRequestAllScreensDestroyed))
			{
				if (_ignoreDestruction) return;

				UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
			}
			if (nameEvent.Equals(EventScreenInformationDestroyAllEvenIgnored))
            {
				_ignoreDestruction = false;
				UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
			}
			if (nameEvent.Equals(EventScreenInformationUpdateInformation))
			{
				UpdateTitle((string)parameters[0]);
				UpdateDescription((string)parameters[1]);
			}
			if (nameEvent.Equals(EventScreenInformationSetInputText))
			{
				if (_inputValue != null)
				{
					_inputValue.text = (string)parameters[0];
				}
			}
			if (nameEvent.Equals(EventScreenInformationEnableInputText))
            {
				if (_inputValue != null)
				{
					_inputValue.interactable = (bool)parameters[0];
				}
			}
			if (nameEvent.Equals(EventScreenInformationAddInputText))
			{
				if (_inputValue != null)
				{
					_inputValue.text += (string)parameters[0];
				}
			}
			if (nameEvent.Equals(EventScreenInformationAddInformation))
			{
				AddToDescription((string)parameters[0]);
			}
			if (nameEvent.Equals(EventScreenInformationSetAlignment))
            {				
				Transform contentDescription = yourvrexperience.Utils.Utilities.FindNameInChildren(_content, "Description");
				contentDescription.GetComponent<TextMeshProUGUI>().alignment = (TextAlignmentOptions)parameters[0];
			}
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			if (nameEvent.Equals(ScreenVRKeyboardView.EventScreenVRKeyboardSetNewText))
			{
				if (_inputValue.gameObject == (GameObject)parameters[0])
				{
					_inputValue.text = (string)parameters[1];
				}
			}
#endif
		}

		void Update()
        {
			if (_enableTimer)
            {
				if (_totalTime <= 0)
                {
					_enableTimer = false;
				}
				else
                {
					_timeAcumSec += Time.deltaTime;
					if (_timeAcumSec >= 1)
					{
						_timeAcumSec -= 1;
						_totalTime--;
						UpdateDescription(LanguageController.Instance.GetText(_codeXMLLanguage, _totalTime));
					}
				}
			}
        }
	}
}
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using yourvrexperience.Narration;
using yourvrexperience.speech;
using yourvrexperience.Utils;
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR
using yourvrexperience.VR;
#endif
using static yourvrexperience.Narration.NarrationController;

namespace yourvrexperience.template6dof
{
	public class ScreenHUDAIInteractionView : BaseScreenView, IScreenAIView
	{
		public const string ScreenName = "ScreenHUDAIInteractionView";		

		public const string EventScreenHUDAIInteractionViewAskQuestion = "EventScreenHUDAIInteractionViewAskQuestion";		

		public const string EventScreenAIInteractionDestroy = "EventScreenAIInteractionDestroy";

		[SerializeField] private GameObject ContentBase;
		[SerializeField] private GameObject ContentRecording;
		[SerializeField] private GameObject ContentAIProcessing;
		[SerializeField] private GameObject ContentNarration;

		[SerializeField] private Button buttonClose;
		[SerializeField] private Button buttonRequest;
		[SerializeField] private Button buttonRecord;
		[SerializeField] private Button buttonStopRecording;
		[SerializeField] private Button buttonReturnQuestion;
		[SerializeField] private Button buttonReplayAnswer;

		[SerializeField] private TextMeshProUGUI description;
		[SerializeField] private TextMeshProUGUI descriptionRecording;
		[SerializeField] private TextMeshProUGUI descriptionProcessing;
		[SerializeField] private TextMeshProUGUI descriptionNarration;
		[SerializeField] private TextMeshProUGUI buttonRequestText;
		[SerializeField] private CustomInput inputField;

		private bool _aiQuestionPerformed = false;

		public GameObject GetGameObject()
		{
			return this.gameObject;
		}
		public void SetTextInputField(string text)
		{
			inputField.text = text;
		}
		public void SetDescriptionRecording(string text)
		{
			descriptionRecording.text = text;
		}
		public void SetDescriptionProcessing(string text)
		{
			descriptionProcessing.text = text;
		}

		public void PositionNarrator()
		{
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
			Vector3 forwardGuide = -MainController.Instance.GuideTourView.GetModel().transform.forward;
			GameObject contentVRScreen = MainController.Instance.GuideTourView.ScreenVR;
			Vector3 posScreen = contentVRScreen.transform.position;
			ContentNarration.transform.position = posScreen;
			ContentNarration.transform.forward = forwardGuide;
#endif
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			SystemEventController.Instance.Event += OnSystemEvent;
			UIEventController.Instance.Event += OnUIEvent;

			description.text = LanguageController.Instance.GetText("screen.ai.interaction.question.description");
			buttonRequestText.text = LanguageController.Instance.GetText("screen.ai.interaction.request.to.ai");
			inputField.text = "";

			buttonClose.onClick.AddListener(OnButtonClose);
			buttonRequest.onClick.AddListener(OnButtonRequestAI);
			buttonRecord.onClick.AddListener(OnRecordSpeech);
			buttonStopRecording.onClick.AddListener(OnStopRecordingSpeech);
			buttonReturnQuestion.onClick.AddListener(OnReturnToQuestion);
			buttonReplayAnswer.onClick.AddListener(OnReplayAnswer);

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			inputField.OnFocusDownEvent += OnFocusQuestionInput;

			RefocusScreen refocusComponent = this.gameObject.GetComponent<RefocusScreen>();
			if (refocusComponent == null)
			{
				refocusComponent = this.gameObject.AddComponent<RefocusScreen>();
			}
			refocusComponent.Activate(VRInputController.Instance.Camera, ScreenController.Instance.DistanceScreen, 1, 0.4f);
#endif

#if UNITY_WEBGL
			buttonRecord.gameObject.SetActive(false);
#endif
			SystemEventController.Instance.DispatchSystemEvent(Template6DOFAIData.EventTemplate6DOFAIDataChangeState, AIRequestStates.Presentation, this);			
		}

        public override void Destroy()
		{
			base.Destroy();

			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
			
			SystemEventController.Instance.DispatchSystemEvent(Template6DOFAIData.EventTemplate6DOFAIDataCancelAIRequest);
			
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			UIEventController.Instance.DispatchUIEvent(PanelInputTextAction.EventPanelInputExternalClose);
#endif			
			SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, true);
		}


#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
		private void OnFocusQuestionInput()
		{
			PanelInputTextAction inputActionText = MainController.Instance.CreateInputActionEditText();
			if (inputActionText != null) inputActionText.InputDescriptionObject = inputField;
		}
#endif	
		private void OnButtonClose()
		{
			UIEventController.Instance.DispatchUIEvent(ScreenPauseView.EventScreenPauseViewResumeGame);
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);			
			if (_aiQuestionPerformed)
			{
				_aiQuestionPerformed = false;
				GameLevelData.Instance.SaveGameProgressLocally();
			}
		}

        private void OnButtonRequestAI()
        {
            string questionRequest = inputField.text;
			if (questionRequest.Length < 20)
			{
				string information = LanguageController.Instance.GetText("text.warning");
				string aiQuestionDescription = LanguageController.Instance.GetText("screen.ai.interaction.question.short.to.ai");
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, information, aiQuestionDescription);
			}
			else
			{
				if (GameLevelData.Instance.AllowAIConsumerOperation(4))
				{
					_aiQuestionPerformed = true;
					SystemEventController.Instance.DispatchSystemEvent(Template6DOFAIData.EventTemplate6DOFAIDataChangeState, AIRequestStates.AIRequestProcess, questionRequest);			
				}
				else
				{
					if (_aiQuestionPerformed)
					{
						_aiQuestionPerformed = false;
						GameLevelData.Instance.SaveGameProgressLocally();
					}
					string information = LanguageController.Instance.GetText("text.info");
					string limitAIOperationReached = LanguageController.Instance.GetText("message.limit.ai.operation.for.consumer");
					ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, information, limitAIOperationReached);
				}				
			}
        }

        private void OnStopRecordingSpeech()
        {
			SystemEventController.Instance.DispatchSystemEvent(Template6DOFAIData.EventTemplate6DOFAIDataChangeState, AIRequestStates.SpeechRecognitionProcess);			
        }

        private void OnRecordSpeech()
        {
			if (GameLevelData.Instance.AllowAIConsumerOperation(1))
			{
				SystemEventController.Instance.DispatchSystemEvent(Template6DOFAIData.EventTemplate6DOFAIDataChangeState, AIRequestStates.Recording);
			}
        }

        private void OnReturnToQuestion()
        {
			SystemEventController.Instance.DispatchSystemEvent(TourGuideView.EventTourGuideViewSpeakActivation, false);
			SystemEventController.Instance.DispatchSystemEvent(Template6DOFAIData.EventTemplate6DOFAIDataChangeState, AIRequestStates.Presentation, this);			
        }

        private void OnReplayAnswer()
        {
			SystemEventController.Instance.DispatchSystemEvent(Template6DOFAIData.EventTemplate6DOFAIDataReplyAnswer);			
        }

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(NarrationController.NarrationControllerReportEndedCurrentNarration))
			{
				MainController.Instance.GuideTourView.ApplyIdleAnimation();
			}
			if (nameEvent.Equals(EventScreenAIInteractionDestroy))
			{
				OnButtonClose();
			}
		}

		private void OnUIEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(NarrationToken.NarrationTokenViewUpdateText))
			{
				bool isMainNarration = (bool)parameters[0];
				if (!isMainNarration)
				{
					descriptionNarration.text = (string)parameters[1];
				}
			}
        }

		public void SetState(AIRequestStates state)
		{
			switch (state)
			{
				case AIRequestStates.Presentation:
					ContentBase.SetActive(true);
					ContentRecording.SetActive(false);
					ContentAIProcessing.SetActive(false);
					ContentNarration.SetActive(false);
					break;

				case AIRequestStates.Recording:
					ContentBase.SetActive(false);
					ContentRecording.SetActive(true);
					ContentAIProcessing.SetActive(false);
					ContentNarration.SetActive(false);
					break;				

				case AIRequestStates.SpeechRecognitionProcess:
					ContentBase.SetActive(false);
					ContentRecording.SetActive(true);
					ContentAIProcessing.SetActive(false);
					ContentNarration.SetActive(false);
					break; 					

				case AIRequestStates.AIRequestProcess:
					ContentBase.SetActive(false);
					ContentRecording.SetActive(false);
					ContentAIProcessing.SetActive(true);
					ContentNarration.SetActive(false);
					break;

				case AIRequestStates.PlayNarration:
					ContentBase.SetActive(false);
					ContentRecording.SetActive(false);
					ContentAIProcessing.SetActive(false);
					ContentNarration.SetActive(true);
					break;
			}
		}
	}
}
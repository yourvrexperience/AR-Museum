using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.speech;
using yourvrexperience.Utils;
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
using yourvrexperience.VR;
#endif

namespace yourvrexperience.template6dof
{
	public class ScreenInputMuseumFormView : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenInputMuseumFormView";

		public const string EventScreenInputFormViewSubmitResponse = "EventScreenInputFormViewSubmitResponse";
		public const string EventScreenInputFormViewSpeechRecognized = "EventScreenInputFormViewSpeechRecognized";

		public const float TimeoutMaxRecording = 15;

		[SerializeField] private GameObject ContentBase;
		[SerializeField] private GameObject ContentRecording;
		[SerializeField] private GameObject ContentProcessing;
		[SerializeField] private TextMeshProUGUI descriptionRecording;
		[SerializeField] private TextMeshProUGUI descriptionProcessing;

		[SerializeField] private Button buttonRecord;
		[SerializeField] private Button buttonStopRecording;

		[SerializeField] private TextMeshProUGUI title;
		[SerializeField] private TextMeshProUGUI page;
		[SerializeField] private TextMeshProUGUI description;
		[SerializeField] private TMP_InputField inputField;
		[SerializeField] private Button buttonNext;

		private float _timerRecording = 0;
		private AIRequestStates _state;

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			title.text = LanguageController.Instance.GetText("screen.main.menu.title");

			page.text = (string)parameters[0];
			description.text = (string)parameters[1];
			inputField.text = "";

			buttonNext.onClick.AddListener(OnButtonNext);
			buttonNext.transform.GetComponentInChildren<TextMeshProUGUI>().text = (string)parameters[2];

			buttonRecord.onClick.AddListener(OnRecordSpeech);
			buttonStopRecording.onClick.AddListener(OnStopRecordingSpeech);

			SystemEventController.Instance.Event += OnSystemEvent;

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			RefocusScreen refocusComponent = this.gameObject.GetComponent<RefocusScreen>();
			if (refocusComponent == null)
			{
				refocusComponent = this.gameObject.AddComponent<RefocusScreen>();
			}
			refocusComponent.Activate(VRInputController.Instance.Camera, ScreenController.Instance.DistanceScreen, 1, 0.4f);
#else
			buttonRecord.gameObject.SetActive(false);
#endif			
			ChangeState(AIRequestStates.Presentation);			
		}

        public override void Destroy()
		{
			base.Destroy();

			SystemEventController.Instance.Event -= OnSystemEvent;
		}

        private void OnStopRecordingSpeech()
        {
			ChangeState(AIRequestStates.SpeechRecognitionProcess);
        }

        private void OnRecordSpeech()
        {
			ChangeState(AIRequestStates.Recording);
        }

		private void OnButtonNext()
		{
			string data = inputField.text;
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
			UIEventController.Instance.DispatchUIEvent(EventScreenInputFormViewSubmitResponse, data);			
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(EventScreenInputFormViewSpeechRecognized))
			{
				bool success = (bool)parameters[0];
				string speechToTextResult = (string)parameters[1];
				inputField.text = speechToTextResult;
				ChangeState(AIRequestStates.Presentation);
			}
        }

		private void ChangeState(AIRequestStates newState)
		{
			_state = newState;
			switch (_state)
			{
				case AIRequestStates.Presentation:
					ContentBase.SetActive(true);
					ContentRecording.SetActive(false);
					ContentProcessing.SetActive(false);
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, false);
					break;

				case AIRequestStates.Recording:
					ContentBase.SetActive(false);
					ContentRecording.SetActive(true);
					ContentProcessing.SetActive(false);

					SpeechRecognitionController.Instance.StartRecording();
					_timerRecording = 0;
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, false);
					break;

				case AIRequestStates.SpeechRecognitionProcess:
					ContentBase.SetActive(false);
					ContentRecording.SetActive(true);
					ContentProcessing.SetActive(false);

					descriptionProcessing.text = LanguageController.Instance.GetText("screen.questionarie.processing.speech");

					SpeechRecognitionController.Instance.ProcessSpeech(EventScreenInputFormViewSpeechRecognized);
					_timerRecording = 0;
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, false);
					break;
			}
		}

		void Update()
		{
			switch (_state)
			{
				case AIRequestStates.Presentation:
					break;

				case AIRequestStates.Recording:
					_timerRecording += Time.deltaTime;
					if (_timerRecording > TimeoutMaxRecording)
					{
						ChangeState(AIRequestStates.SpeechRecognitionProcess);
					}
					else
					{
						descriptionRecording.text = LanguageController.Instance.GetText("screen.ai.interaction.now.recording", (int)(TimeoutMaxRecording - _timerRecording));
					}
					break;

				case AIRequestStates.SpeechRecognitionProcess:
					break;
			}
		}
	}
}
using System;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.ai;
using yourvrexperience.Narration;
using yourvrexperience.speech;
using yourvrexperience.Utils;
using static yourvrexperience.Narration.NarrationController;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace yourvrexperience.template6dof
{
    public enum AIProviders { OpenAI = 0, Mistral, Gemini, DeepSeek, OpenRouter, Grok }

    public enum AIRequestStates { None, Presentation, Recording, SpeechRecognitionProcess, AIRequestProcess, PlayNarration }

    [CreateAssetMenu(menuName = "Game/Template6DOFAIData")]
	public class Template6DOFAIData : ScriptableObject
    {
		public const float TimeoutMaxRecording = 10;
		public const float TimeoutCancelAIRequest = 60;
        public const float DistanceAppearTourGuide = 1.5f;

        public const string EventTemplate6DOFAIDataChangeState = "EventTemplate6DOFAIDataChangeState";
        public const string EventTemplate6DOFAIDataAskQuestion = "EventTemplate6DOFAIDataAskQuestion";
        public const string EventTemplate6DOFAIDataRunDelayedAIQuestion = "EventTemplate6DOFAIDataRunDelayedAIQuestion";
        public const string EventTemplate6DOFAIDataCancelAIRequest = "EventTemplate6DOFAIDataCancelAIRequest";
        public const string EventTemplate6DOFAIDataCanceledByServer = "EventTemplate6DOFAIDataCanceledByServer";        
        public const string EventTemplate6DOFAIDataReplyAnswer = "EventTemplate6DOFAIDataReplyAnswer";
        public const string EventTemplate6DOFAIDataSynthezeCompleted = "EventTemplate6DOFAIDataSynthezeCompleted";
        public const string EventTemplate6DOFAIDataSpeechRecognized = "EventTemplate6DOFAIDataSpeechRecognized";
        
	    private static Template6DOFAIData _instance;
        public static Template6DOFAIData Instance
        {
            get { return _instance; }
        }

		private AIRequestStates _state = AIRequestStates.None;
        
		private string _questionToAI;
		private int _conversationCounter = 0;
		private bool _requestedUserQuestion = false;
		private float _timerRecording = 0;
		private float _timerAIProcessing = 0;
		private bool _hasPlayedAnyNarration = false;

		private NarrationController _narrationController;
		private Dictionary<string, NarrationToken> _sentencesTexts = null;
		private List<string> _nameClips = new List<string>();
		private NarrationToken _currentNarrationTokenSynthesize = null;

        private IScreenAIView _screenAIView = null;
        private List<ChatMessage> _chatMessages = null;

        public void Initialize()
        {
            _instance = this;
            _questionToAI = "";
            _conversationCounter = 0;
            _state = AIRequestStates.None;
            _chatMessages = null;

            SystemEventController.Instance.Event += OnSystemEvent;
        }

        private bool PopNameClipSyntheziseRequest()
		{
			if (_nameClips.Count > 0)
			{
				string nameClip = _nameClips[0];
				_nameClips.RemoveAt(0);				
				NarrationToken sentence = null;
				if (_sentencesTexts.TryGetValue(nameClip, out sentence))
				{
					_currentNarrationTokenSynthesize = sentence;
                    if (LanguageController.Instance.GetSpeechGender() == null)
                    {
                        SpeechDatabaseController.Instance.PlaySpeech(_currentNarrationTokenSynthesize.GetSentence(), EventTemplate6DOFAIDataSynthezeCompleted);
                    }
                    else
                    {
                        if (LanguageController.Instance.GetSpeechGender() == "female")
                        {
                            SpeechRecognitionController.Instance.Synthetize(_currentNarrationTokenSynthesize.GetSentence(), GenderVoice.FEMALE, AgeVoice.ADULT, SpeedVoice.NORMAL, EventTemplate6DOFAIDataSynthezeCompleted);
                        }
                        else
                        {
                            SpeechRecognitionController.Instance.Synthetize(_currentNarrationTokenSynthesize.GetSentence(), GenderVoice.MALE, AgeVoice.ADULT, SpeedVoice.NORMAL, EventTemplate6DOFAIDataSynthezeCompleted);
                        }
                    }
				}
				return false;
			}
			else
			{
				return true;
			}
		}

		private void ChangeState(AIRequestStates newState, object[] parameters)
		{
			_state = newState;
			switch (_state)
			{
                case AIRequestStates.None:
                    _chatMessages = null;
                    if (_screenAIView != null)
                    {
                        _screenAIView.SetState(AIRequestStates.Presentation);
                    }                
                    _screenAIView = null;
                    if (_hasPlayedAnyNarration) SystemEventController.Instance.DispatchSystemEvent(GameStateRun.EventGameStateRunNavigateToCurrentPOI);
                    _hasPlayedAnyNarration = false;
                    DestroyNarration();                    
                    break;

				case AIRequestStates.Presentation:
                    _screenAIView = (IScreenAIView)parameters[1];
                    DestroyNarration();
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, false);					
					MainController.Instance.GuideTourView.ApplyIdleAnimation();
                    _screenAIView.SetState(AIRequestStates.Presentation);
					break;

				case AIRequestStates.Recording:
					SpeechRecognitionController.Instance.StartRecording();
					_timerRecording = 0;
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, false);
					MainController.Instance.GuideTourView.ApplyIdleAnimation();
                    _screenAIView.SetState(AIRequestStates.Recording);
					break;				

				case AIRequestStates.SpeechRecognitionProcess:
					SpeechRecognitionController.Instance.ProcessSpeech(EventTemplate6DOFAIDataSpeechRecognized);
					_timerRecording = 0;
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, false);
					MainController.Instance.GuideTourView.ApplyIdleAnimation();
                    _screenAIView.SetState(AIRequestStates.SpeechRecognitionProcess);
					break; 					

				case AIRequestStates.AIRequestProcess:
					_timerAIProcessing = 0;
					string questionRequest = (string)parameters[1];
					
					SystemEventController.Instance.DispatchSystemEvent(EventTemplate6DOFAIDataAskQuestion, false, questionRequest);
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, false);
					MainController.Instance.GuideTourView.ApplyIdleAnimation();
                    _screenAIView.SetState(AIRequestStates.AIRequestProcess);
					break;

				case AIRequestStates.PlayNarration:
                    Vector3 targetPosition = MainController.Instance.GameInputController.Camera.transform.position + DistanceAppearTourGuide * MainController.Instance.GameInputController.Camera.transform.forward;
                    if (!MainController.Instance.IsNormalAxis)
                    {
                        targetPosition.z = MainController.Instance.GuideTourView.transform.position.z;
                    }
                    else
                    {
                        targetPosition.y = MainController.Instance.GuideTourView.transform.position.y;
                    }
                    MainController.Instance.GuideTourView.SetPositionOutsideNarration(targetPosition);
                    SystemEventController.Instance.DispatchSystemEvent(TourGuideView.EventTourGuideViewSpeakActivation, true);
                    _narrationController.Play(0);
                    SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, true);                        
                    _hasPlayedAnyNarration = true;
                    _screenAIView.SetState(AIRequestStates.PlayNarration);
					break;
			}
		}

        private void DestroyNarration()
        {
            if (_narrationController != null)
            {
                NarrationController narrationController = _narrationController;
                _narrationController = null;
                narrationController.Destroy();
            } 
        }        

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(EventTemplate6DOFAIDataSpeechRecognized))
			{
				bool success = (bool)parameters[0];
                if (success)
                {
                    string speechToTextResult = (string)parameters[1];
                    _screenAIView.SetState(AIRequestStates.Presentation);
                    _screenAIView.SetTextInputField(speechToTextResult);
                }
                else
                {
                    ChangeState(AIRequestStates.None, null);
                }
			}            
			if (nameEvent.Equals(EventTemplate6DOFAIDataCanceledByServer))
			{
				ChangeState(AIRequestStates.None, null);
				string information = LanguageController.Instance.GetText("text.warning");
				string serverErrorDescription = LanguageController.Instance.GetText("screen.ai.interaction.question.server.error");
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, information, serverErrorDescription);
			}
            if (nameEvent.Equals(EventTemplate6DOFAIDataReplyAnswer))
            {
                if (_narrationController != null)
                {
                    _narrationController.Play(0);
                }
            }
			if (nameEvent.Equals(NarrationController.EventNarrationControllerReportTokens))
			{
				string nameClip = (string)parameters[0];
				if (!_sentencesTexts.ContainsKey(nameClip))
				{
					_sentencesTexts.Add(nameClip, (NarrationToken)parameters[1]);
				}
			}
			if (nameEvent.Equals(EventTemplate6DOFAIDataSynthezeCompleted))
			{
				if ((bool)parameters[0])
				{
					string textOrigin = (string)parameters[1];
					AudioClip audioSynth = (AudioClip)parameters[2];
					if (_currentNarrationTokenSynthesize.GetSentence().Equals(textOrigin))
					{
						_currentNarrationTokenSynthesize.Audio = audioSynth;
						if (PopNameClipSyntheziseRequest())
						{
							_narrationController.UpdateNarrationTime();
							ChangeState(AIRequestStates.PlayNarration, null);
						}
					}
				}
				else
				{
					SystemEventController.Instance.DispatchSystemEvent(EventTemplate6DOFAIDataCanceledByServer);
				}
			}
            if (nameEvent.Equals(EventTemplate6DOFAIDataChangeState))
            {
                ChangeState((AIRequestStates)parameters[0], parameters);
            }
            if (nameEvent.Equals(EventTemplate6DOFAIDataAskQuestion))
			{
				bool newConversation = (bool)parameters[0];
				_questionToAI = (string)parameters[1];
                _requestedUserQuestion = true;
                SystemEventController.Instance.DelaySystemEvent(EventTemplate6DOFAIDataRunDelayedAIQuestion, 1);
			}
			if (nameEvent.Equals(EventTemplate6DOFAIDataRunDelayedAIQuestion))
			{
                string history = "{}";
                if (_chatMessages == null)
                {
                    _chatMessages = new List<ChatMessage>();
                    _chatMessages.Clear();
                    string instructions = LanguageController.Instance.GetAIInstructions(LanguageController.Instance.CodeLanguage);
                    _questionToAI = instructions + " " + _questionToAI;
                }
                else
                {
                    ListChatMessages listChatMessage = new ListChatMessages();
				    listChatMessage.Messages = _chatMessages.ToArray();
                    history = JsonUtility.ToJson(listChatMessage, false);
                }
				GameAIData.Instance.AskGenericQuestionHistoryAI("", _questionToAI, history);
                _chatMessages.Add(new ChatMessage(0, _questionToAI));
			}
			if (nameEvent.Equals(AskChatLocalHistoryGPTHTTP.EventGenericAskChatHistoryGPTHTTPCompleted))
			{
				if (_requestedUserQuestion)
				{
					bool success = (bool)parameters[0];
					if (success)
					{
						string dataResponse = (string)parameters[1];
                        dataResponse = dataResponse.Replace("*", "");
#if ENABLE_ANALYTICS
		                TourAnalyticsController.Instance.LogAIQuestionEvent(GameLevelData.Instance.Age, _questionToAI, dataResponse);
#endif		                        
                        _chatMessages.Add(new ChatMessage(1, dataResponse));

                        _sentencesTexts = new Dictionary<string, NarrationToken>();
                        _nameClips = new List<string>();

                        string narrationDataXML = XmlCreator.GetNarrationData(dataResponse, LanguageController.Instance.CodeLanguage);
                        _narrationController = MainController.Instance.CreateNarrationGeneric(narrationDataXML, true, false);
                        _narrationController.ReportSentences();                        
#if ENABLE_SPEECH                        
                        foreach (KeyValuePair<string, NarrationToken> item in _sentencesTexts)
                        {
                            _nameClips.Add(item.Key);
                        }
                        PopNameClipSyntheziseRequest();
#else
						_narrationController.UpdateNarrationTime();
						ChangeState(AIRequestStates.PlayNarration, null);
#endif
					}
					else
					{
						SystemEventController.Instance.DispatchSystemEvent(EventTemplate6DOFAIDataCanceledByServer);
					}
				}
			}
			if (nameEvent.Equals(EventTemplate6DOFAIDataCancelAIRequest))
			{                
                ChangeState(AIRequestStates.None, null);                
			}
            if (nameEvent.Equals(GameAIData.EventGameAIDataCostAIRequest))
            {
                string operation = (string)parameters[0];
                int inputTokens = 0;
                string llm = "";
                if (parameters.Length > 1)
                {
                    inputTokens = (int)parameters[1];
                }
                int outputTokens = 0;
                if (parameters.Length > 2)
                {
                    string outputString = (string)parameters[2];
                    string prettyJson = outputString;
                    bool foundFormat = false;
                    try
                    {
                        JArray jsonArray = JArray.Parse(outputString);
                        prettyJson = jsonArray.ToString(Newtonsoft.Json.Formatting.Indented);
                        foundFormat = true;
                    }
                    catch (Exception err) { };
                    if (!foundFormat)
                    {
                        try
                        {
                            JObject jsonArray = JObject.Parse(outputString);
                            prettyJson = jsonArray.ToString(Newtonsoft.Json.Formatting.Indented);
                            foundFormat = true;
                        }
                        catch (Exception err) { };
                    }
                    outputTokens = prettyJson.Split(' ').Length;
                }
                GameAIData.Instance.AskLastOperationCostAI(operation, llm, inputTokens, outputTokens);
            }        
            if (nameEvent.Equals(GameAIData.EventGameAIDataCostAIResponse))
            {
                try
                {
                    float currentCallCost = (float)parameters[0];
                    if (currentCallCost > 0)
                    {
                        string operation = (string)parameters[1];
                        string llmProvider = (string)parameters[2];
                        int inputTokens = (int)parameters[3];
                        int outputTokens = (int)parameters[4];
                    }
                }
                catch (Exception err) { };
            }
        }

        public void Update()
		{
			switch (_state)
			{
				case AIRequestStates.Presentation:
					break;

				case AIRequestStates.Recording:
					_timerRecording += Time.deltaTime;
					if (_timerRecording > TimeoutMaxRecording)
					{
						ChangeState(AIRequestStates.SpeechRecognitionProcess, null);
					}
					else
					{
                        _screenAIView.SetDescriptionRecording(LanguageController.Instance.GetText("screen.ai.interaction.now.recording", (int)(TimeoutMaxRecording - _timerRecording)));
					}
					break;

				case AIRequestStates.SpeechRecognitionProcess:
					_timerAIProcessing += Time.deltaTime;
					if (_timerAIProcessing > TimeoutCancelAIRequest)
					{						
						ChangeState(AIRequestStates.None, null);
					}
					else
					{
                        _screenAIView.SetDescriptionRecording(LanguageController.Instance.GetText("screen.ai.interaction.now.processing", (int)(TimeoutCancelAIRequest - _timerAIProcessing)));
					}
					break;

				case AIRequestStates.AIRequestProcess:
					_timerAIProcessing += Time.deltaTime;
					if (_timerAIProcessing > TimeoutCancelAIRequest)
					{						
						ChangeState(AIRequestStates.None, null);
					}
					else
					{
                        _screenAIView.SetDescriptionProcessing(LanguageController.Instance.GetText("screen.ai.interaction.now.processing", (int)(TimeoutCancelAIRequest - _timerAIProcessing)));
					}
					break;

				case AIRequestStates.PlayNarration:
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
                    _screenAIView.PositionNarrator();
#endif
					break;
			}
		}
  }
}
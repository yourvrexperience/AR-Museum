using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.ai
{
    public enum AIProvidersLLM { CHAT_GPT = 1, ANTHROPIC = 2, MISTRAL = 3, GOOGLE = 4, GROK = 5, DEEPSEEK = 6, OPENROUTER = 7, LOCAL = 8 }

    [CreateAssetMenu(menuName = "Game/GameAIData")]
	public class GameAIData : ScriptableObject
    {
        public const string EventGameAIDataAIStartRequest = "EventGameAIDataAIStartRequest";
        public const string EventGameAIDataAIEndRequest = "EventGameAIDataAIEndRequest";

        public const string EventGameAIDataCostAIRequest = "EventGameAIDataCostAIRequest";
        public const string EventGameAIDataCostAIResponse = "EventGameAIDataCostAIResponse";

        public const string EventCommChatGPTInitAPIKeysAIHTTP = "yourvrexperience.ai.InitAPIKeysHTTP";
        public const string EventCommChatGPTInitProviderLLMHTTP = "yourvrexperience.ai.InitProviderLLMHTTP";
        public const string EventCommChatGPTGetProviderLLMHTTP = "yourvrexperience.ai.GetProviderLLMHTTP";

        public const string EventCommChatGPTNewConversationHTTP = "yourvrexperience.ai.NewConversationChatGPTHTTP";
        public const string EventCommChatGPTAskQuestionHTTP = "yourvrexperience.ai.AskGenericChatGPTHTTP";
        public const string EventCommChatGPTAskLocalHistoryHTTP = "yourvrexperience.ai.AskChatLocalHistoryGPTHTTP";
        public const string EventCommChatGPTAskChatHistoryHTTP = "yourvrexperience.ai.AskChatHistoryGPTHTTP";
        public const string EventCommChatGPTAskDeleteHistoryHTTP = "yourvrexperience.ai.AskDeleteHistoryGPTHTTP";
        public const string EventCommChatGPTDeleteConversationHTTP = "yourvrexperience.ai.DeleteConversationChatGPTHTTP";
        public const string EventCommConsultAIInstructionsHTTP = "yourvrexperience.ai.ConsultAIInstructionsHTTP";
        public const string EventCommChatGPTAskImageHTTP = "yourvrexperience.ai.AskGenericImageGPTHTTP";
        public const string EventCommChatGPTAskImageDerivationHTTP = "yourvrexperience.ai.AskGenericImageDerivationGPTHTTP";
        public const string EventCommChatGPTAskTTSpeechHTTP = "yourvrexperience.ai.AskGenericTTSpeechGPTHTTP";
        public const string EventCommChatGPTAskTTSpeechDirectHTTP = "yourvrexperience.ai.AskGenericTTSpeechDirectGPTHTTP";
        public const string EventCommChatGPTAskVoiceshHTTP = "yourvrexperience.ai.AskGenericVoicesGPTHTTP";
        public const string EventCommAskGenericTranslationGPTHTTP = "yourvrexperience.ai.AskGenericTranslationGPTHTTP";
        public const string EventCommChatGPTAskSoundFXHTTP = "yourvrexperience.ai.AskGenericSoundFXGPTHTTP";
        public const string EventCommChatGPTAskSoundMusicHTTP = "yourvrexperience.ai.AskGenericSoundMusicGPTHTTP";
        public const string EventCommChatGPTAskDeleteLastCommitHTTP = "yourvrexperience.ai.AskGenericDeleteLastCommitGPTHTTP";
        public const string EventCommChatGPTAskAlignSpeechGPTHTTP = "yourvrexperience.ai.AskGenericAlignSpeechGPTHTTP";
        public const string EventCommAskLastOperationCostGPTHTTP = "yourvrexperience.ai.AskLastOperationCostGPTHTTP";

        public const string AIInstructionsTexts = "aiinstructionstexts";

        [System.Serializable]
        public class AIInstruction
        {
            public string LanguageCode = "";
            public TextAsset Instructions;

            public AIInstruction(string languageCode, string instructions)
            {
                LanguageCode = languageCode;
                Instructions = new TextAsset(instructions);
            }
        }

	    private static GameAIData _instance;
        public static GameAIData Instance
        {
            get { return _instance; }
        }

        [Tooltip("The id of the chatgpt user")]
		[SerializeField] private int chatGPTID = 0;
        [Tooltip("The username of the chatgpt")]
		[SerializeField] private string chatGPTUsername = "esteban";
        [Tooltip("The password of the chatgpt")]
		[SerializeField] private string chatGPTPassword = "XXXXXXX";

        [SerializeField] private string serverChatGPT = "http://192.168.0.108:5000/chatgpt/";
        [SerializeField] private string[] languages;
        [SerializeField] private string urlBaseManagement = "http://192.168.0.108:5000/chatgpt/";
        
        public int ChatGPTID
        {
            get { return chatGPTID; }
            set { chatGPTID = value; }
        }
        public string ChatGPTUsername
        {
            get { return chatGPTUsername; }
            set { chatGPTUsername = value; }
        }
        public string ChatGPTPassword
        {
            get { return chatGPTPassword; }
            set { chatGPTPassword = value; }
        }
        public string ServerChatGPT
        {
            get { return serverChatGPT; }
            set { serverChatGPT = value; }
        }
        public string URLBaseManagement
        {
            get { return urlBaseManagement; }
        }

        public void Initialize()
        {
            _instance = this;
        }

        public void CreateNewConversation(string nameConversation)
        {
            CommController.Instance.Request(EventCommChatGPTNewConversationHTTP, false, nameConversation);
        }

        public void InitAPIKeysAI(string apikey_openai, string apikey_mistral, string apikey_deepseek, string apikey_google, string apikey_grok, string apikey_openrouter, string apikey_stability, string apikey_sceneario, string apikey_elevenlabs, string apikey_lmnt, string apikey_cartesia, string apikey_speechify, string apikey_playht, string server_speech, string server_image, string server_audio)
        {            
            List<ItemMultiTextEntry> headers = new List<ItemMultiTextEntry>();
            headers.Add(new ItemMultiTextEntry("Content-Type", "application/json"));
            CommController.Instance.RequestHeader(EventCommChatGPTInitAPIKeysAIHTTP, headers, false, apikey_openai, apikey_mistral, apikey_deepseek, apikey_google, apikey_grok, apikey_openrouter, apikey_stability, apikey_sceneario, apikey_elevenlabs, apikey_lmnt, apikey_cartesia, apikey_speechify, apikey_playht, server_speech, server_image, server_audio);
        }

        public void InitLLMProvider(AIProvidersLLM aiProviderLLM, string model, TTSpeechProvider speechProvider, float costInput, float costOutput)
        {
            List<ItemMultiTextEntry> headers = new List<ItemMultiTextEntry>();
            headers.Add(new ItemMultiTextEntry("Content-Type", "application/json"));
            CommController.Instance.RequestHeader(EventCommChatGPTInitProviderLLMHTTP, headers, false, aiProviderLLM, model, speechProvider, costInput, costOutput);
        }

        public void GetLLMProvider()
        {
            List<ItemMultiTextEntry> headers = new List<ItemMultiTextEntry>();
            headers.Add(new ItemMultiTextEntry("Content-Type", "application/json"));
            CommController.Instance.RequestHeader(EventCommChatGPTGetProviderLLMHTTP, headers, false);
        }

        public void AskQuestionAI(string conversationID, string question, bool chain, string customEvent = "")
        {
			List<ItemMultiTextEntry> headers = new List<ItemMultiTextEntry>();
			headers.Add(new ItemMultiTextEntry("Content-Type", "application/json"));
            string instructions = LanguageController.Instance.GetAIInstructions(LanguageController.Instance.CodeLanguage);
			CommController.Instance.RequestHeader(EventCommChatGPTAskQuestionHTTP, headers, false, true, conversationID, instructions, question, chain, customEvent);
        }

        public void AskGenericQuestionAI(string conversationID, string question, bool chain, string customEvent = "")
        {
			List<ItemMultiTextEntry> headers = new List<ItemMultiTextEntry>();
			headers.Add(new ItemMultiTextEntry("Content-Type", "application/json"));
			CommController.Instance.RequestHeader(EventCommChatGPTAskQuestionHTTP, headers, false, false, conversationID, "", question, chain, customEvent);
        }

        public void AskGenericQuestionHistoryAI(string conversationID, string question, string history, string customEvent = "")
        {
			List<ItemMultiTextEntry> headers = new List<ItemMultiTextEntry>();
			headers.Add(new ItemMultiTextEntry("Content-Type", "application/json"));
			CommController.Instance.RequestHeader(EventCommChatGPTAskLocalHistoryHTTP, headers, false, false, conversationID, "", question, history, customEvent);
        }

        public void AskChatHistory(string conversationID, string customEvent = "")
        {
            List<ItemMultiTextEntry> headers = new List<ItemMultiTextEntry>();
            headers.Add(new ItemMultiTextEntry("Content-Type", "application/json"));
            CommController.Instance.RequestHeader(EventCommChatGPTAskChatHistoryHTTP, headers, false, conversationID, customEvent);
        }

        public void AskDeleteHistory(string conversationID, string customEvent = "")
        {
            List<ItemMultiTextEntry> headers = new List<ItemMultiTextEntry>();
            headers.Add(new ItemMultiTextEntry("Content-Type", "application/json"));
            CommController.Instance.RequestHeader(EventCommChatGPTAskDeleteHistoryHTTP, headers, false, conversationID, customEvent);
        }

        public void DeleteConversation(string idConversation)
        {
            CommController.Instance.Request(EventCommChatGPTDeleteConversationHTTP, false, idConversation);
        }

        public void ConsultAIInstructions(string language)
        {
            CommController.Instance.Request(EventCommConsultAIInstructionsHTTP, true, language);
        }

        public void AskGenericImageAI(int provider, string description, string exclude = "", int steps = 25, int width = 512, int height = 512, string customEvent = "")
        {
            List<ItemMultiTextEntry> headers = new List<ItemMultiTextEntry>();
            headers.Add(new ItemMultiTextEntry("Content-Type", "application/json"));
            CommController.Instance.RequestHeader(EventCommChatGPTAskImageHTTP, headers, true, provider, description, exclude, steps, width, height, customEvent);
        }
        
        public void AskDerivationImageAI(int provider, string description, string exclude = "", int steps = 25, int width = 512, int height = 512, byte[] imageData = null, string customEvent = "")
        {
            List<ItemMultiTextEntry> headers = new List<ItemMultiTextEntry>();
            headers.Add(new ItemMultiTextEntry("Content-Type", "application/json"));
            CommController.Instance.RequestHeader(EventCommChatGPTAskImageDerivationHTTP, headers, true, provider, description, exclude, steps, width, height, imageData, customEvent);
        }

        public void AskGenericTTSpeechAI(string project, string voice, string speech, string language, string emotion = "", float speed = 1, TTSpeechProvider provider = TTSpeechProvider.AudioCraft, float stability = 0.5f, float similarity = 0.75f, float style = 0, string customEvent = "")
        {
            List<ItemMultiTextEntry> headers = new List<ItemMultiTextEntry>();
            headers.Add(new ItemMultiTextEntry("Content-Type", "application/json"));
            CommController.Instance.RequestHeader(EventCommChatGPTAskTTSpeechHTTP, headers, true, project, voice, speech, language, emotion, speed, provider, stability, similarity, style, customEvent);
        }

        public void AskGenericTTSpeechDirectAI(string voiceId, string speech, string language, string emotion = "", string customEvent = "")
        {
            List<ItemMultiTextEntry> headers = new List<ItemMultiTextEntry>();
            headers.Add(new ItemMultiTextEntry("Content-Type", "application/json"));
            CommController.Instance.RequestHeader(EventCommChatGPTAskTTSpeechDirectHTTP, headers, true, voiceId, speech, language, emotion, customEvent);
        }

        public void AskVoicesTTSpeechAI(string language, TTSpeechProvider provider = TTSpeechProvider.AudioCraft, string customEvent = "")
        {
            List<ItemMultiTextEntry> headers = new List<ItemMultiTextEntry>();
            headers.Add(new ItemMultiTextEntry("Content-Type", "application/json"));
            CommController.Instance.RequestHeader(EventCommChatGPTAskVoiceshHTTP, headers, false, language, provider, customEvent);
        }        

        public void AskGenericSound(bool isFXSound, int provider, string description, int duration, string customEvent = "")
        {            
            List<ItemMultiTextEntry> headers = new List<ItemMultiTextEntry>();
            headers.Add(new ItemMultiTextEntry("Content-Type", "application/json"));
            if (isFXSound)
            {
                CommController.Instance.RequestHeader(EventCommChatGPTAskSoundFXHTTP, headers, true, description, duration, provider, customEvent);
            }
            else
            {
                CommController.Instance.RequestHeader(EventCommChatGPTAskSoundMusicHTTP, headers, true, description, duration, customEvent);
            }            
        }

        public void AskGenericTranslationAI(string conversationID, string instructions, string text, bool chain, bool isJSON, string customEvent = "")
        {
            List<ItemMultiTextEntry> headers = new List<ItemMultiTextEntry>();
            headers.Add(new ItemMultiTextEntry("Content-Type", "application/json"));
            CommController.Instance.RequestHeader(EventCommAskGenericTranslationGPTHTTP, headers, false, conversationID, instructions, text, chain, isJSON, customEvent);
        }

        public void AskDeleteLastCommit(string conversationID, string customEvent = "")
        {
            List<ItemMultiTextEntry> headers = new List<ItemMultiTextEntry>();
            headers.Add(new ItemMultiTextEntry("Content-Type", "application/json"));
            CommController.Instance.RequestHeader(EventCommChatGPTAskDeleteLastCommitHTTP, headers, false, conversationID, customEvent);
        }

        public void AskAlignSpeech(string transcript, string language, byte[] data, string customEvent = "")
        {
            CommController.Instance.Request(EventCommChatGPTAskAlignSpeechGPTHTTP, false, transcript, language, data, customEvent);
        }
        
        public void AskLastOperationCostAI(string operation, string llmProvider, int inputTokens, int outputTokens)
        {
            List<ItemMultiTextEntry> headers = new List<ItemMultiTextEntry>();
            headers.Add(new ItemMultiTextEntry("Content-Type", "application/json"));
            CommController.Instance.RequestHeader(EventCommAskLastOperationCostGPTHTTP, headers, false, operation, llmProvider, inputTokens, outputTokens);
        }
   }
}
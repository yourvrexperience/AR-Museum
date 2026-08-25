using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.UserManagement;
using yourvrexperience.Utils;
using yourvrexperience.Narration;
using yourvrexperience.ai;
using static TMPro.TMP_Dropdown;
#if ENABLE_GOOGLE || ENABLE_FACEBOOK
using yourvrexperience.Social;
#endif

namespace yourvrexperience.template6dof
{
    public enum ServerProviders { CHAT_GPT = 1, ANTHROPIC = 2, MISTRAL = 3, GOOGLE = 4, GROK = 5, DEEPSEEK = 6, OPENROUTER = 7, LOCAL = 8 }

	public class ScreenAISelectorView : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenAISelectorView";
		public const string EventScreenAISelectorViewBack = "EventScreenAISelectorViewBack";

		[SerializeField] private TextMeshProUGUI titleScreen;

		[SerializeField] private TextMeshProUGUI titleProvider;
		[SerializeField] private TMP_Dropdown aiProvider;
		[SerializeField] private TextMeshProUGUI titleModel;
		[SerializeField] private TMP_Dropdown aiModel;
		[SerializeField] private TextMeshProUGUI titleSpeech;
		[SerializeField] private TMP_Dropdown aiSpeech;
		[SerializeField] private Button buttonSetProvider;

		[SerializeField] private Button buttonBack;

		private AIProviders _selectorProvider;
		private TTSpeechProvider _selectorSpeech;
		private string _selectorModel;

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			titleScreen.text = LanguageController.Instance.GetText("screen.ai.selector.title");
			titleProvider.text = LanguageController.Instance.GetText("screen.ai.selector.provider");
			titleModel.text = LanguageController.Instance.GetText("screen.ai.selector.model");
			titleSpeech.text = LanguageController.Instance.GetText("screen.ai.selector.speech");

			aiProvider.ClearOptions();
			aiProvider.value = 0;
			aiProvider.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("text.provider.openai")));
			aiProvider.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("text.provider.mistral")));
			aiProvider.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("text.provider.google")));
			aiProvider.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("text.provider.deepseek")));
			aiProvider.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("text.provider.openrouter")));
			aiProvider.options.Add(new TMP_Dropdown.OptionData("EMPTY"));
			aiProvider.onValueChanged.AddListener(OnAIProviderSelected);

			aiSpeech.ClearOptions();
			aiSpeech.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("text.provider.elevenlabs")));
			aiSpeech.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("text.provider.speechify")));
			aiSpeech.options.Add(new TMP_Dropdown.OptionData("EMPTY"));
			aiSpeech.onValueChanged.AddListener(OnAISpeechSelected);

			aiModel.onValueChanged.AddListener(OnAIModelSelected);
			buttonSetProvider.onClick.AddListener(OnSetProviderModel);
			buttonSetProvider.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("text.provider.confirmation");

			buttonBack.onClick.AddListener(OnButtonBack);

			SystemEventController.Instance.Event += OnSystemEvent;
			GameAIData.Instance.GetLLMProvider();

			titleModel.gameObject.SetActive(false);
			aiModel.gameObject.SetActive(false);
			buttonSetProvider.gameObject.SetActive(false);
		}

        public override void Destroy()
		{
			base.Destroy();

			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

        private void OnButtonBack()
        {
			SoundsController.Instance.PlaySoundFX(GameSounds.FxSelection, false, 1);
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
        }

        private void OnAIProviderSelected(int value)
        {
			_selectorProvider = (AIProviders)value;
			_selectorModel = "";
			aiModel.ClearOptions();			
            switch (_selectorProvider)
			{
				case AIProviders.OpenAI:			
					aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("gpt-5.4-nano-2026-03-17")));		
					aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("gpt-5.4-mini-2026-03-17")));		
					aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("none")));	
					break;

				case AIProviders.Mistral:
					aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("mistral-small-2603")));		
					aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("mistral-medium-3-5")));		
					aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("none")));	
					break;

				case AIProviders.Gemini:
                    aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("gemini-3.1-flash-lite")));		
					aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("gemini-3.5-flash")));		
					aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("none")));	
					break;

				case AIProviders.DeepSeek:
					aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("deepseek-v4-flash")));		
					aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("deepseek-v4-pro")));		
					aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("none")));	
					break;

				case AIProviders.Grok:
                    aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("grok-4.20-0309-non-reasoning")));		
					aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("grok-4.3")));		
					aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("none")));	
					break;

				case AIProviders.OpenRouter:
					aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("openai/gpt-4o-mini")));
					aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("mistralai/mistral-nemo")));
					aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("google/gemini-2.0-flash-001")));
					aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("openai/chatgpt-4o-latest")));		
					aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("mistralai/mistral-large")));		
					aiModel.options.Add(new TMP_Dropdown.OptionData(LanguageController.Instance.GetText("google/gemini-pro-1.5")));
					break;
			}
			aiModel.value = 1;			
			aiModel.value = 0;			
			titleModel.gameObject.SetActive(true);
			aiModel.gameObject.SetActive(true);
        }

        private void OnAIModelSelected(int value)
        {
			_selectorModel = aiModel.options[value].text;			
			buttonSetProvider.gameObject.SetActive(true);
			Debug.LogError("OnAIModelSelected::_selectorModel="+_selectorModel);
        }

        private void OnAISpeechSelected(int value)
        {	
			buttonSetProvider.gameObject.SetActive(true);
			switch (value)
			{
				case 0:
					_selectorSpeech = TTSpeechProvider.ElevenLabs;
					break;

				case 1:
					_selectorSpeech = TTSpeechProvider.Speechify;
					break;
			}            
        }

        private void OnSetProviderModel()
        {
			AIProvidersLLM provider = AIProvidersLLM.MISTRAL;
			switch (_selectorProvider)
			{
				case AIProviders.OpenAI:
					provider = AIProvidersLLM.CHAT_GPT;
					break;
				case AIProviders.Mistral:
					provider = AIProvidersLLM.MISTRAL;
					break;
				case AIProviders.Gemini:
					provider = AIProvidersLLM.GOOGLE;
					break;
				case AIProviders.Grok:
					provider = AIProvidersLLM.GROK;
					break;
				case AIProviders.DeepSeek:
					provider = AIProvidersLLM.DEEPSEEK;
					break;
				case AIProviders.OpenRouter:
					provider = AIProvidersLLM.OPENROUTER;
					break;
			}

			buttonSetProvider.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("text.provider.now.working");
			buttonSetProvider.enabled = false;
			GameAIData.Instance.InitLLMProvider(provider, _selectorModel, _selectorSpeech, 0, 0);
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(InitProviderLLMHTTP.EventInitProviderLLMHTTPCompleted))
			{
				string messageConfirmation = "";
				if ((bool)parameters[0])
				{
					messageConfirmation = LanguageController.Instance.GetText("text.provider.success");
				}
				else
				{
					messageConfirmation = LanguageController.Instance.GetText("text.provider.failure");
				}
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, "", messageConfirmation);				
				buttonSetProvider.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("text.provider.confirmation");
				buttonSetProvider.gameObject.SetActive(false);
			}
			if (nameEvent.Equals(GetProviderLLMHTTP.EventGetProviderLLMHTTPCompleted))
			{
				if ((bool)parameters[0])
				{
					ProviderModelData providerData = (ProviderModelData)parameters[1];
					switch ((ServerProviders)providerData.provider)
					{
						case ServerProviders.CHAT_GPT:
							aiProvider.value = 0;
							break;
						case ServerProviders.ANTHROPIC:
							break;
						case ServerProviders.MISTRAL:
							aiProvider.value = 1;
							break;
						case ServerProviders.GOOGLE:
							aiProvider.value = 2;
							break;
						case ServerProviders.GROK:
							break;
						case ServerProviders.DEEPSEEK:
							aiProvider.value = 3;
							break;
						case ServerProviders.OPENROUTER:
							aiProvider.value = 4;
							break;
					}
					titleModel.gameObject.SetActive(true);
					aiModel.gameObject.SetActive(true);
					int counter = 0;
					foreach (OptionData option in aiModel.options)
					{
						if (option.text.Equals(providerData.model))
						{
							aiModel.value = counter;
							break;
						}
						counter++;
					}

					switch ((TTSpeechProvider)providerData.speech)
					{
						case TTSpeechProvider.ElevenLabs: 
							aiSpeech.value = 0;
							break;
						case TTSpeechProvider.OpenAI: 
							break;
						case TTSpeechProvider.Speechify: 
							aiSpeech.value = 1;
							break;
					}

					buttonSetProvider.gameObject.SetActive(false);
				}				
			}
        }
	}
}
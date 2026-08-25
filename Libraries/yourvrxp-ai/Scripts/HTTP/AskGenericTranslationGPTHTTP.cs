using System;
using Newtonsoft.Json;
#if ENABLE_OFUSCATION
#if ENABLE_NEW_OFUSCATION
using GUPS.Obfuscator.Attribute;
#else
using OPS.Obfuscator.Attribute;
#endif
#endif
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.ai
{
#if ENABLE_OFUSCATION
	[DoNotRenameAttribute]
#endif
	public class AskGenericTranslationGPTHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string EventGenericAskTranslationGPTHTTPCompleted = "EventGenericAskTranslationGPTHTTPCompleted";

		[Serializable]
		public class TranslateToken
		{
			public string originaltext;
			public string translatedtext;
		}

		public const string translationEnglishTokenJsonString = @"{
            ""originaltext"": ""Text that needs to be translated"",
            ""translatedtext"": ""The translation of the text""
        }";

		public const string translationSpanishTokenJsonString = @"{
            ""originaltext"": ""Texto que necesita ser traducido"",
            ""translatedtext"": ""La traducción del texto""
        }";

		public const string translationGermanTokenJsonString = @"{
            ""originaltext"": ""Text, der übersetzt werden muss"",
            ""translatedtext"": ""Die Übersetzung des Textes""
        }";

		public const string translationFrenchTokenJsonString = @"{
            ""originaltext"": ""Texte à traduire"",
            ""translatedtext"": ""La traduction du texte""
        }";

		public const string translationItalianTokenJsonString = @"{
            ""originaltext"": ""Testo che deve essere tradotto"",
            ""translatedtext"": ""La traduzione del testo""
        }";

		public const string translationRussianTokenJsonString = @"{
            ""originaltext"": ""Текст, который нужно перевести"",
            ""translatedtext"": ""Перевод текста""
        }";

		public const string translationCatalanTokenJsonString = @"{
            ""originaltext"": ""Texte que necesita ser traduit"",
            ""translatedtext"": ""La traducció del texte""
        }";

		private string _customEvent;
		private string _text;

		private bool _isJSON = false;

		public string UrlRequest
		{			            
			get { return GameAIData.Instance.ServerChatGPT + "translation_text?debug=true"; }
        }

        public string Build(params object[] _list)
		{
			string conversationID = (string)_list[0];
			string instructions = (string)_list[1];
			_text = (string)_list[2];
			bool chain = (bool)_list[3];
			_isJSON = (bool)_list[4];
			_customEvent = (string)_list[5];

			if (_isJSON)
            {
				switch (LanguageController.Instance.CodeLanguage)
				{
					case LanguageController.CodeLanguageEnglish:
						instructions += translationEnglishTokenJsonString;
						break;
					case LanguageController.CodeLanguageSpanish:
						instructions += translationSpanishTokenJsonString;
						break;
					case LanguageController.CodeLanguageGerman:
						instructions += translationGermanTokenJsonString;
						break;
					case LanguageController.CodeLanguageFrench:
						instructions += translationFrenchTokenJsonString;
						break;
					case LanguageController.CodeLanguageItalian:
						instructions += translationItalianTokenJsonString;
						break;
					case LanguageController.CodeLanguageRussian:
						instructions += translationRussianTokenJsonString;
						break;
					case LanguageController.CodeLanguageCatalan:
						instructions += translationCatalanTokenJsonString;
						break;
				}
			}
			
			_method = METHOD_POST;
			_formPost = null;
			_rawData = System.Text.Encoding.UTF8.GetBytes(
					JsonConvert.SerializeObject(new ChatGPTRequestJSON
					{
						UserID = GameAIData.Instance.ChatGPTID,
						Username = GameAIData.Instance.ChatGPTUsername,
						Password = GameAIData.Instance.ChatGPTPassword,
						ConversationId = conversationID,
						Instructions = instructions,
						Question = _text,
						Chain = chain,
						IsJSON = _isJSON,
						Debug = false
					}));

			return null;
        }

		public override void Response(string _response)
		{
			if (_cancelResponse) return;

			if ((_response == null) || (_response.Length == 0))
			{
				if (_customEvent.Length > 0)
				{
					SystemEventController.Instance.DispatchSystemEvent(_customEvent, false, _text);
				}
				else
                {
					SystemEventController.Instance.DispatchSystemEvent(EventGenericAskTranslationGPTHTTPCompleted, false, _text);
				}
				return;
			}

			string finalTranslation = "";
			if (_isJSON)
            {
				TranslateToken chapters = JsonUtility.FromJson<TranslateToken>(_response);
				finalTranslation = chapters.translatedtext;
			}
			else
            {
				finalTranslation = _response;
			}

			// Get Response list
			if (_customEvent.Length > 0)
			{
				SystemEventController.Instance.DispatchSystemEvent(_customEvent, true, _text, finalTranslation);
			}
			else
			{
				SystemEventController.Instance.DispatchSystemEvent(EventGenericAskTranslationGPTHTTPCompleted, true, _text, finalTranslation);
			}
		}
	}
}
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
	public class AskGenericAlignSpeechGPTHTTP : BaseDataHTTP, IHTTPComms
	{		
		public const string EventAskGenericAlignSpeechGPTHTTPCompleted = "EventAskGenericAlignSpeechGPTHTTPCompleted";

		private string _customEvent;

		public string UrlRequest
		{			            
			get { return GameAIData.Instance.ServerChatGPT + "align_audio?debug=true"; }
        }

        public string Build(params object[] _list)
		{
			_customEvent = (string)_list[3];

			_method = METHOD_POST;
			_formPost = new WWWForm();

			_formPost.AddField("userid", GameAIData.Instance.ChatGPTID);
			_formPost.AddField("username", GameAIData.Instance.ChatGPTUsername);
			_formPost.AddField("password", GameAIData.Instance.ChatGPTPassword);
			string textToAlign = (string)_list[0];
			if (LanguageController.Instance.CodeLanguage == LanguageController.CodeLanguageEnglish)
            {
				textToAlign = NumberToWordsConverter.ReplaceNumbersWithWords(textToAlign);
			}
			_formPost.AddField("transcript", textToAlign);
			_formPost.AddField("language", (string)_list[1]);

			byte[] audioData = (byte[])_list[2];
			_formPost.AddBinaryData("audio", audioData);

            return null;
        }

		public override void Response(string _response)
		{
			if (_cancelResponse) return;

			if ((_response == null) || (_response.Length == 0))
			{
				if (_customEvent.Length > 0)
				{
					SystemEventController.Instance.DispatchSystemEvent(_customEvent, false);
				}
				else
                {
					SystemEventController.Instance.DispatchSystemEvent(EventAskGenericAlignSpeechGPTHTTPCompleted, false);
				}
				return;
			}

			// Get Response list
			if (_customEvent.Length > 0)
			{
				SystemEventController.Instance.DispatchSystemEvent(_customEvent, true, _response);
			}
			else
			{
				SystemEventController.Instance.DispatchSystemEvent(EventAskGenericAlignSpeechGPTHTTPCompleted, true, _response);
			}
		}
	}
}
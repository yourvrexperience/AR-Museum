using System.Text;
using Newtonsoft.Json;
#if ENABLE_OFUSCATION
#if ENABLE_NEW_OFUSCATION
using GUPS.Obfuscator.Attribute;
#else
using OPS.Obfuscator.Attribute;
#endif
#endif
using yourvrexperience.Utils;

namespace yourvrexperience.ai
{
	public class TTSpeechDirectGPTRequest
	{
		[JsonProperty(PropertyName = "userid")]
		public int UserID { get; set; }

		[JsonProperty(PropertyName = "username")]
		public string Username { get; set; }

		[JsonProperty(PropertyName = "password")]
		public string Password { get; set; }
		[JsonProperty(PropertyName = "voice")]
		public string Voice { get; set; }

		[JsonProperty(PropertyName = "speech")]
		public string Speech { get; set; }

		[JsonProperty(PropertyName = "language")]
		public string Language { get; set; }

		[JsonProperty(PropertyName = "emotion")]
		public string Emotion { get; set; }
	}

#if ENABLE_OFUSCATION
	[DoNotRenameAttribute]
#endif
	public class AskGenericTTSpeechDirectGPTHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string EventGenericAskTTSpeechDirectGPTHTTPCompleted = "EventGenericAskTTSpeechDirectGPTHTTPCompleted";

		private string _customEvent;
		private string _speech;

		public string UrlRequest
		{			            
			get { return GameAIData.Instance.ServerChatGPT + "speech_direct?debug=true"; }
        }

        public string Build(params object[] _list)
		{
			string voiceId = (string)_list[0];
			_speech = (string)_list[1];
			string language = (string)_list[2];
			string emotion = (string)_list[3];
			_customEvent = (string)_list[4];

			_method = METHOD_POST;
			_formPost = null;
			_rawData = System.Text.Encoding.UTF8.GetBytes(
					JsonConvert.SerializeObject(new TTSpeechDirectGPTRequest
					{
						UserID = GameAIData.Instance.ChatGPTID,
						Username = GameAIData.Instance.ChatGPTUsername,
						Password = GameAIData.Instance.ChatGPTPassword,
						Voice = voiceId,
						Speech = _speech,
						Language = language,
						Emotion = emotion
					}));

            return null;
        }

		public override void Response(string _response)
		{
			if (_cancelResponse) return;

			if (!ResponseCode(_response))
			{
				if (_customEvent.Length > 0)
				{
					SystemEventController.Instance.DispatchSystemEvent(_customEvent, false, _speech);
				}
				else
				{
					SystemEventController.Instance.DispatchSystemEvent(EventGenericAskTTSpeechDirectGPTHTTPCompleted, false, _speech);
				}
			}
		}

		public override void Response(byte[] _response)
		{
			if (_cancelResponse) return;

			if ((_response == null) || (_response.Length == 0) || (Encoding.UTF8.GetString(_response).IndexOf("Error") != -1))
			{
				if (_customEvent.Length > 0)
				{
					SystemEventController.Instance.DispatchSystemEvent(_customEvent, false, _speech);
				}
				else
                {
					SystemEventController.Instance.DispatchSystemEvent(EventGenericAskTTSpeechDirectGPTHTTPCompleted, false, _speech);
				}
				return;
			}

			// Get Response list
			if (_customEvent.Length > 0)
			{
				SystemEventController.Instance.DispatchSystemEvent(_customEvent, true, _speech, _response);
			}
			else
			{
				SystemEventController.Instance.DispatchSystemEvent(EventGenericAskTTSpeechDirectGPTHTTPCompleted, true, _speech, _response);
			}
		}
	}
}
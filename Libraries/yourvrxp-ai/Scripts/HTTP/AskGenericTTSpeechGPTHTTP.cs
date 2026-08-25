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
	public enum TTSpeechProvider { None = -1, AudioCraft = 0, ElevenLabs = 1, OpenAI = 2, LMNT = 3, Cartesia = 4, Speechify = 5, PlayHT = 6 }

	public class TTSpeechGPTRequest
	{
		[JsonProperty(PropertyName = "userid")]
		public int UserID { get; set; }

		[JsonProperty(PropertyName = "username")]
		public string Username { get; set; }

		[JsonProperty(PropertyName = "password")]
		public string Password { get; set; }
		[JsonProperty(PropertyName = "project")]
		public string Project { get; set; }

		[JsonProperty(PropertyName = "voice")]
		public string Voice { get; set; }

		[JsonProperty(PropertyName = "speech")]
		public string Speech { get; set; }

		[JsonProperty(PropertyName = "language")]
		public string Language { get; set; }

		[JsonProperty(PropertyName = "emotion")]
		public string Emotion { get; set; }

		[JsonProperty(PropertyName = "speed")]
		public float Speed { get; set; }
		[JsonProperty(PropertyName = "provider")]
		public int Provider { get; set; }
		[JsonProperty(PropertyName = "stability")]
		public float Stability { get; set; }
		[JsonProperty(PropertyName = "similarity")]
		public float Similarity { get; set; }
		[JsonProperty(PropertyName = "style")]
		public float Style { get; set; }
	}

#if ENABLE_OFUSCATION
	[DoNotRenameAttribute]
#endif
	public class AskGenericTTSpeechGPTHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string EventGenericAskTTSpeechGPTHTTPCompleted = "EventGenericAskTTSpeechGPTHTTPCompleted";

		private string _customEvent;
		private string _speech;

		public string UrlRequest
		{			            
			get { return GameAIData.Instance.ServerChatGPT + "speech?debug=true"; }
        }

        public string Build(params object[] _list)
		{
			string project = (string)_list[0];
			string voice = (string)_list[1];
			_speech = (string)_list[2];
			string language = (string)_list[3];
			string emotion = (string)_list[4];
			float speed = (float)_list[5];
			int provider = (int)((TTSpeechProvider)_list[6]);
			float stability = (float)_list[7];
			float similarity = (float)_list[8];
			float style = (float)_list[9];
			_customEvent = (string)_list[10];

			_method = METHOD_POST;
			_formPost = null;
			_rawData = System.Text.Encoding.UTF8.GetBytes(
					JsonConvert.SerializeObject(new TTSpeechGPTRequest
					{
						UserID = GameAIData.Instance.ChatGPTID,
						Username = GameAIData.Instance.ChatGPTUsername,
						Password = GameAIData.Instance.ChatGPTPassword,
						Project = project,
						Voice = voice,
						Speech = _speech,
						Language = language,
						Emotion = emotion,
						Speed = speed,
						Provider = provider,
						Stability = stability,
						Similarity = similarity,
						Style = style
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
					SystemEventController.Instance.DispatchSystemEvent(EventGenericAskTTSpeechGPTHTTPCompleted, false, _speech);
				}
			}
		}

		public override void Response(byte[] _response)
		{
			if (_cancelResponse) return;

			if ((_response == null) || (_response.Length == 0))
			{
				if (_customEvent.Length > 0)
				{
					SystemEventController.Instance.DispatchSystemEvent(_customEvent, false, _speech);
				}
				else
                {
					SystemEventController.Instance.DispatchSystemEvent(EventGenericAskTTSpeechGPTHTTPCompleted, false, _speech);
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
				SystemEventController.Instance.DispatchSystemEvent(EventGenericAskTTSpeechGPTHTTPCompleted, true, _speech, _response);
			}
		}
	}
}
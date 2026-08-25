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
	public class InitAPIKeys
	{
		[JsonProperty(PropertyName = "userid")]
		public int userid { get; set; }

		[JsonProperty(PropertyName = "username")]
		public string username { get; set; }

		[JsonProperty(PropertyName = "password")]
		public string password { get; set; }

		[JsonProperty(PropertyName = "apikey_openai")]
		public string apikey_openai { get; set; }

		[JsonProperty(PropertyName = "apikey_mistral")]
		public string apikey_mistral { get; set; }

		[JsonProperty(PropertyName = "apikey_deepseek")]
		public string apikey_deepseek { get; set; }

		[JsonProperty(PropertyName = "apikey_google")]
		public string apikey_google { get; set; }
		[JsonProperty(PropertyName = "apikey_grok")]
		public string apikey_grok { get; set; }

		[JsonProperty(PropertyName = "apikey_openrouter")]
		public string apikey_openrouter { get; set; }

		[JsonProperty(PropertyName = "apikey_stability")]
		public string apikey_stability { get; set; }

		[JsonProperty(PropertyName = "apikey_sceneario")]
		public string apikey_sceneario { get; set; }

		[JsonProperty(PropertyName = "apikey_elevenlabs")]
		public string apikey_elevenlabs { get; set; }

		[JsonProperty(PropertyName = "apikey_lmnt")]
		public string apikey_lmnt { get; set; }

		[JsonProperty(PropertyName = "apikey_cartesia")]
		public string apikey_cartesia { get; set; }

		[JsonProperty(PropertyName = "apikey_speechify")]
		public string apikey_speechify { get; set; }

		[JsonProperty(PropertyName = "apikey_playht")]
		public string apikey_playht { get; set; }

		[JsonProperty(PropertyName = "speech_server")]
		public string speech_server { get; set; }

		[JsonProperty(PropertyName = "image_server")]
		public string image_server { get; set; }

		[JsonProperty(PropertyName = "audio_server")]
		public string audio_server { get; set; }
		
		public bool Debug { get; set; }

	}


#if ENABLE_OFUSCATION
	[DoNotRenameAttribute]
#endif
	public class InitAPIKeysHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string EventInitAPIKeysHTTPRequested = "EventInitAPIKeysHTTPRequested";
		public const string EventInitAPIKeysHTTPCompleted = "EventInitAPIKeysHTTPCompleted";
		
		public string UrlRequest
		{			            
			get { return GameAIData.Instance.ServerChatGPT + "init_api_keys?debug=true"; }
        }

        public string Build(params object[] _list)
		{
			_method = METHOD_POST;
			_formPost = null;
			_rawData = System.Text.Encoding.UTF8.GetBytes(
					JsonConvert.SerializeObject(new InitAPIKeys
					{
						userid = GameAIData.Instance.ChatGPTID,
						username = GameAIData.Instance.ChatGPTUsername,
						password = GameAIData.Instance.ChatGPTPassword,
						apikey_openai = (string)_list[0],
						apikey_mistral = (string)_list[1],
						apikey_deepseek = (string)_list[2],
						apikey_google = (string)_list[3],
						apikey_grok = (string)_list[4],
						apikey_openrouter = (string)_list[5],
						apikey_stability = (string)_list[6],
						apikey_sceneario = (string)_list[7],
						apikey_elevenlabs = (string)_list[8],
						apikey_lmnt = (string)_list[9],
						apikey_cartesia = (string)_list[10],
						apikey_speechify = (string)_list[11],
						apikey_playht = (string)_list[12],
						speech_server = (string)_list[13],
						image_server = (string)_list[14],
						audio_server = (string)_list[15]
					}));

			return null;
        }

        public override void Response(string _response)
		{
			if (_cancelResponse) return;

			if (!ResponseCode(_response))
			{				
				SystemEventController.Instance.DispatchSystemEvent(EventInitAPIKeysHTTPCompleted, false);
				return;
			}

			SystemEventController.Instance.DispatchSystemEvent(EventInitAPIKeysHTTPCompleted, true);
		}
	}

}
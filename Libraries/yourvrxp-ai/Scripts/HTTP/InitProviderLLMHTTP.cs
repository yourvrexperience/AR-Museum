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
	public class InitProviderLLM
	{
		[JsonProperty(PropertyName = "userid")]
		public int userid { get; set; }

		[JsonProperty(PropertyName = "username")]
		public string username { get; set; }

		[JsonProperty(PropertyName = "password")]
		public string password { get; set; }
		[JsonProperty(PropertyName = "provider")]
		public int provider { get; set; }

		[JsonProperty(PropertyName = "model")]
		public string model { get; set; }
		[JsonProperty(PropertyName = "speech")]
		public int speech { get; set; }		
		[JsonProperty(PropertyName = "costinput")]
		public float costinput { get; set; }
		[JsonProperty(PropertyName = "costoutput")]
		public float costoutput { get; set; }
	}


#if ENABLE_OFUSCATION
	[DoNotRenameAttribute]
#endif
	public class InitProviderLLMHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string EventInitProviderLLMHTTPRequested = "EventInitProviderLLMHTTPRequested";
		public const string EventInitProviderLLMHTTPCompleted = "EventInitProviderLLMHTTPCompleted";

		public string UrlRequest
		{			            
			get { return GameAIData.Instance.ServerChatGPT + "set_provider_llm?debug=true"; }
        }

        public string Build(params object[] _list)
		{
			_method = METHOD_POST;
			_formPost = null;
			_rawData = System.Text.Encoding.UTF8.GetBytes(
					JsonConvert.SerializeObject(new InitProviderLLM
					{
						userid = GameAIData.Instance.ChatGPTID,
						username = GameAIData.Instance.ChatGPTUsername,
						password = GameAIData.Instance.ChatGPTPassword,
						provider = (int)_list[0],
						model = (string)_list[1],
						speech = (int)_list[2],
						costinput = (float)_list[3],
						costoutput = (float)_list[4]
					}));

			return null;
        }

        public override void Response(string _response)
		{
			if (_cancelResponse) return;

			if (!ResponseCode(_response))
			{				
				SystemEventController.Instance.DispatchSystemEvent(EventInitProviderLLMHTTPCompleted, false);
				return;
			}

			SystemEventController.Instance.DispatchSystemEvent(EventInitProviderLLMHTTPCompleted, true);
		}
	}

}
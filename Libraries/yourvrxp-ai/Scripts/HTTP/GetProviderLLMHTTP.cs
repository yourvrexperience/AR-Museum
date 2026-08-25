using Newtonsoft.Json;
using UnityEngine;

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
	public class CredentialsLLM
	{
		[JsonProperty(PropertyName = "userid")]
		public int userid { get; set; }

		[JsonProperty(PropertyName = "username")]
		public string username { get; set; }

		[JsonProperty(PropertyName = "password")]
		public string password { get; set; }
	}

	[System.Serializable]
	public class ProviderModelData
	{
		public bool success;
		public int provider;
		public string model;
		public int speech;
	}

#if ENABLE_OFUSCATION
	[DoNotRenameAttribute]
#endif
	public class GetProviderLLMHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string EventGetProviderLLMHTTPCompleted = "EventGetProviderLLMHTTPCompleted";

		public string UrlRequest
		{			            
			get { return GameAIData.Instance.ServerChatGPT + "get_provider_llm?debug=true"; }
        }

        public string Build(params object[] _list)
		{
			_method = METHOD_POST;
			_formPost = null;
			_rawData = System.Text.Encoding.UTF8.GetBytes(
					JsonConvert.SerializeObject(new CredentialsLLM
					{
						userid = GameAIData.Instance.ChatGPTID,
						username = GameAIData.Instance.ChatGPTUsername,
						password = GameAIData.Instance.ChatGPTPassword
					}));

			return null;
        }

        public override void Response(string _response)
		{
			if (_cancelResponse) return;

			if (!ResponseCode(_response))
			{				
				SystemEventController.Instance.DispatchSystemEvent(EventGetProviderLLMHTTPCompleted, false);
				return;
			}

			ProviderModelData providerData = JsonUtility.FromJson<ProviderModelData>(_response);
			SystemEventController.Instance.DispatchSystemEvent(EventGetProviderLLMHTTPCompleted, true, providerData);
		}
	}

}
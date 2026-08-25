#if ENABLE_OFUSCATION
#if ENABLE_NEW_OFUSCATION
using GUPS.Obfuscator.Attribute;
#else
using OPS.Obfuscator.Attribute;
#endif
#endif
using Newtonsoft.Json;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.ai
{
#if ENABLE_OFUSCATION
	[DoNotRenameAttribute]
#endif
	public class AskLastOperationCostGPTHTTP : BaseDataHTTP, IHTTPComms
	{
		private string _operation = "";
		private string _llmProvider = "";
		private int _inputItemsNumber = -1;
		private int _outputItemsNumber = -1;

		public string UrlRequest
		{
			get { return GameAIData.Instance.ServerChatGPT + "question/last_cost?debug=true"; }
		}

		public string Build(params object[] _list)
		{
			_operation = (string)_list[0];
			_llmProvider = (string)_list[1];
			_inputItemsNumber = (int)_list[2];
			_outputItemsNumber = (int)_list[3];

			_method = METHOD_POST;
			_formPost = null;
			_rawData = System.Text.Encoding.UTF8.GetBytes(
					JsonConvert.SerializeObject(new ChatGPTRequest
					{
						UserID = GameAIData.Instance.ChatGPTID,
						Username = GameAIData.Instance.ChatGPTUsername,
						Password = GameAIData.Instance.ChatGPTPassword,
						ConversationId = "",
						Instructions = "",
						Question = "",
						Debug = false
					}));


			return null;
		}

		public override void Response(string _response)
		{
			if (_cancelResponse) return;


			if (!ResponseCode(_response))
			{
				return;
			}

			LLMResponse llmResponse = JsonUtility.FromJson<LLMResponse>(_response);
			SystemEventController.Instance.DispatchSystemEvent(GameAIData.EventGameAIDataCostAIResponse, llmResponse.cost / 1000f, _operation, _llmProvider, _inputItemsNumber, _outputItemsNumber);
		}
	}
}
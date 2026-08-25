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
	public class ChatGPTRequest
	{
		[JsonProperty(PropertyName = "userid")]
		public int UserID { get; set; }

		[JsonProperty(PropertyName = "username")]
		public string Username { get; set; }

		[JsonProperty(PropertyName = "password")]
		public string Password { get; set; }
		[JsonProperty(PropertyName = "conversationid")]
		public string ConversationId { get; set; }

		[JsonProperty(PropertyName = "question")]
		public string Question { get; set; }
		[JsonProperty(PropertyName = "instructions")]
		public string Instructions { get; set; }
		[JsonProperty(PropertyName = "chain")]
		public bool Chain { get; set; }
		[JsonProperty(PropertyName = "debug")]
		public bool Debug { get; set; }

	}

	public class ChatGPTRequestJSON : ChatGPTRequest
	{
		[JsonProperty(PropertyName = "isjson")]
		public bool IsJSON { get; set; }

	}

	[System.Serializable]
	public class LLMResponse
	{
		public float cost;
		public string response;
	}

#if ENABLE_OFUSCATION
	[DoNotRenameAttribute]
#endif
	public class AskGenericChatGPTHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string EventGenericAskChatGPTHTTPQuestionText = "EventAskGenericChatGPTHTTPQuestionText";
		public const string EventGenericAskChatGPTHTTPCompleted = "EventAskChatGenericGPTHTTPCompleted";

		private string _customEvent;
		private int _inputTokens = 0;

		public string UrlRequest
		{			            
			get { return GameAIData.Instance.ServerChatGPT + "question?debug=true"; }
        }

        public string Build(params object[] _list)
		{
			bool addQuestionCharacter = (bool)_list[0];
			string conversationID = (string)_list[1];
			string instructions = (string)_list[2];
			string question = (string)_list[3];
			bool chain = (bool)_list[4];
			_customEvent = (string)_list[5];

			if (addQuestionCharacter)
			{
				if (question.IndexOf('?') == -1)
				{
					question += "?";
				}
			}

			_method = METHOD_POST;
			_formPost = null;
			string fullInstructions = question;
			if (instructions.Length > 0)
			{
				fullInstructions = instructions + "\n\n" + question;
			}
			_inputTokens = fullInstructions.Split(' ').Length;
			_rawData = System.Text.Encoding.UTF8.GetBytes(
					JsonConvert.SerializeObject(new ChatGPTRequest
					{
						UserID = GameAIData.Instance.ChatGPTID,
						Username = GameAIData.Instance.ChatGPTUsername,
						Password = GameAIData.Instance.ChatGPTPassword,
						ConversationId = conversationID,
						Instructions = "",
						Question = fullInstructions,
						Chain = chain,
						Debug = false
					}));

			SystemEventController.Instance.DispatchSystemEvent(GameAIData.EventGameAIDataAIStartRequest, "question", fullInstructions);

			return null;
        }

        public override void Response(string _response)
		{
			if (_cancelResponse) return;

			if (!ResponseCode(_response))
			{				
				if (_customEvent.Length > 0)
				{
					SystemEventController.Instance.DispatchSystemEvent(_customEvent, false);
				}
				else
                {
					SystemEventController.Instance.DispatchSystemEvent(EventGenericAskChatGPTHTTPCompleted, false);
				}
				return;
			}

			// Get Response list
			SystemEventController.Instance.DispatchSystemEvent(GameAIData.EventGameAIDataAIEndRequest, "question", _response);
			SystemEventController.Instance.DelaySystemEvent(GameAIData.EventGameAIDataCostAIRequest, 0.2f, "question", _inputTokens, _response);
			if (_customEvent.Length > 0)
			{
				SystemEventController.Instance.DispatchSystemEvent(_customEvent, true, _response);
			}
			else
			{
				SystemEventController.Instance.DispatchSystemEvent(EventGenericAskChatGPTHTTPCompleted, true, _response);
			}
		}
	}

}
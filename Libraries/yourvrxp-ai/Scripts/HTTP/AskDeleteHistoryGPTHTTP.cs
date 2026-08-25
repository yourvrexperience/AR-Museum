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
	public class DeleteHistoryGPTRequest
	{
		[JsonProperty(PropertyName = "userid")]
		public int UserID { get; set; }

		[JsonProperty(PropertyName = "username")]
		public string Username { get; set; }

		[JsonProperty(PropertyName = "password")]
		public string Password { get; set; }
		[JsonProperty(PropertyName = "conversationid")]
		public string ConversationId { get; set; }

		[JsonProperty(PropertyName = "debug")]
		public bool Debug { get; set; }

	}

#if ENABLE_OFUSCATION
	[DoNotRenameAttribute]
#endif
	public class AskDeleteHistoryGPTHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string EventGenericAskDeleteHistoryGPTHTTPCompleted = "EventGenericAskDeleteHistoryGPTHTTPCompleted";

		private string _customEvent;

		public string UrlRequest
		{			            
			get { return GameAIData.Instance.ServerChatGPT + "question/delete?debug=true"; }
        }

		public string Build(params object[] _list)
		{
			string conversationID = (string)_list[0];
			_customEvent = (string)_list[1];

			_method = METHOD_POST;
			_formPost = null;
			_rawData = System.Text.Encoding.UTF8.GetBytes(
					JsonConvert.SerializeObject(new DeleteHistoryGPTRequest
					{
						UserID = GameAIData.Instance.ChatGPTID,
						Username = GameAIData.Instance.ChatGPTUsername,
						Password = GameAIData.Instance.ChatGPTPassword,
						ConversationId = conversationID,
						Debug = false
					}));

            return null;
        }

        public override void Response(string _response)
		{
			if (!ResponseCode(_response))
			{				
				if (_customEvent.Length > 0)
				{
					SystemEventController.Instance.DispatchSystemEvent(_customEvent, false);
				}
				else
                {
					SystemEventController.Instance.DispatchSystemEvent(EventGenericAskDeleteHistoryGPTHTTPCompleted, false);
				}
				return;
			}

			if (_customEvent.Length > 0)
			{
				if (_customEvent.Length > 0) SystemEventController.Instance.DispatchSystemEvent(_customEvent, true);
			}
			else
			{
				SystemEventController.Instance.DispatchSystemEvent(EventGenericAskDeleteHistoryGPTHTTPCompleted, true);
			}
		}
	}

}
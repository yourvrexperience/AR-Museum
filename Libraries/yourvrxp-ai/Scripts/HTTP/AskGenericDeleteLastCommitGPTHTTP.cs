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
	public class AskGenericDeleteLastCommitGPTHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string EventAskGenericDeleteLastCommitGPTHTTPCompleted = "EventAskGenericDeleteLastCommitGPTHTTPCompleted";

		private string _customEvent;

		public string UrlRequest
		{			            
			get { return GameAIData.Instance.ServerChatGPT + "question/delete_last?debug=true"; }
        }

        public string Build(params object[] _list)
		{
			string conversationID = (string)_list[0];
			_customEvent = (string)_list[1];

			_method = METHOD_POST;
			_formPost = null;
			_rawData = System.Text.Encoding.UTF8.GetBytes(
					JsonConvert.SerializeObject(new ChatGPTRequestJSON
					{
						UserID = GameAIData.Instance.ChatGPTID,
						Username = GameAIData.Instance.ChatGPTUsername,
						Password = GameAIData.Instance.ChatGPTPassword,
						ConversationId = conversationID,
						Instructions = "",
						Question = "",
						Chain = false,
						IsJSON = false,
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
					SystemEventController.Instance.DispatchSystemEvent(_customEvent, false);
				}
				else
                {
					SystemEventController.Instance.DispatchSystemEvent(EventAskGenericDeleteLastCommitGPTHTTPCompleted, false);
				}
				return;
			}

			// Get Response list
			if (_customEvent.Length > 0)
			{
				SystemEventController.Instance.DispatchSystemEvent(_customEvent, bool.Parse(_response) );
			}
			else
			{
				SystemEventController.Instance.DispatchSystemEvent(EventAskGenericDeleteLastCommitGPTHTTPCompleted, bool.Parse(_response));
			}
		}
	}
}
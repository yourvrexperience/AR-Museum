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
	public class DeleteConversationChatGPTHTTP : BaseDataHTTP, IHTTPComms
	{
		[System.Serializable]
		public class ConversationDeleteResponse
		{
			public bool success;
		}

		public const string EventDeleteConversationChatGPTHTTPCompleted = "EventDeleteConversationChatGPTHTTPCompleted";

		public string UrlRequest
		{						
            get { return GameAIData.Instance.ServerChatGPT + "conversations/delete"; }
        }

        public string Build(params object[] _list)
		{
			string conversationID = (string)_list[0];
            return "?debug=" + true 
					+ "&userid=" + GameAIData.Instance.ChatGPTID
					+ "&username=" + GameAIData.Instance.ChatGPTUsername
					+ "&password=" + GameAIData.Instance.ChatGPTPassword
					+ "&conversationid=" + conversationID;
        }

        public override void Response(string _response)
		{
			if (!ResponseCode(_response))
			{
				SystemEventController.Instance.DispatchSystemEvent(EventDeleteConversationChatGPTHTTPCompleted, false);
				return;
			}

			ConversationDeleteResponse deleteConfirmation = JsonUtility.FromJson<ConversationDeleteResponse>(_response);
			SystemEventController.Instance.DispatchSystemEvent(EventDeleteConversationChatGPTHTTPCompleted, deleteConfirmation);
		}
	}
}
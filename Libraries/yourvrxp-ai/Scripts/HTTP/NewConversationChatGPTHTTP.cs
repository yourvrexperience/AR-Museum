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
	public class NewConversationChatGPTHTTP : BaseDataHTTP, IHTTPComms
	{
		[System.Serializable]
		public class NewConversationConfirmation
		{
			public bool success;
			public string conversation_id;
		}

		public const string EventNewConversationChatGPTHTTPCompleted = "EventNewConversationChatGPTHTTPCompleted";

		public string UrlRequest
		{			
            get { return GameAIData.Instance.ServerChatGPT + "conversations/new"; }
        }

        public string Build(params object[] _list)
		{
            return "?debug=" + true 
					+ "&userid=" + GameAIData.Instance.ChatGPTID
					+ "&username=" + GameAIData.Instance.ChatGPTUsername
					+ "&password=" + GameAIData.Instance.ChatGPTPassword
					+ "&namescript=" + (string)_list[0];
        }

        public override void Response(string _response)
		{
			if (!ResponseCode(_response))
			{
				SystemEventController.Instance.DispatchSystemEvent(EventNewConversationChatGPTHTTPCompleted, false);
				return;
			}
			
			NewConversationConfirmation newConversation = JsonUtility.FromJson<NewConversationConfirmation>(_response);
			SystemEventController.Instance.DispatchSystemEvent(EventNewConversationChatGPTHTTPCompleted, true, newConversation);
		}
	}
}
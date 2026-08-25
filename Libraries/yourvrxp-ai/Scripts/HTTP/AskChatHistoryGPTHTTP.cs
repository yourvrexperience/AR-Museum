using System;
using System.Collections.Generic;
using System.Linq;
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
	public class ChatHistoryGPTRequest
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
	
	[System.Serializable]
	public class ListChatMessages
	{
		public ChatMessage[] Messages;
	}

	[System.Serializable]
	public class ChatMessage
	{
		public int Mode;
		public string Text;

		public ChatMessage(int mode, string text)
		{
			Mode = mode;
			Text = text;
		}
	}

#if ENABLE_OFUSCATION
	[DoNotRenameAttribute]
#endif
	public class AskChatHistoryGPTHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string EventGenericAskChatHistoryGPTHTTPCompleted = "EventGenericAskChatHistoryGPTHTTPCompleted";

		private string _customEvent;

		public string UrlRequest
		{			            
			get { return GameAIData.Instance.ServerChatGPT + "question/history?debug=true"; }
        }

		public List<ChatMessage> ProcessJson(string jsonString)
		{
			string finalList = "{ \"Messages\":" + jsonString + "}";
			ListChatMessages messages = JsonUtility.FromJson<ListChatMessages>(finalList);
			return messages.Messages.ToList();
		}

		public string Build(params object[] _list)
		{
			string conversationID = (string)_list[0];
			_customEvent = (string)_list[1];

			_method = METHOD_POST;
			_formPost = null;
			_rawData = System.Text.Encoding.UTF8.GetBytes(
					JsonConvert.SerializeObject(new ChatHistoryGPTRequest
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
					SystemEventController.Instance.DispatchSystemEvent(EventGenericAskChatHistoryGPTHTTPCompleted, false);
				}
				return;
			}

			// Get Response list
			if (_response.Length == 0)
            {
				if (_customEvent.Length > 0)
				{
					if (_customEvent.Length > 0) SystemEventController.Instance.DispatchSystemEvent(_customEvent, true);
				}
				else
				{
					SystemEventController.Instance.DispatchSystemEvent(EventGenericAskChatHistoryGPTHTTPCompleted, true);
				}
			}
			else
            {
                try
                {
					List<ChatMessage> chatMessages = ProcessJson(_response);
					if (_customEvent.Length > 0)
					{
						SystemEventController.Instance.DispatchSystemEvent(_customEvent, true, chatMessages);
					}
					else
					{
						SystemEventController.Instance.DispatchSystemEvent(EventGenericAskChatHistoryGPTHTTPCompleted, true, chatMessages);
					}
				} catch (Exception err)
                {
					Debug.LogError(err.Message);
                }
			}
		}
	}

}
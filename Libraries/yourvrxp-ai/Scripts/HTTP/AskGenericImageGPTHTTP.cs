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
	[System.Serializable]
	public class ImageGPTRequest
	{
		[JsonProperty(PropertyName = "userid")]
		public int UserID { get; set; }

		[JsonProperty(PropertyName = "username")]
		public string Username { get; set; }

		[JsonProperty(PropertyName = "password")]
		public string Password { get; set; }

		[JsonProperty(PropertyName = "description")]
		public string Description { get; set; }
		[JsonProperty(PropertyName = "provider")]
		public int Provider { get; set; }

		[JsonProperty(PropertyName = "exclude")]
		public string Exclude { get; set; }

		[JsonProperty(PropertyName = "steps")]
		public int Steps { get; set; }

		[JsonProperty(PropertyName = "width")]
		public int Width { get; set; }

		[JsonProperty(PropertyName = "height")]
		public int Height { get; set; }
		[JsonProperty(PropertyName = "data")]
		public string Data { get; set; }
	}

#if ENABLE_OFUSCATION
	[DoNotRenameAttribute]
#endif
	public class AskGenericImageGPTHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string IMAGE = "IMAGE";

		public const string EventGenericAskImageGPTHTTPCompleted = "EventGenericAskImageGPTHTTPCompleted";

		private string _customEvent;

		public string UrlRequest
		{			            
			get { return GameAIData.Instance.ServerChatGPT + "image?debug=true"; }
        }

        public string Build(params object[] _list)
		{
			int provider = (int)_list[0];
			string description = (string)_list[1];
			string exclude = (string)_list[2];
			int steps = (int)_list[3];
			int width = (int)_list[4];
			int height = (int)_list[5];
			_customEvent = (string)_list[6];

			_method = METHOD_POST;
			_formPost = null;
			_rawData = System.Text.Encoding.UTF8.GetBytes(
					JsonConvert.SerializeObject(new ImageGPTRequest
					{
						UserID = GameAIData.Instance.ChatGPTID,
						Username = GameAIData.Instance.ChatGPTUsername,
						Password = GameAIData.Instance.ChatGPTPassword,
						Provider = provider,
						Description = description,
						Exclude = exclude,
						Steps = steps,
						Width = width,
						Height = height,
						Data = ""
					}));

			SystemEventController.Instance.DispatchSystemEvent(GameAIData.EventGameAIDataAIStartRequest, IMAGE, description);

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
					SystemEventController.Instance.DispatchSystemEvent(EventGenericAskImageGPTHTTPCompleted, false);
				}
			}
		}

		public override void Response(byte[] _response)
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
					SystemEventController.Instance.DispatchSystemEvent(EventGenericAskImageGPTHTTPCompleted, false);
				}
				return;
			}

			// Get Response list
			SystemEventController.Instance.DelaySystemEvent(GameAIData.EventGameAIDataCostAIRequest, 0.2f, IMAGE);			
			SystemEventController.Instance.DispatchSystemEvent(GameAIData.EventGameAIDataAIEndRequest);
			if (_customEvent.Length > 0)
			{
				SystemEventController.Instance.DispatchSystemEvent(_customEvent, true, _response);
			}
			else
			{
				SystemEventController.Instance.DispatchSystemEvent(EventGenericAskImageGPTHTTPCompleted, true, _response);
			}
		}
	}
}
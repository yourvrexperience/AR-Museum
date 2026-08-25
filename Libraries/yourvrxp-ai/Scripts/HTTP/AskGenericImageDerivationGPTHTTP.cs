using System;
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
	public class AskGenericImageDerivationGPTHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string IMAGE = "IMAGE";

		public const string EventGenericAskImageDerivationGPTHTTPCompleted = "EventGenericAskImageDerivationGPTHTTPCompleted";

		private string _customEvent;

		public string UrlRequest
		{
			get { return GameAIData.Instance.ServerChatGPT + "image/derivation?debug=true"; }
		}

        public string Build(params object[] _list)
		{
			int provider = (int)_list[0];
			string description = (string)_list[1];
			string exclude = (string)_list[2];
			int steps = (int)_list[3];
			int width = (int)_list[4];
			int height = (int)_list[5];
			_customEvent = (string)_list[7];

			byte[] imageData = (byte[])_list[6];
			string image64 = Convert.ToBase64String(imageData);

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
						Data = image64
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
					SystemEventController.Instance.DispatchSystemEvent(EventGenericAskImageDerivationGPTHTTPCompleted, false);
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
					SystemEventController.Instance.DispatchSystemEvent(EventGenericAskImageDerivationGPTHTTPCompleted, false);
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
				SystemEventController.Instance.DispatchSystemEvent(EventGenericAskImageDerivationGPTHTTPCompleted, true, _response);
			}
		}
	}
}
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
#if ENABLE_OFUSCATION
	[DoNotRenameAttribute]
#endif
	public class AskGenericSoundMusicGPTHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string EventGenericAskSoundMusicGPTHTTPCompleted = "EventGenericAskSoundMusicGPTHTTPCompleted";

		private string _customEvent;
		private string _speech;

		public string UrlRequest
		{			            
			get { return GameAIData.Instance.ServerChatGPT + "music?debug=true"; }
        }

        public string Build(params object[] _list)
		{
			string description = (string)_list[0];
			int duration = (int)_list[1];
			_customEvent = (string)_list[2];

			_method = METHOD_POST;
			_formPost = null;
			_rawData = System.Text.Encoding.UTF8.GetBytes(
					JsonConvert.SerializeObject(new SoundGPTRequest
					{
						UserID = GameAIData.Instance.ChatGPTID,
						Username = GameAIData.Instance.ChatGPTUsername,
						Password = GameAIData.Instance.ChatGPTPassword,
						Description = description,
						Duration = duration
					}));

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
					SystemEventController.Instance.DispatchSystemEvent(EventGenericAskSoundMusicGPTHTTPCompleted, false);
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
					SystemEventController.Instance.DispatchSystemEvent(EventGenericAskSoundMusicGPTHTTPCompleted, false);
				}
				return;
			}

			// Get Response list
			if (_customEvent.Length > 0)
			{
				SystemEventController.Instance.DispatchSystemEvent(_customEvent, true, _response);
			}
			else
			{
				SystemEventController.Instance.DispatchSystemEvent(EventGenericAskSoundMusicGPTHTTPCompleted, true, _response);
			}
		}
	}
}
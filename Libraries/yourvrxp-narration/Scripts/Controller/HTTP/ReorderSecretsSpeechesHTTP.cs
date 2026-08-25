using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Security.Cryptography;
using yourvrexperience.Utils;

namespace yourvrexperience.Narration
{
	public class ReorderSecretsSpeechesHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string EventReorderSecretsSpeechesHTTPCompleted = "EventReorderSecretsSpeechesHTTPCompleted";

        private string m_urlRequest = "";
		private string _customEvent = "";

        public string UrlRequest
        {
            get
            {
                if (m_urlRequest.Length == 0)
                {
                    m_urlRequest = GameLevelData.Instance.URLBaseManagement + "MuseumReorderSecretSpeeches.php";
                }
                return m_urlRequest;
            }
        }

        public string Build(params object[] _list)
		{
			_method = METHOD_POST;
			
			_customEvent = (string)_list[0];

			_formPost = new WWWForm();
			_formPost.AddField("secret", (int)_list[1]);
			_formPost.AddField("age", (int)_list[2]);
			_formPost.AddField("floor", (int)_list[3]);
			_formPost.AddField("operation", ((bool)_list[4]?1:0));

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
					SystemEventController.Instance.DispatchSystemEvent(EventReorderSecretsSpeechesHTTPCompleted, false);
				}
				return;
			}

			string[] data = _jsonResponse.Split(new string[] { CommController.TOKEN_SEPARATOR_EVENTS }, StringSplitOptions.None);
            if (bool.Parse(data[0]))
			{
				if (_customEvent.Length > 0)
				{
					SystemEventController.Instance.DispatchSystemEvent(_customEvent, true);
				}
				else
				{
					SystemEventController.Instance.DispatchSystemEvent(EventReorderSecretsSpeechesHTTPCompleted, true);
				}
                
			}
			else
			{
				if (_customEvent.Length > 0)
				{
					SystemEventController.Instance.DispatchSystemEvent(_customEvent, false);
				}
				else
				{
					SystemEventController.Instance.DispatchSystemEvent(EventReorderSecretsSpeechesHTTPCompleted, false);
				}
			}
		}
	}

}
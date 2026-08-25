using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Security.Cryptography;
using yourvrexperience.Utils;

namespace yourvrexperience.Narration
{	
	public class SetVersionHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string EventSetVersionHTTPCompleted = "EventSetVersionHTTPCompleted";

        private string m_urlRequest = "";

        public string UrlRequest
        {
            get
            {
                if (m_urlRequest.Length == 0)
                {
                    m_urlRequest = GameLevelData.Instance.URLBaseManagement + "MuseumTourSetVersion.php";
                }
                return m_urlRequest;
            }
        }

		public string Build(params object[] _list)
		{
            string callParams = "?iduser=" + (int)_list[0]  + "&passworduser=" + (string)_list[1] + "&version=" + (int)_list[2] + "&secrets=" + (int)_list[3];
            return callParams;
        }

		public override void Response(string _response)
		{
			if (!ResponseCode(_response))
			{
				SystemEventController.Instance.DispatchSystemEvent(EventSetVersionHTTPCompleted, false);
				return;
			}

			string[] data = _jsonResponse.Split(new string[] { CommController.TOKEN_SEPARATOR_EVENTS }, StringSplitOptions.None);
            if (bool.Parse(data[0]))
			{
                SystemEventController.Instance.DispatchSystemEvent(EventSetVersionHTTPCompleted, true);
			}
			else
			{
                SystemEventController.Instance.DispatchSystemEvent(EventSetVersionHTTPCompleted, false);
			}
		}
	}

}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Security.Cryptography;
using yourvrexperience.Utils;

namespace yourvrexperience.Narration
{
	public class ConsultVersionHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string EventConsultVersionHTTPCompleted = "EventConsultVersionHTTPCompleted";

        private string m_urlRequest = "";

        public string UrlRequest
        {
            get
            {				
                if (m_urlRequest.Length == 0)
                {
                    m_urlRequest = GameLevelData.Instance.URLBaseManagement + "MuseumTourGetVersion.php";
                }
                return m_urlRequest;
            }
        }

		public string Build(params object[] _list)
		{
            return "";
        }

		public override void Response(string _response)
		{
			if (!ResponseCode(_response))
			{
				SystemEventController.Instance.DispatchSystemEvent(EventConsultVersionHTTPCompleted, false);
				return;
			}

			string[] versionData = _jsonResponse.Split(new string[] { CommController.TOKEN_SEPARATOR_EVENTS }, StringSplitOptions.None);
            if (!bool.Parse(versionData[0]))
            {
                SystemEventController.Instance.DispatchSystemEvent(EventConsultVersionHTTPCompleted, false);
            }
            else
            {
				SystemEventController.Instance.DispatchSystemEvent(EventConsultVersionHTTPCompleted, true, int.Parse(versionData[1]), int.Parse(versionData[2]), int.Parse(versionData[3]), int.Parse(versionData[4]), int.Parse(versionData[5]), versionData[6], versionData[7]);
            }
        }
	}
}
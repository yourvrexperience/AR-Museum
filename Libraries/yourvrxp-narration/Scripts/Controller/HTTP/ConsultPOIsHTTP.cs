using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Security.Cryptography;
using yourvrexperience.Utils;

namespace yourvrexperience.Narration
{
	public class ConsultPOIsHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string EventConsultPOIsHTTPCompleted = "EventConsultPOIsHTTPCompleted";

        private string m_urlRequest = "";

        public string UrlRequest
        {
            get
            {				
                if (m_urlRequest.Length == 0)
                {
                    m_urlRequest = GameLevelData.Instance.URLBaseManagement + "MuseumTourConsult.php";
                }
                return m_urlRequest;
            }
        }

		public string Build(params object[] _list)
		{
            string callParams = "?id=" + (int)_list[0] + "&age=" + (int)_list[1] + "&dev=" + ((bool)_list[2]?1:0);
            return callParams;
        }

		public override void Response(byte[] _response)
		{
			if (!ResponseUTF8Code(_response))
			{
				SystemEventController.Instance.DispatchSystemEvent(EventConsultPOIsHTTPCompleted, false);
				return;
			}

			string[] poisData = _jsonResponse.Split(new String[] { CommController.TOKEN_SEPARATOR_EVENTS }, StringSplitOptions.None);
            if (!bool.Parse(poisData[0]))
            {
                SystemEventController.Instance.DispatchSystemEvent(EventConsultPOIsHTTPCompleted, false);
            }
            else
            {
                string dataPosition = poisData[1].Replace("\\","");
                string secretsPosition = poisData[2].Replace("\\","");
                string dataNarration = poisData[3].Replace("\\","");
				SystemEventController.Instance.DispatchSystemEvent(EventConsultPOIsHTTPCompleted, true, dataPosition, secretsPosition, dataNarration);
            }
        }
	}

}
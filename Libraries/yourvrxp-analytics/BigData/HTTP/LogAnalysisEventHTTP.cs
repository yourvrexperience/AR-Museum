using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Security.Cryptography;
using yourvrexperience.Utils;

namespace yourvrexperience.Analytics
{
	public class LogAnalysisEventHTTP : BaseDataHTTP, IHTTPComms
	{
		private string _nameEvent;

        private string m_urlRequest = "";

        public string UrlRequest
        {
            get
            {
                if (m_urlRequest.Length == 0)
                {
                    m_urlRequest = CommsHTTPAnalysis.Instance.GetBaseURL() + "AnalysisLogEvent.php";
                }
                return m_urlRequest;
            }
        }

        public string Build(params object[] _list)
		{
			_method = METHOD_POST;

            _nameEvent = (string)_list[0];

			_formPost = new WWWForm();
			_formPost.AddField("nameevent", _nameEvent);
			_formPost.AddField("email", (string)_list[1]);
			_formPost.AddField("age", (int)_list[2]);
			_formPost.AddField("language", (string)_list[3]);
			_formPost.AddField("level", (int)_list[4]);
            _formPost.AddField("data", (string)_list[5]);

            return null;
		}

		public override void Response(string _response)
		{
			if (!ResponseCode(_response))
			{
				CommsHTTPAnalysis.Instance.DisplayLog(_jsonResponse);
				return;
			}

			string[] data = _jsonResponse.Split(new string[] { CommController.TOKEN_SEPARATOR_USER_DATA }, StringSplitOptions.None);
            if (bool.Parse(data[0]))
			{
                CommsHTTPAnalysis.Instance.DisplayLog("Event ["+_nameEvent+"] logged successfully");
			}
			else
			{
                CommsHTTPAnalysis.Instance.DisplayLog("Event ["+_nameEvent+"] failed to be logged");
			}
		}
	}

}
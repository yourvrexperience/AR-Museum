using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Security.Cryptography;
using yourvrexperience.Utils;

namespace yourvrexperience.Narration
{
	public class DeleteSpeechHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string EventDeleteSpeechHTTPCompleted = "EventDeleteSpeechHTTPCompleted";

        private string m_urlRequest = "";
		private string _customEvent = "";

		private bool _all;
		private int _secret;
		private int _age;
		private int _floor;
		private int _poi;
		private int _segment;

        public string UrlRequest
        {
            get
            {
                if (m_urlRequest.Length == 0)
                {
                    m_urlRequest = GameLevelData.Instance.URLBaseManagement + "MuseumDeleteSpeech.php";
                }
                return m_urlRequest;
            }
        }

        public string Build(params object[] _list)
		{
			_method = METHOD_POST;
			
			int idUser = (int)_list[0];
			string passwordUser = (string)_list[1];

			_customEvent = (string)_list[2];

			_all = (bool)_list[3];
			_secret = (int)_list[4];
			_age = (int)_list[5];
			_floor = (int)_list[6];
			_poi = (int)_list[7];
			_segment = (int)_list[8];

			_formPost = new WWWForm();
			_formPost.AddField("iduser", idUser);
			_formPost.AddField("passworduser", passwordUser);

			_formPost.AddField("all", (_all?1:0));
			_formPost.AddField("secret", _secret);
			_formPost.AddField("age", _age);
			_formPost.AddField("floor", _floor);
			_formPost.AddField("poi", _poi);
            _formPost.AddField("segment", _segment);

            return null;
		}

		public override void Response(string _response)
		{
			if (!ResponseCode(_response))
			{
				if (_customEvent.Length > 0)
				{
					SystemEventController.Instance.DispatchSystemEvent(_customEvent, false, _all, _secret, _age, _floor, _poi, _segment);
				}
				else
				{
					SystemEventController.Instance.DispatchSystemEvent(EventDeleteSpeechHTTPCompleted, false, _all, _secret, _age, _floor, _poi, _segment);
				}
				return;
			}

			string[] data = _jsonResponse.Split(new string[] { CommController.TOKEN_SEPARATOR_EVENTS }, StringSplitOptions.None);
            if (bool.Parse(data[0]))
			{
				if (_customEvent.Length > 0)
				{
					SystemEventController.Instance.DispatchSystemEvent(_customEvent, true, _all, _secret, _age, _floor, _poi, _segment);
				}
				else
				{
					SystemEventController.Instance.DispatchSystemEvent(EventDeleteSpeechHTTPCompleted, true, _all, _secret, _age, _floor, _poi, _segment);
				}                
			}
			else
			{
				if (_customEvent.Length > 0)
				{
					SystemEventController.Instance.DispatchSystemEvent(_customEvent, false, _all, _secret, _age, _floor, _poi, _segment);
				}
				else
				{
					SystemEventController.Instance.DispatchSystemEvent(EventDeleteSpeechHTTPCompleted, false, _all, _secret, _age, _floor, _poi, _segment);
				}
			}
		}
	}

}
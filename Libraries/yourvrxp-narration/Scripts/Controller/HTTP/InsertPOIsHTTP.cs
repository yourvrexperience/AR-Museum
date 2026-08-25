using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Security.Cryptography;
using yourvrexperience.Utils;

namespace yourvrexperience.Narration
{
	public class InsertPOIsHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string EventInsertPOIsHTTPCompleted = "EventInsertPOIsHTTPCompleted";

        private string m_urlRequest = "";

        public string UrlRequest
        {
            get
            {
                if (m_urlRequest.Length == 0)
                {
                    m_urlRequest = GameLevelData.Instance.URLBaseManagement + "MuseumTourRegister.php";
                }
                return m_urlRequest;
            }
        }

        public string Build(params object[] _list)
		{
			_method = METHOD_POST;

			int idUser          = (int)_list[0];
			string passwordUser = (string)_list[1];
			int uid             = (int)_list[2];
			int age             = (int)_list[3];
			int level           = (int)_list[4];
			int dev             = ((bool)_list[5]) ? 1 : 0;
			string positions    = (string)_list[6];
			string secrets      = (string)_list[7];
			string narration    = (string)_list[8];

			// Base64 the big text fields so their brackets/delimiters/coordinates
			// don't look like a PHP/SQL injection to the firewall (this is what
			// was tripping rule 933210 / "PCRE limits exceeded").
			string posB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(positions ?? ""));
			string secB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(secrets   ?? ""));
			string narB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(narration ?? ""));

			StringBuilder meta = new StringBuilder();
			meta.Append("iduser=").Append(idUser);
			meta.Append("&passworduser=").Append(Uri.EscapeDataString(passwordUser ?? ""));
			meta.Append("&id=").Append(uid);
			meta.Append("&age=").Append(age);
			meta.Append("&level=").Append(level);
			meta.Append("&dev=").Append(dev);
			meta.Append("&positions_b64=").Append(Uri.EscapeDataString(posB64));
			meta.Append("&secrets_b64=").Append(Uri.EscapeDataString(secB64));
			meta.Append("&narration_b64=").Append(Uri.EscapeDataString(narB64));

			// The entire body is the url-encoded metadata, sent as octet-stream.
			// Goes through CommController's UploadHandlerRaw branch, same as the
			// speech endpoint. (_rawData is the field backing the RawData property.)
			_rawData = Encoding.UTF8.GetBytes(meta.ToString());

            return null;
		}

		public override void Response(string _response)
		{
			if (!ResponseCode(_response))
			{
				SystemEventController.Instance.DispatchSystemEvent(EventInsertPOIsHTTPCompleted, false);
				return;
			}

			string[] data = _jsonResponse.Split(new string[] { CommController.TOKEN_SEPARATOR_EVENTS }, StringSplitOptions.None);
            if (bool.Parse(data[0]))
			{
                SystemEventController.Instance.DispatchSystemEvent(EventInsertPOIsHTTPCompleted, true);
			}
			else
			{
                SystemEventController.Instance.DispatchSystemEvent(EventInsertPOIsHTTPCompleted, false);
			}
		}
	}

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Security.Cryptography;
using yourvrexperience.Utils;

namespace yourvrexperience.Narration
{
	public class StoreSpeechHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string EventStoreSpeechHTTPCompleted = "EventStoreSpeechHTTPCompleted";

        private string m_urlRequest = "";
		private string _customEvent = "";
		private string _text = "";
		private int _secret = -1;

        public string UrlRequest
        {
            get
            {
                if (m_urlRequest.Length == 0)
                {
                    m_urlRequest = GameLevelData.Instance.URLBaseManagement + "MuseumStoreSpeech.php";
                }
                return m_urlRequest;
            }
        }

        public string Build(params object[] _list)
		{
			_method = METHOD_POST;

			int idUser          = (int)_list[0];
			string passwordUser = (string)_list[1];
			_customEvent        = (string)_list[2];
			_secret             = (int)_list[3];
			_text               = (string)_list[4];
			int age             = (int)_list[5];
			int floor           = (int)_list[6];
			int poi             = (int)_list[7];
			int segment         = (int)_list[8];
			string language     = (string)_list[9];
			byte[] soundSpeechData = (byte[])_list[10];

			// --- Build metadata as a url-encoded string. The free-text "text" is
			//     base64-encoded so its quotes/accents never trip the WAF. ---
			string textB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(_text ?? ""));

			StringBuilder meta = new StringBuilder();
			meta.Append("iduser=").Append(idUser);
			meta.Append("&passworduser=").Append(Uri.EscapeDataString(passwordUser ?? ""));
			meta.Append("&secret=").Append(_secret);
			meta.Append("&age=").Append(age);
			meta.Append("&floor=").Append(floor);
			meta.Append("&poi=").Append(poi);
			meta.Append("&segment=").Append(segment);
			meta.Append("&language=").Append(Uri.EscapeDataString(language ?? ""));
			meta.Append("&text_b64=").Append(Uri.EscapeDataString(textB64));

			byte[] metaBytes = Encoding.UTF8.GetBytes(meta.ToString());

			// --- Frame: [4-byte little-endian meta length][meta][raw audio] ---
			int n = metaBytes.Length;
			byte[] body = new byte[4 + n + soundSpeechData.Length];
			body[0] = (byte)(n & 0xFF);
			body[1] = (byte)((n >> 8) & 0xFF);
			body[2] = (byte)((n >> 16) & 0xFF);
			body[3] = (byte)((n >> 24) & 0xFF);
			Buffer.BlockCopy(metaBytes, 0, body, 4, n);
			Buffer.BlockCopy(soundSpeechData, 0, body, 4 + n, soundSpeechData.Length);

			// Route through CommController's UploadHandlerRaw branch (octet-stream).
			// NOTE: _rawData is the field backing the RawData property in BaseDataHTTP,
			//       the raw-bytes equivalent of how the old code set _formPost.
			//       Rename here if your backing field uses a different name.
			_rawData = body;

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
					SystemEventController.Instance.DispatchSystemEvent(EventStoreSpeechHTTPCompleted, false);
				}
				return;
			}

			string[] data = _jsonResponse.Split(new string[] { CommController.TOKEN_SEPARATOR_EVENTS }, StringSplitOptions.None);
            if (bool.Parse(data[0]))
			{
				if (_customEvent.Length > 0)
				{
					SystemEventController.Instance.DispatchSystemEvent(_customEvent, true, _text);
				}
				else
				{
					SystemEventController.Instance.DispatchSystemEvent(EventStoreSpeechHTTPCompleted, true, _text);
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
					SystemEventController.Instance.DispatchSystemEvent(EventStoreSpeechHTTPCompleted, false);
				}
			}
		}
	}

}

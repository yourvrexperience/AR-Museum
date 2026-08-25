using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace yourvrexperience.Utils
{
	public class BaseDataHTTP
	{
		public const int METHOD_GET = 0;
		public const int METHOD_POST = 1;
		public const int METHOD_CANCEL = 2;

		protected string _code;
		protected string _jsonResponse;
		protected int _method = METHOD_GET;
		protected WWWForm _formPost;
		protected List<IMultipartFormSection> _formData;
		protected byte[] _rawData;

		protected bool _cancelResponse = false;

		public int Method
		{
			get { return _method; }
		}

		public byte[] RawData 
		{ 
			get { return _rawData; } 
		}

		public WWWForm FormPost
		{
			get { return _formPost; }
		}
		public List<IMultipartFormSection> FormData
		{
			get { return _formData; }
		}

		public virtual Dictionary<string, string> GetHeaders()
		{
			Dictionary<string, string> headers = new Dictionary<string, string>();
			return headers;
		}
		

		private string CleanUndesiredTags(string data)
		{
			string output = data;
			if ((output.IndexOf("<br>") != -1) ||
				(output.IndexOf("<br/>") != -1) ||
				(output.IndexOf("<br />") != -1))
			{
				output = output.Replace("<br>", "");
				output = output.Replace("<br/>", "");
				output = output.Replace("<br />", "");
			}
			return output;
		}

		public bool ResponseBase64Code(string response)
		{
			_jsonResponse = response;

			if (CommController.DEBUG_LOG)
			{
				Debug.Log("BaseDataJSON::ResponseBase64Code(BYTE)=" + _jsonResponse);
			}
			return (_jsonResponse.IndexOf("Error::") == -1);
		}

		public bool ResponseCode(byte[] response)
		{
			_jsonResponse = Encoding.ASCII.GetString(response);
			_jsonResponse = CleanUndesiredTags(_jsonResponse);
			if (CommController.DEBUG_LOG)
			{
				Debug.Log("BaseDataJSON::ResponseCode(BYTE)=" + _jsonResponse);
			}
			return (_jsonResponse.IndexOf("Error::") == -1);
		}

		public bool ResponseUTF8Code(byte[] response)
		{
			if (response != null)
			{
				_jsonResponse = Encoding.UTF8.GetString(response);
				_jsonResponse = CleanUndesiredTags(_jsonResponse);
			}
			else
			{
				_jsonResponse = "Error::";
			}
			if (CommController.DEBUG_LOG)
			{
				Debug.Log("BaseDataJSON::ResponseCode(BYTE)=" + _jsonResponse);
			}
			return (_jsonResponse.IndexOf("Error::") == -1);
		}

		public bool ResponseCode(string response)
		{
			_jsonResponse = response;
			_jsonResponse = CleanUndesiredTags(_jsonResponse);
			if (CommController.DEBUG_LOG)
			{
				Debug.Log("BaseDataJSON::ResponseCode(STRING)=" + _jsonResponse);
			}
			return (_jsonResponse.IndexOf("Error::") == -1);
		}

		public virtual void Response(byte[] _response)
		{
		}

		public virtual void Response(string _response)
		{
		}

		public virtual void CancelResponse()
		{
			_cancelResponse = true;
		}
	}
}
#if ENABLE_OFUSCATION
#if ENABLE_NEW_OFUSCATION
using GUPS.Obfuscator.Attribute;
#else
using OPS.Obfuscator.Attribute;
#endif
#endif
using System;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.UserManagement
{

#if ENABLE_OFUSCATION
	[DoNotRenameAttribute]
#endif
	public class GetConfigurationServerParametersHTTP : BaseDataHTTP, IHTTPComms
	{
		private string m_urlRequest = "";

		public string UrlRequest
		{
			get
			{
				if (m_urlRequest.Length == 0)
				{
					m_urlRequest = CommsHTTPConstants.Instance.GetBaseURL() + "GetConfigurationServerParameters.php";
				}
				return m_urlRequest;
			}
		}

		public string Build(params object[] _list)
		{
			return "";
		}

		public override void Response(byte[] _response)
		{
			if (!ResponseCode(_response))
			{
				CommsHTTPConstants.Instance.DisplayLog(_jsonResponse);
				return;
			}

			string[] data = _jsonResponse.Split(new string[] { CommController.TOKEN_SEPARATOR_EVENTS }, StringSplitOptions.None);
			if (bool.Parse(data[0]))
			{
                UsersController.Instance.EmailCustomerService = data[1];
                UsersController.Instance.IsServiceEnabled = bool.Parse(data[2]);

				Debug.Log("EMAIL CUSTOMER SERVICE=" + UsersController.Instance.EmailCustomerService);
				Debug.Log("IS SERVICE ACTIVE=" + UsersController.Instance.IsServiceEnabled);

                SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_CONFIGURATION_DATA_RECEIVED, UsersController.Instance.IsServiceEnabled);
			}
		}
	}
}
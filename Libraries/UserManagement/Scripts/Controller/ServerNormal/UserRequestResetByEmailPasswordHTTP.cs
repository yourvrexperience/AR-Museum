using System;
using yourvrexperience.Utils;
#if ENABLE_OFUSCATION
#if ENABLE_NEW_OFUSCATION
using GUPS.Obfuscator.Attribute;
#else
using OPS.Obfuscator.Attribute;
#endif
#endif

namespace yourvrexperience.UserManagement
{
#if ENABLE_OFUSCATION
	[DoNotRenameAttribute]
#endif
	public class UserRequestResetByEmailPasswordHTTP : BaseDataHTTP, IHTTPComms
	{
        private string m_urlRequest = "";

        public string UrlRequest
        {
            get
            {
                if (m_urlRequest.Length == 0)
                {
                    m_urlRequest = CommsHTTPConstants.Instance.GetBaseURL() + "UserRequestResetByEmailPassword.php";
                }
                return m_urlRequest;
            }
        }

        public string Build(params object[] _list)
		{
			return "?language=" + LanguageController.Instance.CodeLanguage + "&email=" + (string)_list[0];
		}

		public override void Response(byte[] _response)
		{
			if (!ResponseCode(_response))
			{
				CommsHTTPConstants.Instance.DisplayLog(_jsonResponse);
                SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_RESPONSE_RESET_PASSWORD, false);
				return;
			}

			string[] data = _jsonResponse.Split(new string[] { CommController.TOKEN_SEPARATOR_EVENTS }, StringSplitOptions.None);
			if (bool.Parse(data[0]))
			{
                SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_RESPONSE_RESET_PASSWORD, true);
			}
			else
			{
                SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_RESPONSE_RESET_PASSWORD, false);
			}
		}
	}

}
#if ENABLE_OFUSCATION
#if ENABLE_NEW_OFUSCATION
using GUPS.Obfuscator.Attribute;
#else
using OPS.Obfuscator.Attribute;
#endif
#endif
using System;
using yourvrexperience.Utils;

namespace yourvrexperience.UserManagement
{
#if ENABLE_OFUSCATION
	[DoNotRenameAttribute]
#endif
	public class UserRegisterWithEmailHTTP : BaseDataHTTP, IHTTPComms
	{
        private string m_urlRequest = "";

        public string UrlRequest
        {
            get
            {
                if (m_urlRequest.Length == 0)
                {
                    m_urlRequest = CommsHTTPConstants.Instance.GetBaseURL() + "UserRegisterByEmail.php";
                }
                return m_urlRequest;
            }
        }

        public string Build(params object[] _list)
		{
			return "?language=" + LanguageController.Instance.CodeLanguage + "&email=" + (string)_list[0] + "&password=" + (string)_list[1] + "&platform=" + (string)_list[2];
		}

		public override void Response(string _response)
		{
			if (!ResponseCode(_response))
			{
				CommsHTTPConstants.Instance.DisplayLog(_jsonResponse);
				SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_REGISTER_RESULT, false);
				return;
			}

			string[] data = _jsonResponse.Split(new string[] { CommController.TOKEN_SEPARATOR_USER_DATA }, StringSplitOptions.None);
			if (bool.Parse(data[0]))
			{
                SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_REGISTER_RESULT, true, data);
			}
			else
			{
                SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_REGISTER_RESULT, false);
			}
		}
	}
}
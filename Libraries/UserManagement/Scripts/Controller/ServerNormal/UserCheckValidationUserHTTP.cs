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
	public class UserCheckValidationUserHTTP : BaseDataHTTP, IHTTPComms
	{
        private string m_urlRequest = "";

        public string UrlRequest
        {
            get
            {
                if (m_urlRequest.Length == 0)
                {
                    m_urlRequest = CommsHTTPConstants.Instance.GetBaseURL() + "UserCheckValidation.php";
                }
                return m_urlRequest;
            }
        }

        public string Build(params object[] _list)
		{
			return "?id=" + (string)_list[0];
		}

		public override void Response(string _response)
		{
			if (!ResponseCode(_response))
			{
				CommsHTTPConstants.Instance.DisplayLog(_jsonResponse);
				SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_CHECK_VALIDATION_RESULT, false);
				return;
			}

			string[] data = _jsonResponse.Split(new string[] { CommController.TOKEN_SEPARATOR_EVENTS }, StringSplitOptions.None);
			if (bool.Parse(data[0]))
			{
				SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_CHECK_VALIDATION_RESULT, true);
			}
			else
			{
				SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_CHECK_VALIDATION_RESULT, false);
			}
		}
	}

}
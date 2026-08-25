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
    public class UserConsultByIdHTTP : BaseDataHTTP, IHTTPComms
	{
        private string m_urlRequest = "";

        public string UrlRequest
        {
            get
            {
                if (m_urlRequest.Length == 0)
                {
                    m_urlRequest = CommsHTTPConstants.Instance.GetBaseURL() + "UserConsult.php";
                }
                return m_urlRequest;
            }
        }

        public string Build(params object[] _list)
		{
			return "?id=" + (string)_list[0] + "&password=" + (string)_list[1] + "&user=" + (string)_list[2];
		}

		public override void Response(string _response)
		{
			if (!ResponseCode(_response))
			{
				CommsHTTPConstants.Instance.DisplayLog(_jsonResponse);
				UIEventController.Instance.DispatchUIEvent(UsersController.EVENT_USER_RESULT_CONSULT_SINGLE_RECORD, false);
				return;
			}

            string[] data = _jsonResponse.Split(new string[] { CommController.TOKEN_SEPARATOR_USER_DATA }, StringSplitOptions.None);
            if (bool.Parse(data[0]))
            {
                SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_RESULT_CONSULT_SINGLE_RECORD, true, data);
            }
            else
            {
                SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_RESULT_CONSULT_SINGLE_RECORD, false);
            }
		}
	}
}
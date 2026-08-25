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
    public class UserRemoveSingleRecordHTTP : BaseDataHTTP, IHTTPComms
    {
        private string m_urlRequest = "";

        public string UrlRequest
        {
            get
            {
                if (m_urlRequest.Length == 0)
                {
                    m_urlRequest = CommsHTTPConstants.Instance.GetBaseURL() + "UserRemove.php";
                }
                return m_urlRequest;
            }
        }

        public string Build(params object[] _list)
        {
            return "?id=" + (string)_list[0] + "&password=" + (string)_list[1] + "&delete=" + (string)_list[2];
        }

        public override void Response(string _response)
        {
            if (!ResponseCode(_response))
            {
                CommsHTTPConstants.Instance.DisplayLog(_jsonResponse);
                SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_CONFIRMATION_REMOVED_RECORD, false);
                return;
            }

            string[] response = _jsonResponse.Split(new string[] { CommController.TOKEN_SEPARATOR_EVENTS }, StringSplitOptions.None);
            SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_CONFIRMATION_REMOVED_RECORD, bool.Parse(response[0]), response[1]);
        }
    }
}
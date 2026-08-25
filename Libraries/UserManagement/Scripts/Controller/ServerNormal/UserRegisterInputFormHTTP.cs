using System;
using System.Text;
using UnityEngine;
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
	public class UserRegisterInputFormHTTP : BaseDataHTTP, IHTTPComms
	{
        private string m_urlRequest = "";

        public string UrlRequest
        {
            get
            {
                if (m_urlRequest.Length == 0)
                {
                    m_urlRequest = CommsHTTPConstants.Instance.GetBaseURL() + "UserRegisterInputForm.php";
                }
                return m_urlRequest;
            }
        }

        public string Build(params object[] _list)
		{
			_method = METHOD_POST;

			_formPost = new WWWForm();			
			_formPost.AddField("email", (string)_list[0]);
			_formPost.AddField("registered", yourvrexperience.Utils.Utilities.GetTimestamp().ToString());
			
			Encoding unicode = Encoding.UTF8;
			byte[] fileBinaryData = unicode.GetBytes((string)_list[1]);
			_formPost.AddField("size", fileBinaryData.Length);
			_formPost.AddBinaryData("data", fileBinaryData);

			return null;
        }

        public override void Response(string _response)
		{
			if (!ResponseCode(_response))
			{
				SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_INPUT_FORM_RESULT, false);
				return;
			}

			string[] data = _jsonResponse.Split(new string[] { CommController.TOKEN_SEPARATOR_EVENTS }, StringSplitOptions.None);
			bool success = bool.Parse(data[0]);
			if (success)
			{
				SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_INPUT_FORM_RESULT, true);
			}
			else
			{
				SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_INPUT_FORM_RESULT, false);
			}
		}
	}
}
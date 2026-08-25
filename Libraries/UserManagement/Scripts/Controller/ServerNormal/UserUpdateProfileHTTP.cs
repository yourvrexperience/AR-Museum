using System;
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
	public class UserUpdateProfileHTTP : BaseDataHTTP, IHTTPComms
	{
        private string m_user;
		private string m_name;
		private string m_address;
		private string m_description;

        private string m_urlRequest = "";

        public string UrlRequest
        {
            get
            {
                if (m_urlRequest.Length == 0)
                {
                    m_urlRequest = CommsHTTPConstants.Instance.GetBaseURL() + "UserUpdateProfile.php";
                }
                return m_urlRequest;
            }
        }

        public string Build(params object[] _list)
		{
			_method = METHOD_POST;

			_formPost = new WWWForm();
			_formPost.AddField("id", (string)_list[0]);
			_formPost.AddField("password", (string)_list[1]);
            _formPost.AddField("user", (string)_list[2]);
            _formPost.AddField("name", (string)_list[3]!=null ? (string)_list[3] : "");
            _formPost.AddField("address", (string)_list[4]!=null ? (string)_list[4] : "");
            _formPost.AddField("description", (string)_list[5]!=null ? (string)_list[5] : "");
            _formPost.AddField("data", (string)_list[6]!=null ? (string)_list[6] : "");
			_formPost.AddField("data2", (string)_list[7]!=null ? (string)_list[7] : "");
			_formPost.AddField("data3", (string)_list[8]!=null ? (string)_list[8] : "");
			_formPost.AddField("data4", (string)_list[9]!=null ? (string)_list[9] : "");
			_formPost.AddField("data5", (string)_list[10]!=null ? (string)_list[10] : "");

			m_user = (string)_list[2];
			m_name = (string)_list[3] != null ? (string)_list[3] : "";
			m_address = (string)_list[4] != null ? (string)_list[4] : "";
            m_description = (string)_list[5] != null ? (string)_list[5] : "";

            return null;
		}

		public override void Response(string _response)
		{
			if (!ResponseCode(_response))
			{
				CommsHTTPConstants.Instance.DisplayLog(_jsonResponse);
				SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_UPDATE_PROFILE_RESULT, false);
				return;
			}

			string[] data = _jsonResponse.Split(new string[] { CommController.TOKEN_SEPARATOR_USER_DATA }, StringSplitOptions.None);
            if (bool.Parse(data[0]))
			{
                SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_UPDATE_PROFILE_RESULT, true, data);
			}
			else
			{
                SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_UPDATE_PROFILE_RESULT, false);
			}
		}
	}

}
#if ENABLE_OFUSCATION
#if ENABLE_NEW_OFUSCATION
using GUPS.Obfuscator.Attribute;
#else
using OPS.Obfuscator.Attribute;
#endif
#endif
using System;
using System.Collections.Generic;
using yourvrexperience.Utils;

namespace yourvrexperience.UserManagement
{
#if ENABLE_OFUSCATION
    [DoNotRenameAttribute]
#endif
    public class UserConsultAllUsersHTTP : BaseDataHTTP, IHTTPComms
	{
        private string m_urlRequest = "";

        public string UrlRequest
        {
            get
            {
                if (m_urlRequest.Length == 0)
                {
                    m_urlRequest = CommsHTTPConstants.Instance.GetBaseURL() + "UserConsultAll.php";
                }
                return m_urlRequest;
            }
        }

        static string DecodeToString(byte[] bytes)
		{
			char[] chars = new char[bytes.Length / sizeof(char)];
			System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
			return new string(chars);
		}

		public string Build(params object[] _list)
		{
            string callParams = "?id=" + (string)_list[0] + "&password=" + (string)_list[1] + "&facebook=" + ((string)_list[2]).ToLower() + "&profile=" + ((string)_list[3]).ToLower();
            return callParams;
        }

		public override void Response(string _response)
		{
			if (!ResponseCode(_response))
			{
				CommsHTTPConstants.Instance.DisplayLog(_jsonResponse);
				SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_RESULT_CONSULT_ALL_RECORDS, false);
				return;
			}

            string[] blocks = _jsonResponse.Split(new string[] { CommController.TOKEN_SEPARATOR_BLOCKS }, StringSplitOptions.None);
            if (!bool.Parse(blocks[0]))
            {
                SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_RESULT_CONSULT_ALL_RECORDS, false);
            }
            else
            {
                string[] lines = blocks[1].Split(new string[] { CommController.TOKEN_SEPARATOR_LINES }, StringSplitOptions.None);
                List<string[]> usersRecords = new List<string[]>();
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] userRecord = lines[i].Split(new string[] { CommController.TOKEN_SEPARATOR_USER_DATA }, StringSplitOptions.None);
                    if (lines[i].Length > 0)
                    {
                        usersRecords.Add(userRecord);
                    }                    
                }
                SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_RESULT_CONSULT_ALL_RECORDS, true, usersRecords);
            }
        }
	}
}
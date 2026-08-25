using System;
using yourvrexperience.Utils;
#if ENABLE_OFUSCATION
#if ENABLE_NEW_OFUSCATION
using GUPS.Obfuscator.Attribute;
#else
using OPS.Obfuscator.Attribute;
#endif
#endif

namespace yourvrexperience.ai
{
#if ENABLE_OFUSCATION
    [DoNotRenameAttribute]
#endif
    public class ConsultAIInstructionsHTTP : BaseDataHTTP, IHTTPComms
	{
		public const string EventConsultAIInstructionsHTTPCompleted = "EventConsultAIInstructionsHTTPCompleted";

        private string m_urlRequest = "";
        private string _language;

        public string UrlRequest
        {
            get
            {
                if (m_urlRequest.Length == 0)
                {
                    m_urlRequest = GameAIData.Instance.URLBaseManagement + "MuseumAIInstructions.php";
                }
                return m_urlRequest;
            }
        }

		public string Build(params object[] _list)
		{
            _language = (string)_list[0];
            string callParams = "?language=" + _language;
            return callParams;
        }

		public override void Response(byte[] _response)
		{
			if (!ResponseUTF8Code(_response))
			{
				SystemEventController.Instance.DispatchSystemEvent(EventConsultAIInstructionsHTTPCompleted, false);
				return;
			}

			string[] instructionsData = _jsonResponse.Split(new String[] { CommController.TOKEN_SEPARATOR_EVENTS }, StringSplitOptions.None);
            if (!bool.Parse(instructionsData[0]))
            {
                SystemEventController.Instance.DispatchSystemEvent(EventConsultAIInstructionsHTTPCompleted, false);
            }
            else
            {
				SystemEventController.Instance.DispatchSystemEvent(EventConsultAIInstructionsHTTPCompleted, true, _language, instructionsData[1]);
            }
        }
	}

}
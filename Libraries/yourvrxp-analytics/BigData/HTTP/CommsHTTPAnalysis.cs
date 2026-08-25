using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.Analytics
{
    public class CommsHTTPAnalysis : MonoBehaviour
    {
        public string URL_BASE_PHP = "http://localhost:8080/usermanagement/";

        public const string URL_BASE_COOCKIE = "URL_BASE_COOCKIE";

        public const string EVENT_COMM_ANALYSIS_LOG_EVENT  = "yourvrexperience.Analytics.LogAnalysisEventHTTP";

        private static CommsHTTPAnalysis _instance;

        public static CommsHTTPAnalysis Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(CommsHTTPAnalysis)) as CommsHTTPAnalysis;
                }
                return _instance;
            }
        }

        private bool _thereIsConnection = true;

        public bool ThereIsConnection
        {
            get { return _thereIsConnection; }
            set { _thereIsConnection = value; }
        }

        void OnDestroy()
        {
            Destroy();
        }

        public void Destroy()
        {
            if (Instance != null)
            {
                GameObject.Destroy(_instance);
                _instance = null;
            }            
        }

        public void DisplayLog(string data)
        {
            CommController.Instance.DisplayLog(data);
        }

        private static string _urlBase = "";

        public string GetBaseURL()
        {
            if (_urlBase.Length == 0)
            {
                _urlBase = PlayerPrefs.GetString(URL_BASE_COOCKIE, URL_BASE_PHP);
            }
            return _urlBase;
        }

        public void SetBaseURL(string urlBase)
        {
            PlayerPrefs.SetString(URL_BASE_COOCKIE, urlBase);
        }

        public void LogEvent(string nameEvent, string email, int age, string language, int level, string jsonData)
        {            
            CommController.Instance.Request(EVENT_COMM_ANALYSIS_LOG_EVENT, false, nameEvent, email, age, language, level, jsonData);
        }
    }
}

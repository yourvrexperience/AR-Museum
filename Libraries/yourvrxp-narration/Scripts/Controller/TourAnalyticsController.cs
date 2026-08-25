using System.Collections.Generic;
using UnityEngine;
using static yourvrexperience.Narration.GameLevelData;
#if ENABLE_ANALYTICS
using yourvrexperience.Analytics;
#if ENABLE_FIREBASE
using Firebase;
using Firebase.Analytics;
#endif
#endif

namespace yourvrexperience.Narration
{
	public class TourAnalyticsController : MonoBehaviour
	{
        public const bool DEBUG = false;

        public const string EventPOIVisited = "POI-Visited";
        public const string EventPOIReplayed = "POI-Replayed";
        public const string EventEasterEggUnlocked = "Unlocked-Secret";
        public const string EventTrackingLost = "Tracking-Lost";
        public const string EventTimeDone = "Time-Done";
        public const string EventLanguageOS = "Language-OS";
        public const string EventLanguageSelected = "Language-Selected";
        public const string EventAIQuestion = "AI-Question";

        private static TourAnalyticsController _instance;

        public static TourAnalyticsController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(TourAnalyticsController)) as TourAnalyticsController;
                }
                return _instance;
            }
        }

        private int _floor = 0;
        private string _email = "";
        private string _language = "";

        public string Email {                
                get { return _email; }
                set { _email = value; }
        }

        public int Floor {                
                get { return _floor; }
                set { _floor = value; }
        }

        public string Language {                
                get { return _language; }
                set { _language = value; }
        }

#if ENABLE_FIREBASE
         private Firebase.FirebaseApp _app;
#endif
		public void Initialize()
		{
#if ENABLE_ANALYTICS            
#if ENABLE_FIREBASE         
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
                var dependencyStatus = task.Result;
                if (dependencyStatus == Firebase.DependencyStatus.Available)
                {
                    _app = Firebase.FirebaseApp.DefaultInstance;
                    if (DEBUG) Debug.LogError("++++++++++++++FIREBASE INITIALITZATION SUCCESS");
                } 
                else 
                {
                    Debug.LogError(System.String.Format("Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                }
            });
#endif    
#endif            
		}

        public void LogPOIVisitedEvent(GameAge age, int index, float started, float ended, bool skipped, float skiptime, int paused, int restarted)
        {
#if ENABLE_ANALYTICS
#if ENABLE_FIREBASE
            FirebaseAnalytics.LogEvent(EventPOIVisited, 
                                        new Parameter("age", (int)age),
                                        new Parameter("number", index),
                                        new Parameter("started", started),
                                        new Parameter("ended", ended),
                                        new Parameter("skipped", (skipped?1:0)),
                                        new Parameter("skiptime", skiptime),
                                        new Parameter("paused", paused),
										new Parameter("restarted", restarted));
#else
        List<ParameterAnalysis> parameters = new List<ParameterAnalysis>
        {
            new ParameterAnalysis("age", "integer", (int)age),
            new ParameterAnalysis("number", "integer", index),
            new ParameterAnalysis("started", "float", started),
            new ParameterAnalysis("ended", "float", ended),
            new ParameterAnalysis("skipped", "bool", skipped),
            new ParameterAnalysis("skiptime", "float", skiptime),
            new ParameterAnalysis("paused", "integer", paused),
            new ParameterAnalysis("restarted", "integer", restarted)
        };

        string jsonData = JsonHelper.ToJson<ParameterAnalysis>(parameters.ToArray(), true);
        CommsHTTPAnalysis.Instance.LogEvent(EventPOIVisited, _email, (int)age, _language, _floor, jsonData);
#endif 
#endif                                           
        }

        public void LogAIQuestionEvent(GameAge age, string question, string answer)
        {
#if ENABLE_ANALYTICS
#if ENABLE_FIREBASE
        FirebaseAnalytics.LogEvent(EventAIQuestion, 
                                        new Parameter("age", (int)age),
                                        new Parameter("question", question),
                                        new Parameter("answer", answer));
#else
        List<ParameterAnalysis> parameters = new List<ParameterAnalysis>
        {
            new ParameterAnalysis("age", "integer", (int)age),
            new ParameterAnalysis("question", "string", question),
            new ParameterAnalysis("answer", "string", answer)
        };

        string jsonData = JsonHelper.ToJson<ParameterAnalysis>(parameters.ToArray(), true);
        CommsHTTPAnalysis.Instance.LogEvent(EventAIQuestion, _email, (int)age, _language, _floor, jsonData);
#endif 
#endif                                           
        }

        public void LogPOIReplayEvent(GameAge age, int index)
        {
#if ENABLE_ANALYTICS            
#if ENABLE_FIREBASE
        FirebaseAnalytics.LogEvent(EventPOIReplayed, 
                                    new Parameter("age", (int)age),
                                    new Parameter("number", index));
#else
        List<ParameterAnalysis> parameters = new List<ParameterAnalysis>
        {
            new ParameterAnalysis("age", "integer", (int)age),
            new ParameterAnalysis("number", "integer", index)
        };

        string jsonData = JsonHelper.ToJson<ParameterAnalysis>(parameters.ToArray(), true);
        CommsHTTPAnalysis.Instance.LogEvent(EventPOIReplayed, _email, (int)age, _language, _floor, jsonData);
#endif      
#endif                                      
        }

        public void LogEasterEggUnlockedEvent(GameAge age, int area, int index)
        {
#if ENABLE_ANALYTICS            
#if ENABLE_FIREBASE
            FirebaseAnalytics.LogEvent(EventEasterEggUnlocked, 
                                        new Parameter("age", (int)age),
                                        new Parameter("area", area),                                        
                                        new Parameter("number", index));
#else
            List<ParameterAnalysis> parameters = new List<ParameterAnalysis>
            {
                new ParameterAnalysis("age", "integer", (int)age),
                new ParameterAnalysis("area", "integer", area),                
                new ParameterAnalysis("number", "integer", index)
            };

            string jsonData = JsonHelper.ToJson<ParameterAnalysis>(parameters.ToArray(), true);
            CommsHTTPAnalysis.Instance.LogEvent(EventEasterEggUnlocked, _email, (int)age, _language, _floor, jsonData);
#endif
#endif    
        }

        public void LogTrackingLostEvent(GameAge age, int poiIndex)
        {
#if ENABLE_ANALYTICS            
#if ENABLE_FIREBASE            
            FirebaseAnalytics.LogEvent(EventTrackingLost, 
                                        new Parameter("age", (int)age),
                                        new Parameter("poi", poiIndex));
#else
            List<ParameterAnalysis> parameters = new List<ParameterAnalysis>
            {
                new ParameterAnalysis("age", "integer", (int)age),
                new ParameterAnalysis("poi", "integer", poiIndex)
            };

            string jsonData = JsonHelper.ToJson<ParameterAnalysis>(parameters.ToArray(), true);
            CommsHTTPAnalysis.Instance.LogEvent(EventTrackingLost, _email, (int)age, _language, _floor, jsonData);
#endif           
#endif                                             
        }	

        public void SceneLoadedEvent(GameAge age, int indexScene, string nameScene)
        {
#if ENABLE_ANALYTICS
#if ENABLE_FIREBASE
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelStart, 
                                        new Parameter("age", (int)age),
                                        new Parameter(FirebaseAnalytics.ParameterLevel, indexScene),
                                        new Parameter(FirebaseAnalytics.ParameterLevelName, nameScene));
#else
            List<ParameterAnalysis> parameters = new List<ParameterAnalysis>
            {
                new ParameterAnalysis("age", "integer", (int)age),
                new ParameterAnalysis("level", "integer", indexScene),
                new ParameterAnalysis("name", "string", nameScene)
            };

            string jsonData = JsonHelper.ToJson<ParameterAnalysis>(parameters.ToArray(), true);
            CommsHTTPAnalysis.Instance.LogEvent("EventLevelStart", _email, (int)age, _language, _floor, jsonData);
#endif    
#endif                                           
        }

        public void SceneCompletedEvent(GameAge age, int indexScene, string nameScene, float timeDone)
        {
#if ENABLE_ANALYTICS
#if ENABLE_FIREBASE
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelEnd,
                                        new Parameter("age", (int)age), 
                                        new Parameter(FirebaseAnalytics.ParameterLevel, indexScene),
                                        new Parameter(FirebaseAnalytics.ParameterLevelName, nameScene),
                                        new Parameter(EventTimeDone, timeDone));
#else
            List<ParameterAnalysis> parameters = new List<ParameterAnalysis>
            {
                new ParameterAnalysis("age", "integer", (int)age),
                new ParameterAnalysis("level", "integer", indexScene),
                new ParameterAnalysis("name", "string", nameScene),
                new ParameterAnalysis("time", "float", timeDone)
            };

            string jsonData = JsonHelper.ToJson<ParameterAnalysis>(parameters.ToArray(), true);
            CommsHTTPAnalysis.Instance.LogEvent("EventLevelEnd", _email, (int)age, _language, _floor, jsonData);
#endif
#endif    
        }

        public void LogEvent(GameAge age, string nameEvent, string value)
        {
#if ENABLE_ANALYTICS            
#if ENABLE_FIREBASE            
            FirebaseAnalytics.LogEvent(nameEvent, new Parameter("type", value));
#else
            List<ParameterAnalysis> parameters = new List<ParameterAnalysis>
            {
                new ParameterAnalysis("type", "string", value)
            };

            string jsonData = JsonHelper.ToJson<ParameterAnalysis>(parameters.ToArray(), true);
            CommsHTTPAnalysis.Instance.LogEvent(nameEvent, _email, (int)age, _language, _floor, jsonData);
#endif                                           
#endif    
        }

        public void LogEvent(GameAge age, string nameEvent, int value)
        {
#if ENABLE_ANALYTICS            
#if ENABLE_FIREBASE            
            FirebaseAnalytics.LogEvent(nameEvent, new Parameter("type", value));
#else
            List<ParameterAnalysis> parameters = new List<ParameterAnalysis>
            {
                new ParameterAnalysis("type", "integer", value)
            };

            string jsonData = JsonHelper.ToJson<ParameterAnalysis>(parameters.ToArray(), true);
            CommsHTTPAnalysis.Instance.LogEvent(nameEvent, _email, (int)age, _language, _floor, jsonData);
#endif                                           
#endif    
        }

        public void LogEvent(GameAge age, string nameEvent, float value)
        {
#if ENABLE_ANALYTICS            
#if ENABLE_FIREBASE            
            FirebaseAnalytics.LogEvent(nameEvent, new Parameter("type", value));
#else
            List<ParameterAnalysis> parameters = new List<ParameterAnalysis>
            {
                new ParameterAnalysis("type", "float", value)
            };

            string jsonData = JsonHelper.ToJson<ParameterAnalysis>(parameters.ToArray(), true);
            CommsHTTPAnalysis.Instance.LogEvent(nameEvent, _email, (int)age, _language, _floor, jsonData);
#endif    
#endif                                           
        }

        public void LogLanguageOSEvent(string codeLanguage)
        {
#if ENABLE_ANALYTICS            
#if ENABLE_FIREBASE            
            FirebaseAnalytics.LogEvent(EventLanguageOS, 
                                        new Parameter("language", codeLanguage));
#else
            List<ParameterAnalysis> parameters = new List<ParameterAnalysis>
            {
                new ParameterAnalysis("language", "string", codeLanguage)
            };

            string jsonData = JsonHelper.ToJson<ParameterAnalysis>(parameters.ToArray(), true);
            CommsHTTPAnalysis.Instance.LogEvent(EventLanguageOS, _email, -1, _language, -1, jsonData);
#endif
#endif    
        }

        public void LogLanguageSelectedEvent(string codeLanguage)
        {
#if ENABLE_ANALYTICS            
#if ENABLE_FIREBASE            
           FirebaseAnalytics.LogEvent(EventLanguageSelected, 
                                        new Parameter("language", codeLanguage));
#else
            List<ParameterAnalysis> parameters = new List<ParameterAnalysis>
            {
                new ParameterAnalysis("language", "string", codeLanguage)
            };

            string jsonData = JsonHelper.ToJson<ParameterAnalysis>(parameters.ToArray(), true);
            CommsHTTPAnalysis.Instance.LogEvent(EventLanguageSelected, _email, -1, _language, -1, jsonData);
#endif                                           
#endif    
        }
	}
}
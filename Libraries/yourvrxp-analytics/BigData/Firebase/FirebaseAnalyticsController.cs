#if ENABLE_FIREBASE
using Firebase;
using Firebase.Analytics;
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using yourvrexperience.Utils;

namespace yourvrexperience.Analytics
{
    public class FirebaseAnalyticsController : MonoBehaviour
    {
        private const bool DEBUG = false;

        public const string EventTimeDone = "Time-Done";
        public const string EventLanguageOS = "Language-OS";
        public const string EventLanguageSelected = "Language-Selected";

        private List<TimedEventData> _listEvents = new List<TimedEventData>();
#if ENABLE_FIREBASE        
        private Firebase.FirebaseApp _app;

        public virtual void Initialize()
        {
            if (DEBUG) Debug.LogError("FirebaseController::CALL INITIALITZATION");
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
        }

        public void SceneLoadedEvent(int indexScene, string nameScene)
        {
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelStart, 
                                        new Parameter(FirebaseAnalytics.ParameterLevel, indexScene),
                                        new Parameter(FirebaseAnalytics.ParameterLevelName, nameScene));
        }

        public void SceneCompletedEvent(int indexScene, string nameScene, float timeDone)
        {
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelEnd, 
                                        new Parameter(FirebaseAnalytics.ParameterLevel, indexScene),
                                        new Parameter(FirebaseAnalytics.ParameterLevelName, nameScene),
                                        new Parameter(EventTimeDone, timeDone));
        }

        public void LogEvent(string nameEvent, string value)
        {
            FirebaseAnalytics.LogEvent(nameEvent, new Parameter("type", value));
        }

        public void LogEvent(string nameEvent, int value)
        {
            FirebaseAnalytics.LogEvent(nameEvent, new Parameter("type", value));
        }

        public void LogEvent(string nameEvent, float value)
        {
            FirebaseAnalytics.LogEvent(nameEvent, new Parameter("type", value));
        }

        public void LogLanguageOSEvent(string codeLanguage)
        {
            FirebaseAnalytics.LogEvent(EventLanguageOS, 
                                        new Parameter("language", codeLanguage));
        }

        public void LogLanguageSelectedEvent(string codeLanguage)
        {
            FirebaseAnalytics.LogEvent(EventLanguageSelected, 
                                        new Parameter("language", codeLanguage));
        }
#endif        
    }
}
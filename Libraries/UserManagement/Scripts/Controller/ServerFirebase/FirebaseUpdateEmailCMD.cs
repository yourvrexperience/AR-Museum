#if ENABLE_FIREBASE
using Firebase;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using yourvrexperience.Utils;


namespace yourvrexperience.UserManagement
{
    public class FirebaseUpdateEmailCMD : MonoBehaviour
    {
        public const bool DEBUG = false;

         public const float DELAY_TO_REPORT_EVENT = 0.5f;

        public const string EVENT_FIREBASE_UPDATE_EMAIL_REPORT_UPDATED = "EVENT_FIREBASE_UPDATE_EMAIL_REPORT_UPDATED";
        public const string EVENT_FIREBASE_UPDATE_EMAIL_DELAYED_DESTROY = "EVENT_FIREBASE_UPDATE_EMAIL_DELAYED_DESTROY";

        private string _emailPlaceholder;
        private string _emailFinal;
        private string _password;
        
        public static void CommandFirebaseUpdateEmail(params object[] parameters)
        {
            GameObject command = new GameObject();
            command.AddComponent<FirebaseUpdateEmailCMD>().Initialize(parameters);   
            command.name = "FirebaseUpdateEmailCMD";
        }

        public void Initialize(params object[] parameters)
        {
            SystemEventController.Instance.Event += OnSystemEvent;

            _emailPlaceholder = (string)parameters[0];
            _emailFinal = (string)parameters[1];

            UpdateUserEmail();
        }

        public void OnDestroy()
        {
            Destroy();
        }

        public void Destroy()
        {
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
        }

        private void UpdateUserEmail()
        {
            if (DEBUG) Debug.LogError("FirebaseUpdateEmailCMD::UpdateUserEmail::UPDATE EMAIL USER[" + _emailPlaceholder + " WITH " + _emailFinal + "]!!!!!");

            FirebaseDatabase.DefaultInstance
                    .GetReference(FirebaseController.TABLE_USERS).OrderByChild("email").EqualTo(_emailPlaceholder).LimitToFirst(2)
                    .GetValueAsync().ContinueWith(x =>
                    {
                        if (x.Result != null && x.Result.ChildrenCount > 0)
                        {
                            if (DEBUG) Debug.LogError("FirebaseUpdateEmailCMD::UpdateUserEmail::FOUND A TOTAL OF[" + x.Result.ChildrenCount + "]!!!!!");
                            foreach (var childSnapshot in x.Result.Children)
                            {
                                childSnapshot.Child("email").Reference.SetValueAsync(_emailFinal);

                                SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_UPDATE_EMAIL_REPORT_UPDATED, DELAY_TO_REPORT_EVENT);
                                return;
                            }
                        }
                        else
                        {
                            SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_UPDATE_EMAIL_REPORT_UPDATED, DELAY_TO_REPORT_EVENT);
                        }
                    });
        }


        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent == EVENT_FIREBASE_UPDATE_EMAIL_REPORT_UPDATED)
            {
                SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_UPDATE_EMAIL_DELAYED_DESTROY, 1);
            }
            if (nameEvent == EVENT_FIREBASE_UPDATE_EMAIL_DELAYED_DESTROY)
            {
                GameObject.Destroy(this.gameObject);
            }
        }

    }
}
#endif
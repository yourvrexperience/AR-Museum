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
    public class FirebaseRemoveUserCMD : MonoBehaviour
    {
        public const bool DEBUG = false;

        public const float DELAY_TO_REPORT_EVENT = 0.5f;

        public const string EVENT_FIREBASE_REMOVE_USER_RECORD       = "EVENT_FIREBASE_REMOVE_USER_RECORD";
        public const string EVENT_FIREBASE_REMOVE_PROFILE_RECORD    = "EVENT_FIREBASE_REMOVE_PROFILE_RECORD";
        public const string EVENT_FIREBASE_REMOVE_DELAYED_DESTROY   = "EVENT_FIREBASE_REMOVE_DELAYED_DESTROY";
        public const string EVENT_FIREBASE_REMOVE_AUTHENTICATED   = "EVENT_FIREBASE_REMOVE_AUTHENTICATED";

        private long _userID;
        private LoginPlatforms _loginPlatform;
        private string _accessToken;

        public static void CommandFirebaseRemoveUser(params object[] parameters)
        {
            GameObject command = new GameObject();
            command.AddComponent<FirebaseRemoveUserCMD>().Initialize(parameters);            
            command.name = "FirebaseRemoveUserCMD";   
        }

        public void Initialize(params object[] parameters)
        {
            SystemEventController.Instance.Event += OnSystemEvent;

            _userID = (long)parameters[0];
            _loginPlatform = (LoginPlatforms)parameters[1];
            _accessToken = (string)parameters[2];

            RemoveByUserID();
        }

        public void OnDestroy()
        {
            Destroy();
        }

        public void Destroy()
        {
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
        }

        private void RemoveByUserID()
        {
            if (DEBUG) Debug.LogError("FirebaseLoginUserCMD::RemoveByUserID::REMOVE USER ID[" + _userID + "]!!!!!");

            FirebaseDatabase.DefaultInstance
                    .GetReference(FirebaseController.TABLE_USERS).OrderByChild("id").EqualTo(_userID).LimitToFirst(2)
                    .GetValueAsync().ContinueWith(x =>
                    {
                        if (x.Result != null && x.Result.ChildrenCount > 0)
                        {
                            if (DEBUG) Debug.LogError("FirebaseLoginUserCMD::RemoveByUserID::FOUND A TOTAL OF[" + x.Result.ChildrenCount + "]!!!!!");
                            foreach (var childSnapshot in x.Result.Children)
                            {
                                string email = childSnapshot.Child("email").Value.ToString();
                                string password = childSnapshot.Child("password").Value.ToString();
                                childSnapshot.Reference.SetValueAsync(null);
                                SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_REMOVE_USER_RECORD, DELAY_TO_REPORT_EVENT, true, email, password);
                                return;
                            }
                        }
                        else
                        {
                            SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_REMOVE_USER_RECORD, DELAY_TO_REPORT_EVENT, false);
                        }
                    });
        }

        private void RemoveProfileByUserID()
        {
            if (DEBUG) Debug.LogError("FirebaseLoginUserCMD::RemoveProfileByUserID::REMOVE PROFILE BY USER ID[" + _userID + "]!!!!!");

            FirebaseDatabase.DefaultInstance
                    .GetReference(FirebaseController.TABLE_PROFILES).OrderByChild("user").EqualTo(_userID).LimitToFirst(2)
                    .GetValueAsync().ContinueWith(x =>
                    {
                        if (x.Result != null && x.Result.ChildrenCount > 0)
                        {
                            if (DEBUG) Debug.LogError("FirebaseLoginUserCMD::RemoveProfileByUserID::FOUND A TOTAL OF[" + x.Result.ChildrenCount + "]!!!!!");
                            foreach (var childSnapshot in x.Result.Children)
                            {
                                childSnapshot.Reference.SetValueAsync(null);
                                SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_REMOVE_PROFILE_RECORD, DELAY_TO_REPORT_EVENT, true);
                                return;
                            }
                        }
                        else
                        {
                            SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_REMOVE_PROFILE_RECORD, DELAY_TO_REPORT_EVENT, false);
                        }
                    });
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent == EVENT_FIREBASE_REMOVE_USER_RECORD)
            {
                if ((bool)parameters[0])
                {
                    string email = (string)parameters[1];
                    string password = (string)parameters[2];
                    switch (_loginPlatform)
                    {
                        case LoginPlatforms.Email:
                            FirebaseController.Instance.SignInAuthenticatedUser(email, password, EVENT_FIREBASE_REMOVE_AUTHENTICATED);
                            break;

                        case LoginPlatforms.Facebook:
                            UsersController.Instance.LoginRequested = true;
                            FirebaseController.Instance.CreateFacebookAuthenticatedUser(_accessToken, EVENT_FIREBASE_REMOVE_AUTHENTICATED);
                            break;

                        case LoginPlatforms.Google:
                            UsersController.Instance.LoginRequested = true;
                            FirebaseController.Instance.CreateGoogleAuthenticatedUser(_accessToken, EVENT_FIREBASE_REMOVE_AUTHENTICATED);
                            break;

                        case LoginPlatforms.Apple:
                            UsersController.Instance.LoginRequested = true;
                            FirebaseController.Instance.CreateAppleAuthenticatedUser(_accessToken, EVENT_FIREBASE_REMOVE_AUTHENTICATED);
                            break;
                    }
                }
            }
            if (nameEvent == EVENT_FIREBASE_REMOVE_AUTHENTICATED)
            {
                bool success = (bool)parameters[0];
                if (success)
                {
                    FirebaseController.Instance.RemoveAuthenticatedUser((Firebase.Auth.FirebaseUser)parameters[1]);
                }
            }
            if (nameEvent == FirebaseController.EVENT_FIREBASE_AUTH_USER_DELETED_RESULT)
            {
                if ((bool)parameters[0])
                {
                    RemoveProfileByUserID();
                }
            }
            if (nameEvent == EVENT_FIREBASE_REMOVE_PROFILE_RECORD)
            {
                SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_CONFIRMATION_REMOVED_RECORD, true, _userID.ToString());
            }
            if (nameEvent == UsersController.EVENT_USER_CONFIRMATION_REMOVED_RECORD)
            {
                SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_REMOVE_DELAYED_DESTROY, 2);
            }
            if (nameEvent == EVENT_FIREBASE_REMOVE_DELAYED_DESTROY)
            {
                GameObject.Destroy(this.gameObject);
            }
        }

    }
}
#endif
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
    public class FirebaseUpdateProfileCMD : MonoBehaviour
    {
        public const bool DEBUG = false;

        public const float DELAY_TO_REPORT_EVENT = 0.5f;

        public const string EVENT_FIREBASE_UPDATE_PROFILE_REPORT_UPDATED = "EVENT_FIREBASE_UPDATE_PROFILE_REPORT_UPDATED";
        public const string EVENT_FIREBASE_UPDATE_PROFILE_DELAYED_DESTROY = "EVENT_FIREBASE_UPDATE_PROFILE_DELAYED_DESTROY";

        private LoginPlatforms _loginPlatform;
        private string _accessToken;
        private long _userID;
        private string _nameProfile;
        private string _addressProfile;
        private string _descriptionProfile;
        private string _dataProfile;
        
        public static void CommandFirebaseUpdateProfile(params object[] parameters)
        {
            if (GameObject.FindAnyObjectByType<FirebaseUpdateProfileCMD>() == null)
            {
                GameObject command = new GameObject();
                command.AddComponent<FirebaseUpdateProfileCMD>().Initialize(parameters);   
                command.name = "FirebaseUpdateProfileCMD";            
            }
        }

        public void Initialize(params object[] parameters)
        {
            SystemEventController.Instance.Event += OnSystemEvent;

            _loginPlatform = (LoginPlatforms)parameters[0];
            _accessToken = (string)parameters[1];
            _userID = (long)parameters[2];
            _nameProfile = (string)parameters[3];
            _addressProfile = (string)parameters[4];
            _descriptionProfile = (string)parameters[5];
            _dataProfile = (string)parameters[6];

            UpdateUserProfile();
        }

        public void OnDestroy()
        {
            Destroy();
        }

        public void Destroy()
        {
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
        }

        private void UpdateUserProfile()
        {
            if (DEBUG) Debug.LogError("FirebaseUpdateProfileCMD::UpdaateUserProfile::UPDATE PROFILE USER[" + _userID + "]!!!!!");

            FirebaseDatabase.DefaultInstance
                    .GetReference(FirebaseController.TABLE_PROFILES).OrderByChild("user").EqualTo(_userID).LimitToFirst(2)
                    .GetValueAsync().ContinueWith(x =>
                    {
                        if (x.Result != null && x.Result.ChildrenCount > 0)
                        {
                            if (DEBUG) Debug.LogError("FirebaseUpdateProfileCMD::UpdaateUserProfile::FOUND A TOTAL OF[" + x.Result.ChildrenCount + "]!!!!!");
                            foreach (var childSnapshot in x.Result.Children)
                            {
                                childSnapshot.Child("name").Reference.SetValueAsync(_nameProfile);
                                childSnapshot.Child("address").Reference.SetValueAsync(_addressProfile);
                                childSnapshot.Child("description").Reference.SetValueAsync(_descriptionProfile);
                                childSnapshot.Child("data").Reference.SetValueAsync(_dataProfile);

                                SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_UPDATE_PROFILE_REPORT_UPDATED, DELAY_TO_REPORT_EVENT, _userID);
                                return;
                            }
                        }
                        else
                        {
                            SystemEventController.Instance.DelaySystemEvent(UsersController.EVENT_USER_UPDATE_PROFILE_RESULT, DELAY_TO_REPORT_EVENT, false);
                        }
                    });
        }


        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent == EVENT_FIREBASE_UPDATE_PROFILE_REPORT_UPDATED)
            {
                long userIDTarget = (long)parameters[0];
                _userID = userIDTarget;
                FirebaseLoginUserCMD.CommandFirebaseLoginUser(_loginPlatform, _accessToken, _userID, "", UsersController.EVENT_USER_UPDATE_PROFILE_RESULT, true);                
            }
            if (nameEvent == UsersController.EVENT_USER_UPDATE_PROFILE_RESULT)
            {
                SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_UPDATE_PROFILE_DELAYED_DESTROY, 2);
            }
            if (nameEvent == EVENT_FIREBASE_UPDATE_PROFILE_DELAYED_DESTROY)
            {
                GameObject.Destroy(this.gameObject);
            }
        }

    }
}
#endif
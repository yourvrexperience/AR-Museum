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
    public class FirebaseLoginUserCMD : MonoBehaviour
    {
        public const bool DEBUG = false;

        public const float DELAY_TO_REPORT_EVENT = 0.5f;

        public const string EVENT_FIREBASELOGIN_USER_RECORD     = "EVENT_FIREBASELOGIN_USER_RECORD";
        public const string EVENT_FIREBASELOGIN_PROFILE_RECORD  = "EVENT_FIREBASELOGIN_PROFILE_RECORD";
        public const string EVENT_FIREBASELOGIN_DELAYED_DESTROY = "EVENT_FIREBASELOGIN_DELAYED_DESTROY";

        private LoginPlatforms _loginPlatform;
        private string _accessToken;
        private string _email;
        private string _password;
        private string _outputEvent;
        private bool _requestProfile = false;
        private bool _requestFacebook = false;

        private long _userID;
        private string[] _dataFormatUser;

        public static void CommandFirebaseLoginUser(params object[] parameters)
        {
            GameObject command = new GameObject();
            command.AddComponent<FirebaseLoginUserCMD>().Initialize(parameters);
            command.name = "FirebaseLoginUserCMD";
        }

        public void Initialize(params object[] parameters)
        {
            SystemEventController.Instance.Event += OnSystemEvent;

            bool checkByEmail = false;
            _loginPlatform = (LoginPlatforms)parameters[0];
            _accessToken = (string)parameters[1];
            if (parameters[2] is string)
            {
                _email = (string)parameters[2];
                checkByEmail = true;
            }
            else
            {
                if (parameters[2] is long)
                {
                    _userID = (long)parameters[2];
                }
            }            
            _password = (string)parameters[3];
            _outputEvent = (string)parameters[4];
            if (parameters.Length > 5) _requestProfile = (bool)parameters[5];

            if (checkByEmail)
            {
                if (_password.Length == 0)
                {
                    SearchMailLogin();
                }
                else
                {
                    switch (_loginPlatform)
                    {
                        case LoginPlatforms.Email:
                            FirebaseController.Instance.SignInAuthenticatedUser(_email, _password, FirebaseController.EVENT_FIREBASE_AUTH_USER_SIGNIN_RESULT);
                            break;

                        case LoginPlatforms.Facebook:
                            UsersController.Instance.LoginRequested = true;
                            FirebaseController.Instance.CreateFacebookAuthenticatedUser(_accessToken, FirebaseController.EVENT_FIREBASE_AUTH_USER_SIGNIN_RESULT);
                            break;

                        case LoginPlatforms.Google:
                            UsersController.Instance.LoginRequested = true;
                            FirebaseController.Instance.CreateGoogleAuthenticatedUser(_accessToken, FirebaseController.EVENT_FIREBASE_AUTH_USER_SIGNIN_RESULT);
                            break;

                        case LoginPlatforms.Apple:
                            UsersController.Instance.LoginRequested = true;
                            FirebaseController.Instance.CreateAppleAuthenticatedUser(_accessToken, FirebaseController.EVENT_FIREBASE_AUTH_USER_SIGNIN_RESULT);
                            break;
                    }                    
                }
            }
            else
            {
                SearchUserIDLogin();
            }
        }

        public void OnDestroy()
        {
            Destroy();
        }

        public void Destroy()
        {
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
        }

        public static string ParseSnapshotUser(DataSnapshot childSnapshot)
        {
            string packetUser = UserModel.FormatPacketData(yourvrexperience.Utils.Utilities.CastAsLong(childSnapshot.Child("id").Value),
                                                     childSnapshot.Child("email").Value.ToString(),
                                                     childSnapshot.Child("password").Value.ToString(),
                                                     childSnapshot.Child("nickname").Value.ToString(),
                                                     yourvrexperience.Utils.Utilities.CastAsLong(childSnapshot.Child("registerdate").Value),
                                                     yourvrexperience.Utils.Utilities.CastAsLong(childSnapshot.Child("lastlogin").Value),
                                                     yourvrexperience.Utils.Utilities.CastAsInteger(childSnapshot.Child("admin").Value),
                                                     yourvrexperience.Utils.Utilities.CastAsInteger(childSnapshot.Child("validated").Value));
            return packetUser;
        }

        private void SearchMailLogin()
        {
            if (DEBUG) Debug.LogError("FirebaseLoginUserCMD::SearchMailLogin::LOOKING FOR [" + _email + "," + _password + "]!!!!!");

            FirebaseDatabase.DefaultInstance
                    .GetReference(FirebaseController.TABLE_USERS).OrderByChild("email").EqualTo(_email).LimitToFirst(2)
                    .GetValueAsync().ContinueWith(x => {
                        if (x.Result != null && x.Result.ChildrenCount > 0)
                        {
                            foreach (var childSnapshot in x.Result.Children)
                            {
                                if (childSnapshot.Child("id") == null || childSnapshot.Child("id").Value == null)
                                {
                                    if (DEBUG) Debug.LogError("Bad data in sample.  Did you forget to call SetEditorDatabaseUrl with your project id?");
                                    SystemEventController.Instance.DelaySystemEvent(_outputEvent, DELAY_TO_REPORT_EVENT, false);                                 
                                }
                                else
                                {
                                    if (DEBUG) Debug.LogError("FirebaseLoginUserCMD::SearchMailLogin::FOUND RECORD[" + childSnapshot.Child("id").Value.ToString() + "] FOR [" + _email + "," + _password + "]!!!!!");
                                    _userID = yourvrexperience.Utils.Utilities.CastAsLong(childSnapshot.Child("id").Value);
                                    string packetUser = ParseSnapshotUser(childSnapshot);
                                    _dataFormatUser = new string[4];
                                    _dataFormatUser[0] = "true";
                                    _dataFormatUser[1] = packetUser;
                                    _dataFormatUser[2] = "";
                                    _dataFormatUser[3] = "";
                                    SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASELOGIN_USER_RECORD, DELAY_TO_REPORT_EVENT);                            
                                }
                            }
                        }
                        else
                        {
                            if (DEBUG) Debug.LogError("FirebaseLoginUserCMD::SearchMailLogin::NO RECORD FOUND FOR [" + _email + "," + _password + "]");
                            SystemEventController.Instance.DelaySystemEvent(_outputEvent, DELAY_TO_REPORT_EVENT, false);  
                        }
                    });
        }

        private void SearchUserIDLogin()
        {
            if (DEBUG) Debug.LogError("FirebaseLoginUserCMD::SearchUserIDLogin::LOOKING FOR [" + _email + "," + _password + "]!!!!!");

            FirebaseDatabase.DefaultInstance
                    .GetReference(FirebaseController.TABLE_USERS).OrderByChild("id").EqualTo(_userID).LimitToFirst(2)
                    .GetValueAsync().ContinueWith(x => {
                        if (x.Result != null && x.Result.ChildrenCount > 0)
                        {
                            foreach (var childSnapshot in x.Result.Children)
                            {
                                if (childSnapshot.Child("id") == null || childSnapshot.Child("id").Value == null)
                                {
                                    if (DEBUG) Debug.LogError("Bad data in sample.  Did you forget to call SetEditorDatabaseUrl with your project id?");
                                    SystemEventController.Instance.DelaySystemEvent(_outputEvent, DELAY_TO_REPORT_EVENT, false);
                                }
                                else
                                {
                                    string passwordDB = childSnapshot.Child("password").Value.ToString();
                                    if (DEBUG) Debug.LogError("FirebaseLoginUserCMD::SearchUserIDLogin::PASSWORD[" + passwordDB + "] NON-ENCRYPTED");
                                    if ((passwordDB != _password) && (_password.Length > 0))
                                    {
                                        if (DEBUG) Debug.LogError("FirebaseLoginUserCMD::SearchUserIDLogin::PASSWORD[" + _password + "] DOESN'T MATCH[" + childSnapshot.Child("password").Value.ToString() + "]");
                                        SystemEventController.Instance.DelaySystemEvent(_outputEvent, DELAY_TO_REPORT_EVENT, false);
                                    }
                                    else
                                    {
                                        if (DEBUG) Debug.LogError("FirebaseLoginUserCMD::SearchUserIDLogin::FOUND RECORD[" + childSnapshot.Child("id").Value.ToString() + "] FOR [" + _email + "," + _password + "]!!!!!");
                                        _userID = yourvrexperience.Utils.Utilities.CastAsLong(childSnapshot.Child("id").Value);
                                        string packetUser = ParseSnapshotUser(childSnapshot);
                                        _dataFormatUser = new string[4];
                                        _dataFormatUser[0] = "true";
                                        _dataFormatUser[1] = packetUser;
                                        _dataFormatUser[2] = "";
                                        _dataFormatUser[3] = "";
                                        SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASELOGIN_USER_RECORD, DELAY_TO_REPORT_EVENT);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (DEBUG) Debug.LogError("FirebaseLoginUserCMD::SearchUserIDLogin::NO RECORD FOUND FOR [" + _email + "," + _password + "]");
                            SystemEventController.Instance.DelaySystemEvent(_outputEvent, DELAY_TO_REPORT_EVENT, false);
                        }
                    });
        }

        private void SearchProfileLogin()
        {
            if (DEBUG) Debug.LogError("FirebaseLoginUserCMD::SearchProfileLogin::LOOKING FOR m_userID[" + _userID + "] IN PROFILES!!!!!");

            FirebaseDatabase.DefaultInstance
                    .GetReference(FirebaseController.TABLE_PROFILES).OrderByChild("user").EqualTo(_userID)
                    .ValueChanged += (object sender2, ValueChangedEventArgs e2) => {
                        if (e2.DatabaseError != null)
                        {
                            Debug.LogError(e2.DatabaseError.Message);
                            return;
                        }
                        if (e2.Snapshot != null && e2.Snapshot.ChildrenCount > 0)
                        {
                            foreach (var childSnapshot in e2.Snapshot.Children)
                            {
                                if (childSnapshot.Child("user") == null || childSnapshot.Child("user").Value == null)
                                {
                                    if (DEBUG) Debug.LogError("Bad data in sample.  Did you forget to call SetEditorDatabaseUrl with your project id?");
                                    SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASELOGIN_PROFILE_RECORD, DELAY_TO_REPORT_EVENT, false);
                                }
                                else
                                {
                                    if (DEBUG) Debug.LogError("FirebaseLoginUserCMD::SearchProfileLogin::PROFILE FOR USER ["+_userID+"] FOUND RECORD!!!!!");
                                    string packetProfile = ProfileModel.FormatPacketProfile(yourvrexperience.Utils.Utilities.CastAsLong(childSnapshot.Child("user").Value),
                                                                                    (long)-1,
                                                                                    childSnapshot.Child("name").Value.ToString(),
                                                                                    childSnapshot.Child("address").Value.ToString(),
                                                                                    childSnapshot.Child("description").Value.ToString(),                                                                                    
                                                                                    childSnapshot.Child("data").Value.ToString());
                                    _dataFormatUser[2] = packetProfile;
                                    SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASELOGIN_PROFILE_RECORD, DELAY_TO_REPORT_EVENT, true);
                                }
                            }
                        }
                        else
                        {
                            if (DEBUG) Debug.LogError("FirebaseLoginUserCMD::SearchProfileLogin::NO RECORD FOUND FOR [" + _userID + "]");
                            SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASELOGIN_PROFILE_RECORD, DELAY_TO_REPORT_EVENT, false);
                        }
                    };
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent == FirebaseController.EVENT_FIREBASE_AUTH_USER_SIGNIN_RESULT)
            {
                if ((bool)parameters[0])
                {
                    SearchMailLogin();
                }
                else
                {
                    SystemEventController.Instance.DispatchSystemEvent(_outputEvent, false);
                }
            }
            if (nameEvent == EVENT_FIREBASELOGIN_USER_RECORD)
            {
                if (_requestProfile)
                {
                    SearchProfileLogin();
                }
                else
                {
                    SystemEventController.Instance.DispatchSystemEvent(_outputEvent, true, _dataFormatUser);
                }
            }
            if (nameEvent == EVENT_FIREBASELOGIN_PROFILE_RECORD)
            {
                SystemEventController.Instance.DispatchSystemEvent(_outputEvent, true, _dataFormatUser);
            }
            if (nameEvent == _outputEvent)
            {
                SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASELOGIN_DELAYED_DESTROY, 2);
            }
            if (nameEvent == EVENT_FIREBASELOGIN_DELAYED_DESTROY)
            {
                GameObject.Destroy(this.gameObject);
            }
        }
    }
}
#endif
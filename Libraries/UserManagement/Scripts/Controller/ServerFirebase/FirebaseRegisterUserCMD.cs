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
    public class FirebaseRegisterUserCMD : MonoBehaviour
    {
        public const bool DEBUG = false;

        public const float DELAY_TO_REPORT_EVENT = 0.5f;

        public const string EVENT_FIREBASE_REGISTER_CONFIRMED_USER      = "EVENT_FIREBASE_REGISTER_CONFIRMED_USER";
        public const string EVENT_FIREBASE_REGISTER_CONFIRMED_PROFILE   = "EVENT_FIREBASE_REGISTER_CONFIRMED_PROFILE";
        public const string EVENT_FIREBASE_REGISTER_RESULT_ERROR        = "EVENT_FIREBASE_REGISTER_RESULT_ERROR";
        public const string EVENT_FIREBASE_REGISTER_DELAYED_DESTROY     = "EVENT_FIREBASE_REGISTER_DELAYED_DESTROY";

        protected LoginPlatforms _loginPlatform;
        protected long _id = -1;
        protected string _email;
        protected string _password;
        protected string _nickname;
        private string _platform;
        protected long _registerdate;
        protected long _lastlogin;
        protected int _admin;
        protected string _code;
        protected int _validated;
        protected string _ip;

        protected bool _confirmedUser = false;
        protected bool _confirmedProfile = false;

        private Firebase.Auth.FirebaseUser _newUserCreated;
        
        public static void CommandFirebaseRegisterUser(params object[] parameters)
        {
            GameObject command = new GameObject();
            command.AddComponent<FirebaseRegisterUserCMD>().Initialize(parameters);
            command.name = "FirebaseRegisterUserCMD";
        }
         
        public virtual void Initialize(params object[] _list)
        {
            SystemEventController.Instance.Event += OnSystemEvent;

            if (DEBUG) Debug.LogError("UserManagement::CreateNewUser::Checking existing user...");

            _loginPlatform = (LoginPlatforms)_list[0];
            _id = (long)_list[1];
            _email = (string)_list[2];
            _password = (string)_list[3];
            _nickname = (string)_list[4];
            _platform = (string)_list[5];
            _registerdate = yourvrexperience.Utils.Utilities.GetTimestamp();
            _lastlogin = yourvrexperience.Utils.Utilities.GetTimestamp();
            _code = yourvrexperience.Utils.Utilities.RandomCodeIV(6);
            _validated = 1;
            _ip = IpManager.GetIP(ADDRESSFAM.IPv4) + "|" + IpManager.GetIP(ADDRESSFAM.IPv6);

            FirebaseController.Instance.SearchEmailInUserTable(_email);
        }

        public void OnDestroy()
        {
            Destroy();
        }

        public void Destroy()
        {
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
        }

        protected void CreateNewUserReal()
        {
            if (DEBUG) Debug.LogError("UserManagement::CreateNewUserReal::Running Transaction...");
            DatabaseReference reference = FirebaseDatabase.DefaultInstance.GetReference(FirebaseController.TABLE_USERS);
            reference.RunTransaction(AddNewUserTransaction)
              .ContinueWith(task => {
                  if (task.Exception != null)
                  {
                      if (DEBUG) Debug.LogError("UserManagement::CreateNewUserReal::EXCEPTION=" + task.Exception.Message);
                      SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_REGISTER_RESULT_ERROR, DELAY_TO_REPORT_EVENT);
                  }
                  else if (task.IsCompleted)
                  {
                      SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_REGISTER_CONFIRMED_USER, DELAY_TO_REPORT_EVENT, true);
                  }
              });
        }

        protected TransactionResult AddNewUserTransaction(MutableData mutableData)
        {
            List<object> myUsers = mutableData.Value as List<object>;

            if (myUsers == null)
            {
                myUsers = new List<object>();
            }

            Dictionary<string, object> newUser = new Dictionary<string, object>();
            newUser["id"] = _id;
            newUser["email"] = _email;
            newUser["password"] = _password;
            newUser["nickname"] = _nickname;
            newUser["platform"] = _platform;
            newUser["registerdate"] = _registerdate;
            newUser["lastlogin"] = _lastlogin;
            newUser["code"] = _code;
            newUser["validated"] = _validated;
            newUser["admin"] = _admin;
            newUser["ip"] = _ip;
            myUsers.Add(newUser);

            mutableData.Value = myUsers;
            return TransactionResult.Success(mutableData);
        }

        protected void CreateNewProfileReal()
        {
            if (DEBUG) Debug.LogError("UserManagement::CreateNewProfileReal::Running Transaction...");
            DatabaseReference reference = FirebaseDatabase.DefaultInstance.GetReference(FirebaseController.TABLE_PROFILES);
            reference.RunTransaction(AddNewUserProfile)
              .ContinueWith(task => {
                  if (task.Exception != null)
                  {
                      if (DEBUG) Debug.LogError("UserManagement::CreateNewProfileReal::EXCEPTION=" + task.Exception.Message);
                      SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_REGISTER_RESULT_ERROR, DELAY_TO_REPORT_EVENT);
                  }
                  else if (task.IsCompleted)
                  {
                      SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_REGISTER_CONFIRMED_PROFILE, DELAY_TO_REPORT_EVENT, true);
                  }
              });
        }

        protected TransactionResult AddNewUserProfile(MutableData mutableData)
        {
            List<object> myProfiles = mutableData.Value as List<object>;

            if (myProfiles == null)
            {
                myProfiles = new List<object>();
            }

            Dictionary<string, object> newProfile = new Dictionary<string, object>();
            newProfile["user"] = _id;
            newProfile["name"] = _nickname;
            newProfile["address"] = "undefined";
            newProfile["description"] = "Write some words about you...";
            newProfile["data"] = "Extra data...";
            myProfiles.Add(newProfile);

            mutableData.Value = myProfiles;
            return TransactionResult.Success(mutableData);
        }

        protected virtual void CheckConfirmationCreation(bool _force = false)
        {
            if ((_confirmedUser && _confirmedProfile) || _force)
            {
                string[] dataFormatted = new string[2];
                dataFormatted[0] = "true";
                dataFormatted[1] = _id.ToString();
                SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_REGISTER_RESULT, true, dataFormatted);
            }
        }

        protected virtual void OnSystemEvent(string _nameEvent, object[] _list)
        {
            if (_nameEvent == FirebaseController.EVENT_FIREBASE_EMAIL_FOUND)
            {
                if (!(bool)_list[0])
                {
                    // FirebaseController.Instance.GetNewUserID();
                    FirebaseController.Instance.SignInAuthenticatedUser(_email, _password, FirebaseController.EVENT_FIREBASE_AUTH_USER_SIGNIN_RESULT);
                }
                else
                {
                    // SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_REGISTER_RESULT, false);
                    SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_REGISTER_DELAYED_DESTROY, 0.01f);
                    string emailFound = (string)_list[2];
                    string passwordFound = (string)_list[3];
                    if (DEBUG) Debug.LogError("======EMAIL EXIST, NOW LOGIN========EMAIL["+emailFound+"]::PASSWORD["+passwordFound+"]::PLATFORM["+_loginPlatform.ToString()+"]::TOKEN["+UsersController.Instance.CurrentAccessToken+"]");
                    UIEventController.Instance.DelayUIEvent(UsersController.EVENT_USER_LOGIN_REQUEST, DELAY_TO_REPORT_EVENT, emailFound, passwordFound, _loginPlatform, UsersController.Instance.CurrentAccessToken);
                }
            }
            if (_nameEvent == FirebaseController.EVENT_FIREBASE_AUTH_USER_SIGNIN_RESULT)
            {
                if ((bool)_list[0])
                {
                    FirebaseController.Instance.GetNewUserID();
                }
                else
                {
                    switch (_loginPlatform)
                    {
                        case LoginPlatforms.Email:                                                    
                            FirebaseController.Instance.CreateAuthenticatedUser(_email, _password, FirebaseController.EVENT_FIREBASE_AUTH_USER_REGISTER_RESULT);
                            break;

                        case LoginPlatforms.Facebook:
                            FirebaseController.Instance.CreateFacebookAuthenticatedUser(_platform, FirebaseController.EVENT_FIREBASE_AUTH_USER_REGISTER_RESULT);
                            break;

                        case LoginPlatforms.Google:
                            FirebaseController.Instance.CreateGoogleAuthenticatedUser(_platform, FirebaseController.EVENT_FIREBASE_AUTH_USER_REGISTER_RESULT);
                            break;

                        case LoginPlatforms.Apple:
                            FirebaseController.Instance.CreateAppleAuthenticatedUser(_platform, FirebaseController.EVENT_FIREBASE_AUTH_USER_REGISTER_RESULT);
                            break;
                    }                          
                }
            }
            if (_nameEvent == FirebaseController.EVENT_FIREBASE_AUTH_USER_REGISTER_RESULT)
            {
                if ((bool)_list[0])
                {
                    _newUserCreated = (Firebase.Auth.FirebaseUser)_list[1];
                    FirebaseController.Instance.SendVerificationEmailAuthenticatedUser(_newUserCreated);
                }
                else
                {
                    SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_REGISTER_RESULT, false);
                }
            }
            if (_nameEvent == FirebaseController.EVENT_FIREBASE_AUTH_USER_EMAIL_VERIFICATION)
            {
                if ((bool)_list[0])
                {
                    FirebaseController.Instance.GetNewUserID();
                }
                else
                {
                    SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_REGISTER_RESULT, false);
                }
            }
            if (_nameEvent == FirebaseController.EVENT_FIREBASE_MAX_USER_ID)
            {
                _id = (long)_list[0];
                CreateNewUserReal();
            }
            if (_nameEvent == EVENT_FIREBASE_REGISTER_CONFIRMED_USER)
            {
                UserModel.SaveLocalEmailLogin(_newUserCreated.Email, _password, _loginPlatform);
                switch (_loginPlatform)
                {
                    case LoginPlatforms.Email:                         
                        break;

                    case LoginPlatforms.Facebook:
                        break;

                    case LoginPlatforms.Google:
                        UsersController.Instance.CurrentRegisterEmail = _newUserCreated.Email;
                        FirebaseUpdateEmailCMD.CommandFirebaseUpdateEmail(_email, _newUserCreated.Email);
                        break;

                    case LoginPlatforms.Apple:
                        break;
                }
                _confirmedUser = true;
                CreateNewProfileReal();
            }
            if (_nameEvent == EVENT_FIREBASE_REGISTER_CONFIRMED_PROFILE)
            {
                _confirmedProfile = true;
                CheckConfirmationCreation();
            }
            if (_nameEvent == EVENT_FIREBASE_REGISTER_RESULT_ERROR)
            {
                SystemEventController.Instance.DelaySystemEvent(UsersController.EVENT_USER_REGISTER_RESULT, 1, false);
            }
            if (_nameEvent == UsersController.EVENT_USER_REGISTER_RESULT)
            {
                SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_REGISTER_DELAYED_DESTROY, 2);
            }
            if (_nameEvent == EVENT_FIREBASE_REGISTER_DELAYED_DESTROY)
            {
                GameObject.Destroy(this.gameObject);
            }            
        }
    }
}
#endif
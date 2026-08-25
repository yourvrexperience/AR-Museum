#if ENABLE_FIREBASE
using Firebase;
using Firebase.Auth;
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
    public class FirebaseController : MonoBehaviour
    {
        private const bool DEBUG = false;

        private const float DELAY_TO_REPORT_EVENT = 0.5f;

        public const string EVENT_FIREBASE_EMAIL_FOUND = "EVENT_FIREBASE_EMAIL_FOUND";
        public const string EVENT_FIREBASE_MAX_USER_ID = "EVENT_FIREBASE_MAX_USER_ID";
        public const string EVENT_FIREBASE_FACEBOOK_FOUND = "EVENT_FIREBASE_FACEBOOK_FOUND";

        public const string EVENT_FIREBASE_AUTH_USER_REGISTER_RESULT = "EVENT_FIREBASE_AUTH_USER_REGISTER_RESULT";
        public const string EVENT_FIREBASE_AUTH_USER_DELETED_RESULT = "EVENT_FIREBASE_AUTH_USER_DELETED_RESULT";        
        public const string EVENT_FIREBASE_AUTH_USER_SIGNIN_RESULT = "EVENT_FIREBASE_AUTH_USER_SIGNIN_RESULT";
        public const string EVENT_FIREBASE_AUTH_USER_EMAIL_VERIFICATION = "EVENT_FIREBASE_AUTH_USER_EMAIL_VERIFICATION";

        public const string FIREBASE_URL_PATH = "https://yourowndomain8765434567.firebaseio.com/";
        public const string FIREBASE_PROJECT_ID = "yourowndomain8765434567";

        public const string TABLE_USERS = "users";
        public const string TABLE_PROFILES = "profiles";
        public const string TABLE_INPUTFORMS = "inputforms";

        // ----------------------------------------------
        // SINGLETON
        // ----------------------------------------------	
        private static FirebaseController _instance;

        public static FirebaseController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(FirebaseController)) as FirebaseController;
                }
                return _instance;
            }
        }

        [SerializeField] private string googleIdToken;
        [SerializeField] private string googleAccessToken;

        private Firebase.FirebaseApp _app;

        public void Initialize()
        {
            if (DEBUG) Debug.LogError("FirebaseController::CALL INITIALITZATION");
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
                var dependencyStatus = task.Result;
                if (dependencyStatus == Firebase.DependencyStatus.Available)
                {
                    // Create and hold a reference to your FirebaseApp, i.e.
                    _app = Firebase.FirebaseApp.DefaultInstance;

                    if (DEBUG) Debug.LogError("FirebaseController::INIT SUCCESS");

                    // where app is a Firebase.FirebaseApp property of your application class.
                    SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_CONFIGURATION_DATA_RECEIVED, true);
                }
                else
                {
                    if (DEBUG) Debug.LogError("FirebaseController::INIT FAILURE");

                    // Firebase Unity SDK is not safe to use here.
                    SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_CONFIGURATION_DATA_RECEIVED, false);
                }
            });
        }

        void OnDestroy()
        {
            Destroy();
        }

        public void Destroy()
        {
            if (_instance != null)
            {
                Destroy(_instance);
                _instance = null;
            }
        }

        public void CreateNewUser(string email, string password, LoginPlatforms loginPlatform, string dataPlatform)
        {
            FirebaseRegisterUserCMD.CommandFirebaseRegisterUser(loginPlatform, (long)-1, email, password, email.Substring(0, email.IndexOf('@')), dataPlatform);
        }

        public void LoginUser(LoginPlatforms loginPlatform, string email, string password, string accessToken)
        {
            FirebaseLoginUserCMD.CommandFirebaseLoginUser(loginPlatform, accessToken, email, password, UsersController.EVENT_USER_LOGIN_RESULT, true);
        }

        public void ConsultSingleUserByEmail(LoginPlatforms loginPlatform, string email)
        {
            FirebaseLoginUserCMD.CommandFirebaseLoginUser(loginPlatform, email, "", UsersController.EVENT_USER_RESULT_CONSULT_SINGLE_RECORD, true, true);
        }

        public void CommandFirebaseRemoveUser(LoginPlatforms loginPlatform, long userID, string accessToken)
        {
            FirebaseRemoveUserCMD.CommandFirebaseRemoveUser(userID, loginPlatform, accessToken);
        }

        public void UpdateProfile(LoginPlatforms loginPlatform, string accessToken, string userProfile, string nameProfile, string addressProfile, string descriptionProfile, string dataProfile)
        {
            FirebaseUpdateProfileCMD.CommandFirebaseUpdateProfile(loginPlatform, accessToken, long.Parse(userProfile), nameProfile, addressProfile, descriptionProfile, dataProfile);
        }

        public void InputForm(string email, string jsonFormData)
        {
            FirebaseInputFormCMD.CommandFirebaseInputForm(email, jsonFormData);
        }

        public void SearchEmailInUserTable(string email)
        {
            if (DEBUG) Debug.LogError("FirebaseController::SearchEmail::LOOKING FOR [" + email + "]!!!!!");

            FirebaseDatabase.DefaultInstance
                    .GetReference(FirebaseController.TABLE_USERS).OrderByChild("email").EqualTo(email).LimitToFirst(2)
                    .GetValueAsync().ContinueWith(x => {
                        bool hasBeenFound = false;
                        long idFound = -1;
                        string email = "";
                        string password = "";
                        if (x.Result != null && x.Result.ChildrenCount > 0)
                        {
                            foreach (var childSnapshot in x.Result.Children)
                            {
                                if (DEBUG) Debug.LogError("FirebaseController::SearchEmail::FOUND EMAIL [" + email + "]");
                                if (!hasBeenFound)
                                {
                                    hasBeenFound = true;
                                    idFound = yourvrexperience.Utils.Utilities.CastAsLong(childSnapshot.Child("id").Value);
                                    email = childSnapshot.Child("email").Value.ToString();
                                    password = childSnapshot.Child("password").Value.ToString();
                                }
                            }
                        }
                        else
                        {
                            if (DEBUG) Debug.LogError("FirebaseController::LoginUser::NO EMAIL FOUND FOR [" + email + "]");
                            hasBeenFound = false;                            
                        }
                        
                        if (hasBeenFound)
                        {
                            SystemEventController.Instance.DispatchSystemEvent(EVENT_FIREBASE_EMAIL_FOUND, true, idFound, email, password);
                        }
                        else
                        {
                            SystemEventController.Instance.DispatchSystemEvent(EVENT_FIREBASE_EMAIL_FOUND, false);
                        }                                                        
                    });
        }

        public void GetNewUserID()
        {
            if (DEBUG) Debug.LogError("FirebaseController::GetNewUserID::GETTING THE MAXIMUM ID!!!!!");

            FirebaseDatabase.DefaultInstance
                    .GetReference(FirebaseController.TABLE_USERS).OrderByChild("id").LimitToLast(2)
                    .GetValueAsync().ContinueWith(x => {
                        long newIDUser = 0;
                        if (x.Result != null && x.Result.ChildrenCount > 0)
                        {                            
                            foreach (var childSnapshot in x.Result.Children)
                            {
                                long tempIDUser = yourvrexperience.Utils.Utilities.CastAsLong(childSnapshot.Child("id").Value) + 1;
                                if (tempIDUser > newIDUser)
                                {
                                    newIDUser = tempIDUser;
                                }
                            }
                            if (DEBUG) Debug.LogError("FirebaseController::GetNewUserID::NEW ID[" + newIDUser + "]");
                        }
                        else
                        {
                            if (DEBUG) Debug.LogError("FirebaseController::GetNewUserID::NO RECORDS FOUDN, JUST START");
                        }
                        SystemEventController.Instance.DispatchSystemEvent(EVENT_FIREBASE_MAX_USER_ID, newIDUser);
                    });
        }

        public void ConsultAllUsers()
        {
            if (DEBUG) Debug.LogError("FirebaseController::ConsultAllUsers::CONSULT ALL USERS!!!!");

            FirebaseDatabase.DefaultInstance
                    .GetReference(FirebaseController.TABLE_USERS).OrderByChild("id")
                    .GetValueAsync().ContinueWith(x => {
                        List<string[]> usersRecords = new List<string[]>();
                        if (x.Result != null && x.Result.ChildrenCount > 0)
                        {
                            if (DEBUG) Debug.LogError("FirebaseController::ConsultAllUsers::TOTAL NUMBER OF CHILDREN IN THE DATABSE [" + x.Result.ChildrenCount + "]");
                            foreach (var childSnapshot in x.Result.Children)
                            {
                                string[] userConsult = new string[1];
                                userConsult[0] = FirebaseLoginUserCMD.ParseSnapshotUser(childSnapshot);
                                usersRecords.Add(userConsult);
                            }
                        }
                        else
                        {
                            if (DEBUG) Debug.LogError("FirebaseController::ConsultAllUsers::NO RECORDS FOUND");
                        }
                        if (usersRecords.Count > 0)
                        {
                            SystemEventController.Instance.DelaySystemEvent(UsersController.EVENT_USER_RESULT_CONSULT_ALL_RECORDS, 0.1f, true, usersRecords);
                        }
                        else
                        {
                            SystemEventController.Instance.DelaySystemEvent(UsersController.EVENT_USER_RESULT_CONSULT_ALL_RECORDS, 0.1f, false);
                        }
                    });
        }


        public void CreateAuthenticatedUser(string email, string password, string outputEvent)
        {
            if (DEBUG) Debug.LogError("FirebaseController::CreateAuthenticatedUser::Running Transaction...");
            Firebase.Auth.FirebaseAuth.DefaultInstance.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task => {
                if (task.IsCanceled)
                {
                    Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
                    SystemEventController.Instance.DelaySystemEvent(outputEvent, DELAY_TO_REPORT_EVENT, false);
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                    SystemEventController.Instance.DelaySystemEvent(outputEvent,  DELAY_TO_REPORT_EVENT, false);
                    return;
                }

                // Firebase user has been created.
                Firebase.Auth.FirebaseUser newUser = task.Result.User;
                Debug.LogFormat("Firebase user created successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);
                SystemEventController.Instance.DelaySystemEvent(outputEvent, DELAY_TO_REPORT_EVENT, true, newUser);
            });
        }

        public void CreateGoogleAuthenticatedUser(string googleServiceCode, string outputEvent)
        {
            Firebase.Auth.Credential credential = PlayGamesAuthProvider.GetCredential(googleServiceCode);
            Firebase.Auth.FirebaseAuth.DefaultInstance.SignInAndRetrieveDataWithCredentialAsync(credential).ContinueWith(task => {
                if (task.IsCanceled) {
                    if (DEBUG) Debug.LogError("SignInAndRetrieveDataWithCredentialAsync was canceled.");
                    SystemEventController.Instance.DelaySystemEvent(outputEvent, DELAY_TO_REPORT_EVENT, false);
                    return;
                }
                if (task.IsFaulted) {
                    if (DEBUG) Debug.LogError("SignInAndRetrieveDataWithCredentialAsync encountered an error: " + task.Exception);
                    SystemEventController.Instance.DelaySystemEvent(outputEvent, DELAY_TO_REPORT_EVENT, false);
                    return;
                }

                Firebase.Auth.FirebaseUser newUser = task.Result.User;
                if (DEBUG) Debug.LogFormat("Firebase user created successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);
                SystemEventController.Instance.DelaySystemEvent(outputEvent, DELAY_TO_REPORT_EVENT, true, newUser);
            });
        }

        public void CreateFacebookAuthenticatedUser(string facebookAccessToken, string outputEvent)
        {
            if (DEBUG) Debug.LogError("CreateFacebookAuthenticatedUser AccessToken="+facebookAccessToken);
            Firebase.Auth.Credential credential = Firebase.Auth.FacebookAuthProvider.GetCredential(facebookAccessToken);
            Firebase.Auth.FirebaseAuth.DefaultInstance.SignInAndRetrieveDataWithCredentialAsync(credential).ContinueWith(task => {
                if (task.IsCanceled) {
                    if (DEBUG) Debug.LogError("CreateFacebookAuthenticatedUser was canceled.");
                    SystemEventController.Instance.DelaySystemEvent(outputEvent, DELAY_TO_REPORT_EVENT, false);
                    return;
                }
                if (task.IsFaulted) {
                    if (DEBUG) Debug.LogError("CreateFacebookAuthenticatedUser encountered an error: " + task.Exception);
                    SystemEventController.Instance.DelaySystemEvent(outputEvent, DELAY_TO_REPORT_EVENT, false);
                    return;
                }

                Firebase.Auth.FirebaseUser newUser = task.Result.User;
                if (DEBUG) Debug.LogFormat("Firebase user created successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);
                SystemEventController.Instance.DelaySystemEvent(outputEvent, DELAY_TO_REPORT_EVENT, true, newUser);
            });
        }

        public void CreateAppleAuthenticatedUser(string appleIdToken, string outputEvent)
        {
            string rawNonce = UsersController.Instance.CurrentRawNonce;
            Firebase.Auth.Credential credential = Firebase.Auth.OAuthProvider.GetCredential("apple.com", appleIdToken, rawNonce, null);
            Firebase.Auth.FirebaseAuth.DefaultInstance.SignInAndRetrieveDataWithCredentialAsync(credential).ContinueWith(task => {
                if (task.IsCanceled) {
                    if (DEBUG) Debug.LogError("SignInAndRetrieveDataWithCredentialAsync was canceled.");
                    SystemEventController.Instance.DelaySystemEvent(outputEvent, DELAY_TO_REPORT_EVENT, false);
                    return;
                }
                if (task.IsFaulted) {
                    if (DEBUG) Debug.LogError("SignInAndRetrieveDataWithCredentialAsync encountered an error: " + task.Exception);
                    SystemEventController.Instance.DelaySystemEvent(outputEvent, DELAY_TO_REPORT_EVENT, false);
                    return;
                }

                Firebase.Auth.FirebaseUser newUser = task.Result.User;
                if (DEBUG) Debug.LogFormat("Firebase user created successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);
                SystemEventController.Instance.DelaySystemEvent(outputEvent, DELAY_TO_REPORT_EVENT, true, newUser);
            });
        }

        public void RemoveAuthenticatedUser(Firebase.Auth.FirebaseUser user)
        {
            if (DEBUG) Debug.LogError("FirebaseController::CreateAuthenticatedUser::Running Transaction...");
            user.DeleteAsync().ContinueWith(task => {
                if (task.IsCompleted) 
                {                    
                    SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_AUTH_USER_DELETED_RESULT, DELAY_TO_REPORT_EVENT, true);
                }
                else 
                {
                    SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_AUTH_USER_DELETED_RESULT, DELAY_TO_REPORT_EVENT, false);
                }
            });
        }

        public void SignInAuthenticatedUser(string email, string password, string customEvent)
        {
            Firebase.Auth.FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task => {
                if (task.IsCanceled)
                {
                    Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
                    SystemEventController.Instance.DelaySystemEvent(customEvent, DELAY_TO_REPORT_EVENT, false);
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                    SystemEventController.Instance.DelaySystemEvent(customEvent, DELAY_TO_REPORT_EVENT, false);
                    return;
                }

                Firebase.Auth.FirebaseUser newUser = task.Result.User;
                Debug.LogFormat("User signed in successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);
                SystemEventController.Instance.DelaySystemEvent(customEvent, DELAY_TO_REPORT_EVENT, true, newUser);
            });
        }

        public void SendVerificationEmailAuthenticatedUser(Firebase.Auth.FirebaseUser user)
        {
            if (user != null)
            {
                user.SendEmailVerificationAsync().ContinueWith(task => {
                    if (task.IsCanceled)
                    {
                        Debug.LogError("SendEmailVerificationAsync was canceled.");
                        SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_AUTH_USER_EMAIL_VERIFICATION, 0.1f, false);
                        return;
                    }
                    if (task.IsFaulted)
                    {
                        Debug.LogError("SendEmailVerificationAsync encountered an error: " + task.Exception);
                        SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_AUTH_USER_EMAIL_VERIFICATION, 0.1f, false);
                        return;
                    }

                    Debug.Log("Email sent successfully.");
                    SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_AUTH_USER_EMAIL_VERIFICATION, 0.1f, true);
                });
            }
        }

        public void SendResetPasswordEmail(string emailAddress)
        {
            Firebase.Auth.FirebaseAuth.DefaultInstance.SendPasswordResetEmailAsync(emailAddress).ContinueWith(task => {
                if (task.IsCanceled)
                {
                    Debug.LogError("SendPasswordResetEmailAsync was canceled.");
                    SystemEventController.Instance.DelaySystemEvent(UsersController.EVENT_USER_RESPONSE_RESET_PASSWORD, 0.1f, false);
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("SendPasswordResetEmailAsync encountered an error: " + task.Exception);
                    SystemEventController.Instance.DelaySystemEvent(UsersController.EVENT_USER_RESPONSE_RESET_PASSWORD, 0.1f, false);
                    return;
                }

                Debug.Log("Password reset email sent successfully.");
                SystemEventController.Instance.DelaySystemEvent(UsersController.EVENT_USER_RESPONSE_RESET_PASSWORD, 0.1f, true);
            });
        }
    }
}
#endif
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using yourvrexperience.Utils;
using static yourvrexperience.Utils.AESEncryption;
#if ENABLE_GOOGLE || ENABLE_FACEBOOK || ENABLE_GOOGLE_SIGNIN
using yourvrexperience.Social;
#endif

namespace yourvrexperience.UserManagement
{
    public enum LoginPlatforms { Email = 0, Facebook, Google, Apple };

    public class UsersController : MonoBehaviour
    {
        public const string GOOGLE_EMAIL_PLACEHOLDER      = "google@google.com";
        public const string FACEBOOK_EMAIL_PLACEHOLDER      = "facebook@facebook.com";

        public const string EVENT_CONFIGURATION_DATA_REQUESTED      = "EVENT_CONFIGURATION_DATA_REQUESTED";
        public const string EVENT_CONFIGURATION_DATA_RECEIVED       = "EVENT_CONFIGURATION_DATA_RECEIVED";

        public const string EVENT_USER_LOGIN_REQUEST                = "EVENT_USER_LOGIN_REQUEST";
        public const string EVENT_USER_LOGIN_RESULT                 = "EVENT_USER_LOGIN_RESULT";
        public const string EVENT_USER_LOGIN_FORMATTED              = "EVENT_USER_LOGIN_FORMATTED";

        public const string EVENT_USER_REGISTER_REQUEST             = "EVENT_USER_REGISTER_REQUEST";
        public const string EVENT_USER_SOCIAL_LOGIN_REQUEST         = "EVENT_USER_SOCIAL_LOGIN_REQUEST";
        public const string EVENT_USER_REGISTER_RESULT              = "EVENT_USER_REGISTER_RESULT";
        public const string EVENT_USER_REGISTER_CONFIRMATION        = "EVENT_USER_REGISTER_CONFIRMATION";

        public const string EVENT_USER_UPDATE_PROFILE_REQUEST       = "EVENT_USER_UPDATE_PROFILE_REQUEST";
        public const string EVENT_USER_UPDATE_PROFILE_RESULT        = "EVENT_USER_UPDATE_PROFILE_RESULT";

        public const string EVENT_USER_UPDATE_PROFILE_DATA_REQUEST = "EVENT_USER_UPDATE_PROFILE_DATA_REQUEST";

        public const string EVENT_USER_CHECK_VALIDATION_RESULT      = "EVENT_USER_CHECK_VALIDATION_RESULT";
        public const string EVENT_USER_CHECK_VALIDATION_CONFIRMATION= "EVENT_USER_CHECK_VALIDATION_CONFIRMATION";

        public const string EVENT_USER_CALL_CONSULT_SINGLE_RECORD   = "EVENT_USER_CALL_CONSULT_SINGLE_RECORD";
        public const string EVENT_USER_RESULT_CONSULT_SINGLE_RECORD = "EVENT_USER_RESULT_CONSULT_SINGLE_RECORD";
        public const string EVENT_USER_RESULT_FORMATTED_SINGLE_RECORD = "EVENT_USER_RESULT_FORMATTED_SINGLE_RECORD";

        public const string EVENT_USER_CALL_CONSULT_ALL_RECORDS     = "EVENT_USER_CALL_CONSULT_ALL_RECORDS";
        public const string EVENT_USER_RESULT_CONSULT_ALL_RECORDS   = "EVENT_USER_RESULT_CONSULT_ALL_RECORDS";
        public const string EVENT_USER_RESULT_FORMATTED_ALL_RECORDS = "EVENT_USER_RESULT_FORMATTED_ALL_RECORDS";

        public const string EVENT_USER_REMOVE_SINGLE_RECORD         = "EVENT_USER_REMOVE_SINGLE_RECORD";
        public const string EVENT_USER_CONFIRMATION_REMOVED_RECORD  = "EVENT_USER_CONFIRMATION_REMOVED_RECORD";

        public const string EVENT_USER_REQUEST_RESET_PASSWORD = "EVENT_USER_REQUEST_RESET_PASSWORD";
        public const string EVENT_USER_RESPONSE_RESET_PASSWORD = "EVENT_USER_RESPONSE_RESET_PASSWORD";
        public const string EVENT_USER_RESET_LOCAL_DATA         = "EVENT_USER_RESET_LOCAL_DATA";

        public const string EVENT_USER_INPUT_FORM_REQUEST       = "EVENT_USER_INPUT_FORM_REQUEST";
        public const string EVENT_USER_INPUT_FORM_RESULT        = "EVENT_USER_INPUT_FORM_RESULT";
        public const string EVENT_USER_INPUT_FORM_FORMATTED        = "EVENT_USER_INPUT_FORM_FORMATTED";

        private static UsersController _instance;

        public static UsersController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(UsersController)) as UsersController;
                }
                return _instance;
            }
        }

        protected UserModel _currentUser;
        protected Dictionary<long, UserModel> _users = new Dictionary<long, UserModel>();
        protected bool _reloadUsers = true;

        protected string _emailCustomerService;
        protected bool _isServiceEnabled;
        
        private string _currentRegisterEmail;
        private string _currentRegisterPassword;
        private LoginPlatforms _currentRegisterPlatform;
        private string _currentAccessToken = "";
        private string _currentRawNonce = "";
        private bool _loginRequested = false;

        private string _ivString = "";
        private string _aesKeyPassword = "";

        public UserModel CurrentUser
        {
            get { return _currentUser; }
        }
        public string EmailCustomerService
        {
            get { return _emailCustomerService; }
            set { _emailCustomerService = value; }
        }
        public bool IsServiceEnabled
        {
            get { return _isServiceEnabled; }
            set { _isServiceEnabled = value; }
        }
        public bool ReloadUsers
        {
            set { _reloadUsers = value; }
        }
        public string CurrentRegisterEmail
        {
            get { return _currentRegisterEmail; }
            set { _currentRegisterEmail = value; }
        }
        public string CurrentRegisterPassword
        {
            get { return _currentRegisterPassword; }
            set { _currentRegisterPassword = value; }
        }
        public LoginPlatforms CurrentRegisterPlatform
        {
            get { return _currentRegisterPlatform; }
            set { _currentRegisterPlatform = value; }
        }
        public string CurrentAccessToken
        {
            get { return _currentAccessToken; }
            set { _currentAccessToken = value; }
        }
        public string CurrentRawNonce
        {
            get { return _currentRawNonce; }
        }
        public bool LoginRequested
        {
            get { return _loginRequested; }
            set { _loginRequested = value; }
        }

        public virtual void Initialize(string ivPassword = "", string aesKeyPassword = "")
        {
            if (ivPassword.Length > 0) _ivString = AESEncryption.GenerateIVFromPassword(ivPassword);
            _aesKeyPassword = aesKeyPassword;

            _currentUser = new UserModel("");
            _currentUser.LoadLocalInfo();

            UIEventController.Instance.Event += OnUIEvent;
            SystemEventController.Instance.Event += OnSystemEvent;
        }

        public string EncryptData(string data)
        {
            if (data.Length > 0)
            {
                AESEncryptedText encryptionData = AESEncryption.Encrypt(Encoding.UTF8.GetBytes(data), _aesKeyPassword, Convert.FromBase64String(_ivString));
                return Convert.ToBase64String(encryptionData.EncryptedData);
            }
            else
            {
                return data;
            }
        }

        public string DecryptData(string data)
        {
            if (data.Length > 0)
            {
                byte[] dataDecrypted = AESEncryption.Decrypt(Convert.FromBase64String(data), _ivString, _aesKeyPassword);
                return Encoding.UTF8.GetString(dataDecrypted);
            }
            else
            {
                return data;
            }
        }

        void OnDestroy()
        {
            Destroy();
        }

        public virtual void Destroy()
        {
            if (Instance != null)
            {
                if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
                if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;

                GameObject.Destroy(_instance.gameObject);
                _instance = null;
            }
        }

		public bool LogIn()
		{            
			if (CurrentUser == null)
			{
				return false;
			}
			else
			{
				if ((CurrentUser.Email.Length > 0) && (CurrentUser.Password.Length > 0))
				{
					UserModel.LoginWithStoredLogin();
					return true;
				}
				else
				{
					return false;
				}
			}
		}

        public UserModel GetLocalUser(long idUser)
        {
            if (_users.ContainsKey(idUser))
            {
                return _users[idUser];
            }
            return null;
        }

        private UserModel UpdateLocalUserData(int index, string[] data)
        {
            // PARSE RECEIVED USER DATA
            UserModel requestedUser = new UserModel();
            requestedUser.UpdateAllData(index, data);

            // GET LOCAL USER
            UserModel localUser = GetLocalUser(requestedUser.Id);

            // UPDATE LOCAL USER DATA
            if (localUser != null)
            {
                if (requestedUser.Id == _currentUser.Id)
                {
                    _currentUser.Copy(requestedUser);
                }
                else
                {
                    localUser.Copy(requestedUser);
                }
            }
            else
            {
                _users.Add(requestedUser.Id, requestedUser);
            }

            return requestedUser;
        }

        public void CheckCurrentUserValidated()
        {
            CommsHTTPConstants.Instance.CheckUserValidation(CurrentUser.Id);
        }

        protected virtual void OnUIEvent(string nameEvent, params object[] parameters)
        {
            if (nameEvent == EVENT_CONFIGURATION_DATA_REQUESTED)
            {
#if ENABLE_FIREBASE
                FirebaseController.Instance.Initialize();
#else
                CommsHTTPConstants.Instance.GetServerConfigurationParameters();
#endif
            }
            if (nameEvent == EVENT_USER_LOGIN_REQUEST)
            {
                string email = (string)parameters[0];
                string password = (string)parameters[1];
                LoginPlatforms platform = (LoginPlatforms)parameters[2];
                string accessToken = "ACCESS TOKEN";
                if (parameters.Length > 3) accessToken = (string)parameters[3];
                _currentUser.UpdateBasicInfo(email, password, platform);
#if ENABLE_FIREBASE
                FirebaseController.Instance.LoginUser(platform, email, password, accessToken);
#else
                CommsHTTPConstants.Instance.RequestUserByLogin(email, password);
#endif
            }
            if (nameEvent == EVENT_USER_REGISTER_REQUEST)
            {
                _currentRegisterEmail = (string)parameters[0];
                _currentRegisterPassword = (string)parameters[1];
                _currentRegisterPlatform = (LoginPlatforms)parameters[2];
                string platformData = (string)parameters[3];
#if ENABLE_FIREBASE
                FirebaseController.Instance.CreateNewUser(_currentRegisterEmail, _currentRegisterPassword, _currentRegisterPlatform, platformData);                
#else
                CommsHTTPConstants.Instance.RequestUserRegister(_currentRegisterEmail, _currentRegisterPassword, platformData);
#endif
            }
            if (nameEvent == EVENT_USER_SOCIAL_LOGIN_REQUEST)
            {
                _currentRegisterEmail = (string)parameters[0];
                _currentRegisterPassword = (string)parameters[1];
                _currentRegisterPlatform = (LoginPlatforms)parameters[2];
                string platformData = (string)parameters[3];
                _currentUser.UpdateBasicInfo(_currentRegisterEmail, _currentRegisterPassword, _currentRegisterPlatform);
#if ENABLE_FIREBASE
                FirebaseController.Instance.CreateSocialLogin(_currentRegisterEmail, _currentRegisterPassword, _currentRegisterPlatform, platformData);                
#else
                CommsHTTPConstants.Instance.RequestSocialLogin(_currentRegisterEmail, _currentRegisterPassword, platformData);
#endif
            }
#if ENABLE_GOOGLE && ENABLE_FIREBASE
            if (nameEvent == GoogleController.EVENT_GOOGLE_CONTROLLER_AUTHENTICATED)
            {
                if ((bool)parameters[0])
                {
                    _currentAccessToken = (string)parameters[1];
                    if (!_loginRequested)
                    {
                        _currentRegisterEmail = GOOGLE_EMAIL_PLACEHOLDER + yourvrexperience.Utils.Utilities.GetTimestamp();
                        _currentRegisterPassword = yourvrexperience.Utils.Utilities.RandomCodeGeneration(10);                    
                        _currentRegisterPlatform = LoginPlatforms.Google;                        
                        FirebaseController.Instance.CreateNewUser(_currentRegisterEmail, _currentRegisterPassword, _currentRegisterPlatform, _currentAccessToken);                
                    }
                    else
                    {
                        _loginRequested = false;
                        FirebaseController.Instance.LoginUser(LoginPlatforms.Google, _currentUser.Email, _currentUser.Password, _currentAccessToken);
                    }
                }
            }
#endif            
#if ENABLE_FACEBOOK && ENABLE_FIREBASE
            if (nameEvent == FacebookController.EVENT_FACEBOOK_COMPLETE_INITIALITZATION)
            {
                _currentAccessToken = (string)parameters[3];
                if (!_loginRequested)
                {
                    _currentRegisterEmail = (string)parameters[2];
                    _currentRegisterPassword = yourvrexperience.Utils.Utilities.RandomCodeGeneration(10);
                    _currentRegisterPlatform = LoginPlatforms.Facebook;
                    FirebaseController.Instance.CreateNewUser(_currentRegisterEmail, _currentRegisterPassword, _currentRegisterPlatform, _currentAccessToken);
                }
                else
                {
                    _loginRequested = false;
                    FirebaseController.Instance.LoginUser(LoginPlatforms.Facebook, _currentUser.Email, _currentUser.Password, _currentAccessToken);
                }
            }          
            if (nameEvent == FacebookController.EVENT_FACEBOOK_CANCELATION)  
            {
                if (!_loginRequested)
                {
                    SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_REGISTER_RESULT, false);
                }
                else
                {
                    SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_LOGIN_RESULT, false);
                }                
            }
#endif
#if ENABLE_APPLE && ENABLE_FIREBASE
            if (nameEvent == AppleController.EVENT_APPLE_CONTROLLER_AUTHENTICATED)
            {
                _currentAccessToken = (string)parameters[3];
                _currentRawNonce = (string)parameters[4];
                if (!_loginRequested)
                {
                    _currentRegisterEmail = (string)parameters[2];
                    _currentRegisterPassword = yourvrexperience.Utils.Utilities.RandomCodeGeneration(10);
                    _currentRegisterPlatform = LoginPlatforms.Apple;
                    FirebaseController.Instance.CreateNewUser(_currentRegisterEmail, _currentRegisterPassword, _currentRegisterPlatform, _currentAccessToken);
                }
                else
                {
                    _loginRequested = false;
                    FirebaseController.Instance.LoginUser(LoginPlatforms.Apple, _currentUser.Email, _currentUser.Password, _currentAccessToken);
                }
            }
            if (nameEvent == AppleController.EVENT_APPLE_CONTROLLER_CANCELATION)  
            {
                if (!_loginRequested)
                {
                    SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_REGISTER_RESULT, false);
                }
                else
                {
                    SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_LOGIN_RESULT, false);
                }                
            }
#endif
            if (nameEvent == EVENT_USER_UPDATE_PROFILE_REQUEST)
            {
                string userProfile = (string)parameters[0];
                string nameProfile = (string)parameters[1];
                string addressProfile = (string)parameters[2];
                string descriptionProfile = (string)parameters[3];
                string dataProfile = (string)parameters[4];
                string dataProfile2 = (string)parameters[5];
                string dataProfile3 = (string)parameters[6];
                string dataProfile4 = (string)parameters[7];
                string dataProfile5 = (string)parameters[8];
#if ENABLE_FIREBASE
                FirebaseController.Instance.UpdateProfile(_currentUser.Platform, _currentAccessToken, userProfile, nameProfile, addressProfile, descriptionProfile, dataProfile, dataProfile2, dataProfile3, dataProfile4, dataProfile5);
#else
                CommsHTTPConstants.Instance.RequestUpdateProfile(_currentUser.Id.ToString(), _currentUser.Password, userProfile, nameProfile, addressProfile, descriptionProfile, dataProfile, dataProfile2, dataProfile3, dataProfile4, dataProfile5);
#endif
            }
            if (nameEvent == EVENT_USER_UPDATE_PROFILE_DATA_REQUEST)
            {
#if ENABLE_FIREBASE
                FirebaseController.Instance.UpdateProfile(_currentUser.Platform, _currentAccessToken, _currentUser.Profile.User.ToString(), _currentUser.Profile.Name, _currentUser.Profile.Address, _currentUser.Profile.Description, _currentUser.Profile.Data, _currentUser.Profile.Data2, _currentUser.Profile.Data3, _currentUser.Profile.Data4, _currentUser.Profile.Data5);
#else
                CommsHTTPConstants.Instance.RequestUpdateProfile(_currentUser.Id.ToString(), _currentUser.Password, _currentUser.Profile.User.ToString(), _currentUser.Profile.Name, _currentUser.Profile.Address, _currentUser.Profile.Description, _currentUser.Profile.Data, _currentUser.Profile.Data2, _currentUser.Profile.Data3, _currentUser.Profile.Data4, _currentUser.Profile.Data5);
#endif
            }
            if (nameEvent == EVENT_USER_CALL_CONSULT_SINGLE_RECORD)
            {
                long idUserSearch = (long)parameters[0];
                UserModel user = GetLocalUser(idUserSearch);
                if ((user != null) && (user.IsAllDataLoaded()))
                {
                    SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_RESULT_FORMATTED_SINGLE_RECORD, user);
                }
                else
                {
#if ENABLE_FIREBASE
                    if (user == null)
                    {

                    }
                    else
                    {
                        FirebaseController.Instance.ConsultSingleUserByEmail(user.Platform, user.Email);
                    }                    
#else
                CommsHTTPConstants.Instance.RequestConsultUser(_currentUser.Id, _currentUser.Password, idUserSearch);
#endif
                }
            }
            if (nameEvent == EVENT_USER_CALL_CONSULT_ALL_RECORDS)
            {
                if ((_users.Count > 0) && (!_reloadUsers))
                {
                    SystemEventController.Instance.DispatchSystemEvent(EVENT_USER_RESULT_FORMATTED_ALL_RECORDS, true, _users);
                }
                else
                {
                    _reloadUsers = false;
#if ENABLE_FIREBASE
                    FirebaseController.Instance.ConsultAllUsers();
#else
                CommsHTTPConstants.Instance.RequestConsultAllUsers(_currentUser.Id, _currentUser.Password);
#endif
                }
            }
            if (nameEvent == EVENT_USER_REMOVE_SINGLE_RECORD)
            {
                long targetUserID = (long)parameters[0];
                if (_currentUser.Admin || (_currentUser.Id == targetUserID))
                {                    
#if ENABLE_FIREBASE
                    FirebaseController.Instance.CommandFirebaseRemoveUser(_currentUser.Platform, targetUserID, _currentAccessToken);
#else
                    CommsHTTPConstants.Instance.RequestRemoveSingleUser(_currentUser.Id, _currentUser.Password, (long)parameters[0]);
#endif
                }
                else
                {
                    SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_CONFIRMATION_REMOVED_RECORD, false);
                }
            }
            if (nameEvent == EVENT_USER_REQUEST_RESET_PASSWORD)
            {
#if ENABLE_FIREBASE
                FirebaseController.Instance.SendResetPasswordEmail((string)parameters[0]);
#else
                CommsHTTPConstants.Instance.RequestResetPasswordByEmail((string)parameters[0]);
#endif
            }
            if (nameEvent == EVENT_USER_INPUT_FORM_REQUEST)
            {
                string jsonFormData = (string)parameters[0];
                string emailForm = "Anonymous";
                if (_currentUser != null)
                {
                    if (_currentUser.Email.Length > 0)
                    {
                        emailForm = "Anonymous";
                    }
                }
#if ENABLE_FIREBASE
                FirebaseController.Instance.InputForm(emailForm, jsonFormData);
#else
                CommsHTTPConstants.Instance.RequestInputForm(emailForm, jsonFormData);
#endif
            }
        }

        protected virtual void OnSystemEvent(string _nameEvent, object[] _list)
        {            
            if (_nameEvent == EVENT_USER_LOGIN_RESULT)
            {
                if ((bool)_list[0])
                {
                    yourvrexperience.Utils.Utilities.DebugLogError("UsersController::LOGIN SUCCESS");
                    _currentUser.UpdateAllData(1, (string[])_list[1]);
                    if (GetLocalUser(_currentUser.Id) == null)
                    {
                        _users.Add(_currentUser.Id, _currentUser);
                    }
                }
                else
                {
                    Debug.Log("UsersController::LOGIN ERROR");
                }                                
                SystemEventController.Instance.DispatchSystemEvent(EVENT_USER_LOGIN_FORMATTED, (bool)_list[0], _currentUser);
            }
            if (_nameEvent == EVENT_USER_REGISTER_RESULT)
            {
                if ((bool)_list[0])
                {
                    yourvrexperience.Utils.Utilities.DebugLogError("UsersController::REGISTER EMAIL SUCCESS");
                    _currentUser.UpdateAllData(1, (string[])_list[1]);
                    _reloadUsers = true;
                }
                else
                {
                    Debug.LogError("UsersController::REGISTER EMAIL ERROR");
                }
                SystemEventController.Instance.DispatchSystemEvent(EVENT_USER_REGISTER_CONFIRMATION, (bool)_list[0]);
            }
            if (_nameEvent == EVENT_USER_UPDATE_PROFILE_RESULT)
            {
                if ((bool)_list[0])
                {
                    string[] dataArray = (string[])_list[1];
                    if (dataArray != null)
                    {
                        // UPDATE LOCAL DATA USER
                        UserModel requestedUser = UpdateLocalUserData(1, (string[])_list[1]);

                        SystemEventController.Instance.DispatchSystemEvent(EVENT_USER_RESULT_FORMATTED_SINGLE_RECORD, requestedUser);
                    }
                }
                else
                {
                    Debug.Log("UsersController::UPDATE PROFILE ERROR");
                    SystemEventController.Instance.DispatchSystemEvent(EVENT_USER_RESULT_FORMATTED_SINGLE_RECORD);
                }
            }
            if (_nameEvent == EVENT_USER_CHECK_VALIDATION_RESULT)
            {
                if ((bool)_list[0])
                {
                    CurrentUser.Validated = true;
                }                
                SystemEventController.Instance.DispatchSystemEvent(EVENT_USER_CHECK_VALIDATION_CONFIRMATION);
            }
            if (_nameEvent == UsersController.EVENT_USER_RESULT_CONSULT_SINGLE_RECORD)
            {
                if (_list == null)
                {
                    SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_RESULT_FORMATTED_SINGLE_RECORD);
                    return;
                }
                if (_list.Length == 0)
                {
                    SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_RESULT_FORMATTED_SINGLE_RECORD);
                    return;
                }
                if (!(bool)_list[0])
                {
                    SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_RESULT_FORMATTED_SINGLE_RECORD);
                    return;
                }

                // UPDATE LOCAL DATA USER
                UserModel requestedUser = UpdateLocalUserData(1, (string[])_list[1]);

                SystemEventController.Instance.DispatchSystemEvent(UsersController.EVENT_USER_RESULT_FORMATTED_SINGLE_RECORD, requestedUser);
            }
            if (_nameEvent == EVENT_USER_RESULT_CONSULT_ALL_RECORDS)
            {
                if ((bool)_list[0])
                {
                    _users.Clear();

                    List<string[]> dataCollected = (List<string[]>)_list[1];

                    for (int i = 0; i < dataCollected.Count; i++)
                    {
                        UserModel user = new UserModel();
                        user.UpdateAllData(0, dataCollected[i]);
                        _users.Add(user.Id, user);
                    }
                    SystemEventController.Instance.DispatchSystemEvent(EVENT_USER_RESULT_FORMATTED_ALL_RECORDS, true, _users);
                }
            }
            if (_nameEvent == EVENT_USER_CONFIRMATION_REMOVED_RECORD)
            {
                if ((bool)_list[0])
                {
                    long idToRemove = long.Parse((string)_list[1]);
                    _users.Remove(idToRemove);
#if ENABLE_APPLE                    
                    SystemEventController.Instance.DispatchSystemEvent(AppleController.EVENT_APPLE_CONTROLLER_CLEAR_LOCAL_DATA);
#endif                    
                    SystemEventController.Instance.DispatchSystemEvent(EVENT_USER_RESULT_FORMATTED_ALL_RECORDS, false, _users);
                }
            }            
            if (_nameEvent == EVENT_USER_RESET_LOCAL_DATA)
            {
                if (_users != null) _users.Clear();
                if (_currentUser != null)
                {
                    _currentUser.ResetLocalData();
                }
                UserModel.ResetLocalStoredDataInPlayerPrefs();
            }
            if (_nameEvent == UsersController.EVENT_USER_INPUT_FORM_RESULT)
            {
                bool success = (bool)_list[0];                
                SystemEventController.Instance.DispatchSystemEvent(EVENT_USER_INPUT_FORM_FORMATTED, success);
            }
        }

    }
}
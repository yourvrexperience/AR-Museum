using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.UserManagement
{
    public class CommsHTTPConstants : MonoBehaviour
    {
        public string URL_BASE_PHP = "http://localhost:8080/usermanagement/";

        public const string URL_BASE_COOCKIE = "URL_BASE_COOCKIE";

        public const string EVENT_COMM_CONFIGURATION_PARAMETERS         = "yourvrexperience.UserManagement.GetConfigurationServerParametersHTTP";
        public const string EVENT_COMM_REQUEST_USER_BY_LOGIN            = "yourvrexperience.UserManagement.UserLoginByEmailHTTP";
        public const string EVENT_COMM_REQUEST_USER_REGISTER            = "yourvrexperience.UserManagement.UserRegisterWithEmailHTTP";
        public const string EVENT_COMM_REQUEST_USER_SOCIAL_LOGIN        = "yourvrexperience.UserManagement.UserSocialLoginHTTP";
        public const string EVENT_COMM_REQUEST_USER_CONSULT             = "yourvrexperience.UserManagement.UserConsultByIdHTTP";
        public const string EVENT_COMM_REQUEST_USER_CONSULT_ALL         = "yourvrexperience.UserManagement.UserConsultAllUsersHTTP";
        public const string EVENT_COMM_REQUEST_UPDATE_PROFILE           = "yourvrexperience.UserManagement.UserUpdateProfileHTTP";
        public const string EVENT_COMM_REQUEST_RESET_PASSWORD           = "yourvrexperience.UserManagement.UserRequestResetPasswordHTTP";
        public const string EVENT_COMM_REQUEST_RESET_BY_EMAIL_PASSWORD  = "yourvrexperience.UserManagement.UserRequestResetByEmailPasswordHTTP";
        public const string EVENT_COMM_CHECK_VALIDATION_USER            = "yourvrexperience.UserManagement.UserCheckValidationUserHTTP";
        public const string EVENT_COMM_REQUEST_REMOVE_SINGLE_USER       = "yourvrexperience.UserManagement.UserRemoveSingleRecordHTTP";
        public const string EVENT_COMM_REQUEST_REGISTER_INPUT_FORM       = "yourvrexperience.UserManagement.UserRegisterInputFormHTTP";

        private static CommsHTTPConstants _instance;

        public static CommsHTTPConstants Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(CommsHTTPConstants)) as CommsHTTPConstants;
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

        public void GetServerConfigurationParameters()
        {
            CommController.Instance.Request(EVENT_COMM_CONFIGURATION_PARAMETERS, true);
        }

        public void RequestUserByLogin(string email, string password)
        {
            CommController.Instance.Request(EVENT_COMM_REQUEST_USER_BY_LOGIN, false, email, password);
        }

        public void RequestUserRegister(string email, string password, string platform)
        {
            CommController.Instance.Request(EVENT_COMM_REQUEST_USER_REGISTER, false, email, password, platform);
        }

        public void RequestSocialLogin(string email, string password, string platform)
        {
            CommController.Instance.Request(EVENT_COMM_REQUEST_USER_SOCIAL_LOGIN, false, email, password, platform);
        }

        public void RequestUpdateProfile(string id, string password, string user, string name, string address, string description, string data, string data2, string data3, string data4, string data5)
        {
            CommController.Instance.Request(EVENT_COMM_REQUEST_UPDATE_PROFILE, false, id, password, user, name, address, description, data, data2, data3, data4, data5);
        }

        public void RequestResetPassword(string id)
        {
            CommController.Instance.Request(EVENT_COMM_REQUEST_RESET_PASSWORD, true, id);
        }

        public void RequestResetPasswordByEmail(string email)
        {
            CommController.Instance.Request(EVENT_COMM_REQUEST_RESET_BY_EMAIL_PASSWORD, true, email);
        }

        public void RequestConsultUser(long idOwnUser, string password, long idUserSearch)
        {
            CommController.Instance.Request(EVENT_COMM_REQUEST_USER_CONSULT, false, idOwnUser.ToString(), password, idUserSearch.ToString());
        }

        public void RequestConsultAllUsers(long idOwnUser, string password, bool loadFacebook = false, bool loadProfile = false)
        {
            CommController.Instance.Request(EVENT_COMM_REQUEST_USER_CONSULT_ALL, false, idOwnUser.ToString(), password, loadFacebook.ToString(), loadProfile.ToString());
        }

        public void RequestRemoveSingleUser(long idOwnUser, string password, long userToRemove)
        {
            CommController.Instance.Request(EVENT_COMM_REQUEST_REMOVE_SINGLE_USER, false, idOwnUser.ToString(), password, userToRemove.ToString());
        }

        public void RequestInputForm(string emailAddress, string jsonFormData)
        {
            CommController.Instance.Request(EVENT_COMM_REQUEST_REGISTER_INPUT_FORM, false, emailAddress, jsonFormData);
        }

        public void CheckUserValidation(long idUser)
        {
            CommController.Instance.Request(EVENT_COMM_CHECK_VALIDATION_USER, false, idUser.ToString());
        }
    }
}

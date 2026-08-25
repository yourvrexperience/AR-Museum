using System;
using UnityEngine;
#if ENABLE_GOOGLE || ENABLE_FACEBOOK
using yourvrexperience.Social;
#endif
using yourvrexperience.Utils;

namespace yourvrexperience.UserManagement
{
    public class UserModel
    {
        public const char TOKEN_SEPARATOR_SKILL_PARAMETER = ';';
        public const string TOKEN_SEPARATOR_SKILL_LINE = "<skill>";

        public const string ACCOUNT_DATA_EMAIL = "EMAIL";
        public const string ACCOUNT_DATA_FACEBOOK = "FACEBOOK";
        public const string ACCOUNT_DATA_GOOGLE = "GOOGLE";
        public const string ACCOUNT_DATA_TWITTER = "TWITTER";
        public const string ACCOUNT_DATA_PROFILE = "PROFILE";

        // COOKIES
        public const string USER_EMAIL_COOCKIE = "USER_EMAIL_COOCKIE";
        public const string USER_NAME_COOCKIE = "USER_NAME_COOCKIE";
        public const string USER_PASSWORD_COOCKIE = "USER_PASSWORD_COOCKIE";
        public const string USER_PLATFORM_COOCKIE = "USER_PLATFORM_COOCKIE";

        private long _id = -1;
        private int _level = -1;
        private bool _validated = false;
        private string _email = "";
        private string _nickname;
        private string _password;
        private bool _admin = false;
        private int _adminCode = -1;
        private string _hashedPassword;
        private string _salt = "";

        private long _registerdate;
        private long _lastlogin;

        private LoginPlatforms _platform;
        private string _platformData;

        private ProfileModel _profile;

        public long Id
        {
            get { return _id; }
        }
        public bool Validated
        {
            get { return _validated; }
            set { _validated = value; }
        }
        public string Email
        {
            get { return _email; }
            set
            {
                if (value.Length > 0)
                {
                    _email = value;
                }
            }
        }
        public string Password
        {
            get { return _password; }
        }
        public string PasswordPlain
        {
            get { return _password; }
        }
        public string Salt
        {
            get { return _salt; }
            set { _salt = value; }
        }
        public string Nickname
        {
            get { return _nickname; }
        }
        public long Registerdate
        {
            get { return _registerdate; }
        }
        public long Lastlogin
        {
            get { return _lastlogin; }
        }
        public bool Admin
        {
            get { return _admin; }
        }
        public int AdminCode
        {
            get { return _adminCode; }
        }
        public ProfileModel Profile
        {
            get { return _profile; }
            set { _profile = value; }
        }
        public LoginPlatforms Platform
        {
            get { return _platform; }
            set { _platform = value; }
        }        
        public string HashedPassword
        {
            get { return _hashedPassword; }
        }
        public string PlatformData
        {
            get { return _platformData; }
        }        
        public UserModel(params string[] parameters)
        {
            if (parameters.Length == 8)
            {
                _id = long.Parse(parameters[0]);
                _email = parameters[1];
                _nickname = parameters[2];
                _registerdate = long.Parse(parameters[3]);
                _lastlogin = long.Parse(parameters[4]);
                _admin = (int.Parse(parameters[5]) > 0);
                _adminCode = int.Parse(parameters[5]);
                _level = int.Parse(parameters[6]);
                _validated = (int.Parse(parameters[7]) == 1);
                _salt = (_registerdate + _lastlogin).ToString();
            }
        }

        public bool IsEmptyUser()
        {
            return (_id == -1) || (_email == null) || (_email.Length == 0);
        }

        public void LoadLocalInfo()
        {
            _email = UsersController.Instance.DecryptData(PlayerPrefs.GetString(USER_EMAIL_COOCKIE, ""));            
            _password = UsersController.Instance.DecryptData(PlayerPrefs.GetString(USER_PASSWORD_COOCKIE, ""));
            _platform = (LoginPlatforms)PlayerPrefs.GetInt(USER_PLATFORM_COOCKIE, 0);
        }

        public void UpdateBasicInfo(string email, string password, LoginPlatforms platform)
        {
            Email = email;
            _password = password;
            _platform = platform;

            SaveLocalEmailLogin(Email, _password, platform);
        }

        public static void SaveLocalEmailLogin(string email, string password, LoginPlatforms platform)
        {
            PlayerPrefs.SetString(USER_EMAIL_COOCKIE, UsersController.Instance.EncryptData(email.ToLower()));
            PlayerPrefs.SetString(USER_PASSWORD_COOCKIE, UsersController.Instance.EncryptData(password));
            SavePlatform(platform);
        }

        public static bool LoginWithStoredLogin()
        {            
            string email = "";
            try { email = UsersController.Instance.DecryptData(PlayerPrefs.GetString(USER_EMAIL_COOCKIE, "")); } catch (Exception err) {}
            string password = "";
            try { password = UsersController.Instance.DecryptData(PlayerPrefs.GetString(USER_PASSWORD_COOCKIE, "")); } catch (Exception err) {}
            LoginPlatforms platform = LoginPlatforms.Email;
            try { platform = (LoginPlatforms)PlayerPrefs.GetInt(USER_PLATFORM_COOCKIE, 0); } catch (Exception err) {}
            
            if ((email.Length == 0) || (password.Length == 0))
            {
                return false;
            }
            else
            {  
                switch (platform)
                {
                    case LoginPlatforms.Email:
                        UIEventController.Instance.DelayUIEvent(UsersController.EVENT_USER_LOGIN_REQUEST, 0.2f, email, password, platform);
                        break;

                    case LoginPlatforms.Facebook:
                        UsersController.Instance.LoginRequested = true;
#if ENABLE_FACEBOOK
                        FacebookController.Instance.Initialitzation();
#endif
                        break;

                    case LoginPlatforms.Google:
                        UsersController.Instance.LoginRequested = true;
#if ENABLE_GOOGLE
                        GoogleController.Instance.Initialitzation();
#endif
                        break;

                    case LoginPlatforms.Apple:
                        UsersController.Instance.LoginRequested = true;
#if ENABLE_APPLE
                        AppleController.Instance.Initialitzation();
#endif
                        break;
                }

                return true;
            }
        }

        public static string StoredEmail()
        {
            string emailEncrypted = UsersController.Instance.DecryptData(PlayerPrefs.GetString(USER_EMAIL_COOCKIE, ""));
            if (emailEncrypted.Length > 0)
            {
                return emailEncrypted;
            }
            else
            {
                return "";
            }
        }

        public static string StoredPassword()
        {
            string passwordEncrypted = UsersController.Instance.DecryptData(PlayerPrefs.GetString(USER_PASSWORD_COOCKIE, ""));
            if (passwordEncrypted.Length > 0)
            {
                return passwordEncrypted;
            }
            else
            {
                return "";
            }
        }

        public void ResetLocalData()
        {
            _id = -1;
            _email = "";
            _password = "";
            _platform = LoginPlatforms.Email;

            ResetLocalStoredDataInPlayerPrefs();
        }

        public static void SavePlatform(LoginPlatforms platform)
        {
            PlayerPrefs.SetInt(USER_PLATFORM_COOCKIE, (int)platform);
        }

        public static void ResetLocalStoredDataInPlayerPrefs()
        {
            PlayerPrefs.SetString(USER_EMAIL_COOCKIE, "");
            PlayerPrefs.SetString(USER_PASSWORD_COOCKIE, "");
            SavePlatform(LoginPlatforms.Email);
        }

        public int GetLevel()
        {
            return _level;
        }

        public void Copy(UserModel user)
        {
            Email = user.Email;
            _id = user.Id;
            _nickname = user.Nickname;
            _registerdate = user.Registerdate;
            _lastlogin = user.Lastlogin;
            _admin = user.Admin;
            _adminCode = user.AdminCode;
            _level = user.GetLevel();
            _validated = user.Validated;
            _platform = user.Platform;
            if (user.Profile != null) _profile = user.Profile.Clone();
        }

        public UserModel Clone()
        {
            UserModel clone = new UserModel(_id.ToString(), _nickname, _registerdate.ToString(), _lastlogin.ToString(), _admin.ToString(), _level.ToString(), _validated.ToString());
            clone.Email = Email;
            clone.Platform = Platform;
            if (_profile != null) clone.Profile = _profile.Clone();
            return clone;
        }

        public void UpdateData(string[] parameters)
        {
            if (parameters.Length != 9)
            {
                throw new Exception("UserModel::UpdateData::WRONG NUMBER OF PARAMETERS["+ parameters.Length + "]");
            }
            else
            {
                _id = long.Parse((string)parameters[0]);
                _email = (string)parameters[1];
                // _password = (string)parameters[2];
                _nickname = (string)parameters[2];
                _registerdate = long.Parse((string)parameters[3]);
                _lastlogin = long.Parse((string)parameters[4]);
                _admin = (int.Parse((string)parameters[5]) > 0);
                _adminCode = int.Parse((string)parameters[5]);
                _level = int.Parse((string)parameters[6]);
                _validated = (int.Parse((string)parameters[7]) == 1);
                _salt = (_registerdate + _lastlogin).ToString();
                _platformData = (string)parameters[8];

                _hashedPassword = SHAEncryption.HashPassword(_password, _lastlogin.ToString());
                // Debug.LogError("+++++++++++HASHED PASSWORD["+_password+"]="+ _hashedPassword);
            }
        }

        public void UpdateProfile(string[] parameters)
        {
            if (parameters.Length != 11)
            {
                throw new Exception("UserModel::UpdateProfile::WRONG NUMBER OF PARAMETERS[" + parameters.Length + "]");
            }
            else
            {
                _profile = new ProfileModel(long.Parse(parameters[1]), _id, parameters[2], parameters[3], parameters[4], parameters[5], parameters[6], parameters[7], parameters[8], parameters[9], (int.Parse(parameters[10]) == 1));
            }
        }

        public bool IsAllDataLoaded()
        {
            return (_profile != null);
        }        

        public void UpdateAllData(int indexData, string[] allData)
        {
            for (int i = indexData; i < allData.Length; i++)
            {
                string[] items = allData[i].Split(new string[] { CommController.TOKEN_SEPARATOR_EVENTS }, StringSplitOptions.None);
                if (i == indexData)
                {
                    UpdateData(items);
                }
                else
                {
                    switch (items[0])
                    {
                        case ACCOUNT_DATA_PROFILE:
                            UpdateProfile(items);
                            break;
                    }
                }
            }
        }

        public static string FormatPacketData(long _id, string _email, string _password, string _nickname = "", long _registerdate = -1, long _lastlogin = -1, int _admin = -1, int _validated = -1)
        {
            return _id + CommController.TOKEN_SEPARATOR_EVENTS +
                _email + CommController.TOKEN_SEPARATOR_EVENTS +
                // _password + CommController.TOKEN_SEPARATOR_EVENTS +
                _nickname + CommController.TOKEN_SEPARATOR_EVENTS +
                _registerdate + CommController.TOKEN_SEPARATOR_EVENTS +
                _lastlogin + CommController.TOKEN_SEPARATOR_EVENTS +
                _admin + CommController.TOKEN_SEPARATOR_EVENTS +
                _validated;
        }
    }
}
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.UserManagement
{
    public class UsersTools
    {
#if UNITY_EDITOR
        public const string USER_EMAIL_COOCKIE = "USER_EMAIL_COOCKIE";
        public const string USER_NAME_COOCKIE = "USER_NAME_COOCKIE";
        public const string USER_PASSWORD_COOCKIE = "USER_PASSWORD_COOCKIE";
        public const string USER_FACEBOOK_CONNECTED_COOCKIE = "USER_FACEBOOK_CONNECTED_COOCKIE";

        [MenuItem("Users Manager/Clear PlayerPrefs")]
        private static void NewMenuOption()
        {
            PlayerPrefs.DeleteAll();
            Debug.Log("PlayerPrefs CLEARED!!!");
        }

        [MenuItem("Users Manager/Account Info")]
        private static void UseAccountA()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetString(USER_EMAIL_COOCKIE, "info@yourvrexperience.com");
            PlayerPrefs.SetString(USER_PASSWORD_COOCKIE, "12345");
            PlayerPrefs.SetString(USER_NAME_COOCKIE, "Account Info");
            PlayerPrefs.SetInt(USER_FACEBOOK_CONNECTED_COOCKIE, 0);
            Debug.Log("PlayerPrefs Switched Account Info!!!");
        }        
#endif
    }
}
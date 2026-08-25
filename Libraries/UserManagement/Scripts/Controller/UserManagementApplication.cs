using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.UserManagement
{
    public class UserManagementApplication : MonoBehaviour
    {
        public const bool DebugMode = true;

        public const string PLAYERPREFS_LOCAL_ENCRYPTION = "sEcrEt-fTAy-UseR-MaNagEmeNT";
        public const string EncryptionLocalAESKey = "TKerBoVopQiIZZcy";
        
        private static UserManagementApplication _instance;

        public static UserManagementApplication Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(UserManagementApplication)) as UserManagementApplication;
                }
                return _instance;
            }
        }

        [SerializeField] private FormData formData;

        public FormData FormData
        {
            get { return formData; }
        }

        public void Start()
        {
            if (DebugMode)
            {
                Debug.Log("YourVRUIScreenController::Start::First class to initialize for the whole system to work");
            }

            ScreenController.Instance.Initialize();
            CommController.Instance.Init();
            UsersController.Instance.Initialize(PLAYERPREFS_LOCAL_ENCRYPTION, EncryptionLocalAESKey);

            ScreenController.Instance.CreateScreen(ScreenConnectionView.ScreenName, true, false);	
        }

        void OnDestroy()
        {
            Destroy();
        }

        public void Destroy()
        {
            if (_instance != null)
            {
                UserManagementApplication instanceUsers = _instance;
                _instance = null;

                if (CommController.Instance != null) CommController.Instance.Destroy();
                if (UsersController.Instance != null) UsersController.Instance.Destroy();

                Destroy(instanceUsers);                
            }
        }
    }
}
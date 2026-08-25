using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using yourvrexperience.Utils;
#if ENABLE_GOOGLE
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace yourvrexperience.Social
{

	public class GoogleController : MonoBehaviour
	{
		public const bool DEBUG = true;

		public const string EVENT_GOOGLE_CONTROLLER_AUTHENTICATED = "EVENT_GOOGLE_CONTROLLER_AUTHENTICATED";

		private static GoogleController _instance;

		public static GoogleController Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(GoogleController)) as GoogleController;
					if (!_instance)
					{
						GameObject container = new GameObject();
						container.name = "GoogleController";
						_instance = container.AddComponent(typeof(GoogleController)) as GoogleController;
						DontDestroyOnLoad(_instance);
					}
				}
				return _instance;
			}
		}

		public void Destroy()
		{
			if (_instance != null)
			{
				Destroy(_instance.gameObject);
				_instance = null;
			}
		}

		public void Initialitzation()
		{
#if ENABLE_GOOGLE
 			PlayGamesPlatform.Activate();
			PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
#endif
		}

#if ENABLE_GOOGLE
		internal void ProcessAuthentication(SignInStatus status) 
		{
			if (status == SignInStatus.Success) 
			{
 				if (DEBUG) Debug.LogError("GoogleController::Login with Google Play games successful::Social.localUser.userName=" + UnityEngine.Social.localUser.userName);

                PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
                {
                    if (DEBUG) Debug.LogError("Authorization code: " + code);
					UIEventController.Instance.DelayUIEvent(EVENT_GOOGLE_CONTROLLER_AUTHENTICATED, 0.1f, true, code);
                });
			} 
			else 
			{
				if (DEBUG) Debug.LogError("GoogleController::Error autentication");
				UIEventController.Instance.DelayUIEvent(EVENT_GOOGLE_CONTROLLER_AUTHENTICATED, 0.1f, false);
			}
		}
#endif
	}
}


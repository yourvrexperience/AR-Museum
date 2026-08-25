using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Text;
using yourvrexperience.Utils;
using System.Security.Cryptography;
#if ENABLE_APPLE
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
using AppleAuth.Native;
#endif

namespace yourvrexperience.Social
{

	public class AppleController : MonoBehaviour
	{
		public const bool DEBUG = true;

		public const string EVENT_APPLE_CONTROLLER_AUTHENTICATED = "EVENT_APPLE_CONTROLLER_AUTHENTICATED";
		public const string EVENT_APPLE_CONTROLLER_LOGIN_AGAIN = "EVENT_APPLE_CONTROLLER_LOGIN_AGAIN";
		public const string EVENT_APPLE_CONTROLLER_CLEAR_LOCAL_DATA = "EVENT_APPLE_CONTROLLER_CLEAR_LOCAL_DATA";
		public const string EVENT_APPLE_CONTROLLER_CANCELATION = "EVENT_APPLE_CONTROLLER_CANCELATION";
		
		private static AppleController _instance;

		public static AppleController Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(AppleController)) as AppleController;
					if (!_instance)
					{
						GameObject container = new GameObject();
						container.name = "AppleController";
						_instance = container.AddComponent(typeof(AppleController)) as AppleController;
						DontDestroyOnLoad(_instance);
					}
				}
				return _instance;
			}
		}
    	private const string AppleUserIdKey = "AppleUserId";
		private const string AppleUserEmailKey = "AppleUserEmail";
		private const string AppleUserRawNonceKey = "AppleUserRawNonce";
		private const string AppleUserAccessTokenKey = "AppleUserAccessTokenKey";

		private string _id;
		private string _nameHuman;
		private string _email;
		private string _accessToken;
		private string _rawNonce;

#if ENABLE_APPLE	
    	private IAppleAuthManager _appleAuthManager;
#endif
		public void Destroy()
		{
			if (_instance != null)
			{
#if ENABLE_APPLE			
				if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
				if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
#endif
				Destroy(_instance.gameObject);
				_instance = null;
			}
		}

		public void Initialitzation()
		{
#if ENABLE_APPLE			
			UIEventController.Instance.Event += OnUIEvent;
			SystemEventController.Instance.Event += OnSystemEvent;
			if (AppleAuthManager.IsCurrentPlatformSupported)
			{
				var deserializer = new PayloadDeserializer();
				_appleAuthManager = new AppleAuthManager(deserializer);  
				if (PlayerPrefs.HasKey(AppleUserIdKey))
				{
					var storedAppleUserId = PlayerPrefs.GetString(AppleUserIdKey);
					CheckCredentialStatusForUserId(storedAppleUserId);
				}
				else
				{
					SignInWithApple();
				}				  				
			}
#endif
		}

		private string CreateNonce(int length = 32)
		{
			var randomBytes = new byte[length];
			using (var generator = RandomNumberGenerator.Create())
			{
				generator.GetBytes(randomBytes);
				return Convert.ToBase64String(randomBytes);
			}
		}

#if ENABLE_APPLE
		private void SignInWithApple()
    	{
			_rawNonce = CreateNonce();
        	var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName, _rawNonce);
        
        	_appleAuthManager.LoginWithAppleId(
				loginArgs,
				credential =>
				{
					IAppleIDCredential appleIdCredential = credential as IAppleIDCredential;

					_accessToken = "";
					if (appleIdCredential.AuthorizationCode != null)
					{
						_accessToken = Encoding.UTF8.GetString(appleIdCredential.AuthorizationCode, 0, appleIdCredential.AuthorizationCode.Length);
					}
					_email = "";
					if (appleIdCredential.Email != null)
					{
						_email = appleIdCredential.Email;
					}

					_id = appleIdCredential.User;
					if (_email.Length > 0)
					{
						_nameHuman = _email.Substring(0, _email.IndexOf("@"));	
					}					

					if ((_email.Length > 0) && (_accessToken.Length > 0))
					{
						PlayerPrefs.SetString(AppleUserIdKey, credential.User);
						PlayerPrefs.SetString(AppleUserEmailKey, _email);
						PlayerPrefs.SetString(AppleUserRawNonceKey, _rawNonce);
						PlayerPrefs.SetString(AppleUserAccessTokenKey, _accessToken);

						UIEventController.Instance.DelayUIEvent(EVENT_APPLE_CONTROLLER_AUTHENTICATED, 0.1f, _id, _nameHuman, _email, _accessToken, _rawNonce);
					}
					else
					{
						UIEventController.Instance.DelayUIEvent(EVENT_APPLE_CONTROLLER_CANCELATION, 0.1f);
					}					
				},
				error =>
				{
					var authorizationErrorCode = error.GetAuthorizationErrorCode();
					Debug.LogWarning("Sign in with Apple failed " + authorizationErrorCode.ToString() + " " + error.ToString());
					UIEventController.Instance.DelayUIEvent(EVENT_APPLE_CONTROLLER_CANCELATION, 0.1f);
				});
    	}
		
		private void CheckCredentialStatusForUserId(string appleUserId)
		{
			_appleAuthManager.GetCredentialState(
				appleUserId,
				state =>
				{
					switch (state)
					{
						case CredentialState.Authorized:					
							_email = PlayerPrefs.GetString(AppleUserEmailKey, "");
							_rawNonce = PlayerPrefs.GetString(AppleUserRawNonceKey, "");
							_accessToken = PlayerPrefs.GetString(AppleUserAccessTokenKey, "");
		
							UIEventController.Instance.DelayUIEvent(EVENT_APPLE_CONTROLLER_AUTHENTICATED, 0.1f, _id, _nameHuman, _email, _accessToken, _rawNonce);
							return;
						
						case CredentialState.Revoked:
						case CredentialState.NotFound:
							PlayerPrefs.DeleteKey(AppleUserIdKey);
							UIEventController.Instance.DelayUIEvent(EVENT_APPLE_CONTROLLER_LOGIN_AGAIN, 0.2f);
							return;
					}
				},
				error =>
				{
					var authorizationErrorCode = error.GetAuthorizationErrorCode();
					UIEventController.Instance.DelayUIEvent(EVENT_APPLE_CONTROLLER_LOGIN_AGAIN, 0.2f);
				});
		}

		private void OnUIEvent(string nameEvent, params object[] parameters)
		{
			if (nameEvent == EVENT_APPLE_CONTROLLER_LOGIN_AGAIN)
			{
				SignInWithApple();
			}
		}

		private void OnSystemEvent(string nameEvent, params object[] parameters)
		{
			if (nameEvent == EVENT_APPLE_CONTROLLER_CLEAR_LOCAL_DATA)
			{
				PlayerPrefs.DeleteKey(AppleUserIdKey);
			}
		}

		private void Update()
		{
			if (_appleAuthManager != null)
			{
				_appleAuthManager.Update();
			}
		}		
#endif
	}
}


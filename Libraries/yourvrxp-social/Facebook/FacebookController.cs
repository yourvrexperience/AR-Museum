using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using yourvrexperience.Utils;
#if ENABLE_FACEBOOK
using Facebook.Unity;
#endif

namespace yourvrexperience.Social
{

	public class FacebookController : MonoBehaviour
	{
		public const bool DEBUG = true;

		public const string EVENT_FACEBOOK_REQUEST_INITIALITZATION = "EVENT_FACEBOOK_REQUEST_INITIALITZATION";
        public const string EVENT_FACEBOOK_CANCELATION = "EVENT_FACEBOOK_CANCELATION";
        public const string EVENT_FACEBOOK_MY_INFO_LOADED = "EVENT_FACEBOOK_MY_INFO_LOADED";
		public const string EVENT_FACEBOOK_FRIENDS_LOADED = "EVENT_FACEBOOK_FRIENDS_LOADED";
		public const string EVENT_FACEBOOK_COMPLETE_INITIALITZATION = "EVENT_FACEBOOK_COMPLETE_INITIALITZATION";
		public const string EVENT_REGISTER_IAP_COMPLETED = "EVENT_REGISTER_IAP_COMPLETED";

		private static FacebookController _instance;

		public static FacebookController Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(FacebookController)) as FacebookController;
					if (!_instance)
					{
						GameObject container = new GameObject();
						container.name = "FacebookController";
						_instance = container.AddComponent(typeof(FacebookController)) as FacebookController;
						DontDestroyOnLoad(_instance);
					}
				}
				return _instance;
			}
		}

		private string _id = null;
		private string _nameHuman;
		private string _email;
		private bool _isInited = false;
		private bool _invitationAccepted = false;

		private List<ItemMultiTextEntry> _friends = new List<ItemMultiTextEntry>();
        private string _friendsCompact = "";

        private string _accessToken = "";

        public string Id
		{
			get { return _id; }
			set { _id = value; }
		}
		public string NameHuman
		{
			get { return _nameHuman; }
			set { _nameHuman = value; }
		}
		public string Email
		{
			get { return _email; }
		}
		public List<ItemMultiTextEntry> Friends
		{
			get { return _friends; }
		}
		public bool IsInited
		{
			get { return _isInited; }
		}
		public bool InvitationAccepted
		{
			get { return _invitationAccepted; }
		}
        public string AccessToken
        {
            get { return _accessToken; }
        }

		public void InitListener()
		{
			UIEventController.Instance.Event += OnMenuEvent;
		}

		public void Destroy()
		{
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnMenuEvent;
			Destroy(_instance.gameObject);
			_instance = null;
		}

		public void Initialitzation()
		{
#if ENABLE_FACEBOOK
			if (!FB.IsInitialized)
			{
				if (!_isInited)
				{
					InitListener();
					FB.Init(this.OnInitComplete, this.OnHideUnity);
                }
				else
				{
					InitListener();
					RegisterConnectionFacebookID(true);
				}
			}
			else
			{
				InitListener();
				FB.ActivateApp();
				OnInitComplete();
			}
#endif
		}

        public void Logout()
        {
#if ENABLE_FACEBOOK
            if (FB.IsInitialized && FB.IsLoggedIn)
            {
                FB.LogOut();
            }
#endif
        }

        private void OnInitComplete()
		{
#if ENABLE_FACEBOOK
			UIEventController.Instance.DispatchUIEvent(EVENT_FACEBOOK_REQUEST_INITIALITZATION);
			if (DEBUG) Debug.LogError("OnInitCompleteCalled IsLoggedIn='{" + FB.IsLoggedIn + "}' IsInitialized='{" + FB.IsInitialized + "}'");
            if (FB.IsInitialized)
            {
                LogInWithPermissions();
            }
            else
            {
                UIEventController.Instance.DispatchUIEvent(EVENT_FACEBOOK_CANCELATION);
            }
#endif
		}

		private void OnHideUnity(bool isGameShown)
		{
            UIEventController.Instance.DispatchUIEvent(EVENT_FACEBOOK_CANCELATION);
        }

		private void LogInWithPermissions()
		{
#if ENABLE_FACEBOOK
#if ENABLE_FACEBOOK_FRIENDS
            FB.LogInWithReadPermissions(new List<string>() { "public_profile", "email", "user_friends" }, LoggedWithPermissions);
#else
            FB.LogInWithReadPermissions(new List<string>() { "public_profile", "email" }, LoggedWithPermissions);
#endif
#endif
        }

#if ENABLE_FACEBOOK
        private void LoggedWithPermissions(IResult result)
		{
			if (result == null)
			{
                UIEventController.Instance.DispatchUIEvent(EVENT_FACEBOOK_CANCELATION);
                return;
			}

            if (Facebook.Unity.AccessToken.CurrentAccessToken != null)
			{
				_accessToken = Facebook.Unity.AccessToken.CurrentAccessToken.TokenString;
			}

            if (FB.IsLoggedIn && !result.Cancelled)
            {
                FB.API("/me?fields=id,name,email", HttpMethod.GET, HandleMyInformation);
            }
            else
            {
                UIEventController.Instance.DispatchUIEvent(EVENT_FACEBOOK_CANCELATION);
            }
		}
#endif

#if ENABLE_FACEBOOK
		private void HandleMyInformation(IResult result)
		{
			if (result == null)
			{
				return;
			}

			JSONNode jsonResponse = JSONNode.Parse(result.RawResult);

			_id = jsonResponse["id"];
			_nameHuman = jsonResponse["name"];
			_email = jsonResponse["email"];

			if (DEBUG) Debug.LogError("CURRENT PLAYER NAME=" + _nameHuman + "::ID=" + _id + "::EMAIL=" + _email);

			if (_email.Length == 0)
			{
				UIEventController.Instance.DispatchUIEvent(EVENT_FACEBOOK_CANCELATION);
			}
			else
			{
				UIEventController.Instance.DispatchUIEvent(EVENT_FACEBOOK_MY_INFO_LOADED);

#if ENABLE_FACEBOOK_FRIENDS
				FB.API("/me/friends", HttpMethod.GET, HandleListOfFriends);
#else
				RegisterConnectionFacebookID(true);
#endif
			}
        }
#endif

#if ENABLE_FACEBOOK
        private void HandleListOfFriends(IResult result)
		{
			if (result == null)
			{
				if (DEBUG) Debug.Log("HandleListOfFriends::Null Response");
				return;
			}

			if (DEBUG) Debug.LogError("FacebookController::HandleListOfFriends::result.RawResult=" + result.RawResult);
			JSONNode jsonResponse = JSONNode.Parse(result.RawResult);

			JSONNode friends = jsonResponse["data"];
			if (DEBUG) Debug.LogError("FacebookController::HandleListOfFriends::friends.Count=" + friends.Count);
            _friendsCompact = "";
			for (int i = 0; i < friends.Count; i++)
			{
				string nameFriend = friends[i]["name"];
				string idFriend = friends[i]["id"];
				_friends.Add(new ItemMultiTextEntry(idFriend, nameFriend));
                if (_friendsCompact.Length > 0)
                {
                    _friendsCompact += ";";
                }
                _friendsCompact += idFriend;
                if (DEBUG) Debug.Log("   NAME=" + nameFriend + ";ID=" + idFriend);
			}

			UIEventController.Instance.DispatchUIEvent(EVENT_FACEBOOK_FRIENDS_LOADED);

			RegisterConnectionFacebookID(true);
		}
#endif

		public void RegisterConnectionFacebookID(bool dispatchCompletedFacebookInit)
		{
			if (_id != null)
			{
				_isInited = true;
			}
			else
			{
				_isInited = false;
			}
			if (dispatchCompletedFacebookInit)
			{
                DispatchCompletedConnectionEvent();
            }
		}

        public bool DispatchCompletedConnectionEvent()
        {
            if (_id == null)
            {
                return false;
            }
            else
            {
                UIEventController.Instance.DelayUIEvent(EVENT_FACEBOOK_COMPLETE_INITIALITZATION, 0.1f, _id, _nameHuman, _email, _accessToken);
                return true;
            }            
        }

		private void OnMenuEvent(string nameEvent, params object[] parameters)
		{
			if (nameEvent == EVENT_REGISTER_IAP_COMPLETED)
			{
			}
		}

		public string GetNameOfFriendID(string facebookID)
		{
			for (int i = 0; i < _friends.Count; i++)
			{
				if (_friends[i].Items[0] == facebookID)
				{
					return _friends[i].Items[1];
				}
			}

			return null;
		}

		public string GetPackageFriends()
		{
			string output = "";
			for (int i = 0; i < _friends.Count; i++)
			{
				output += _friends[i].Items[0] + "," + _friends[i].Items[1];
				if (i < _friends.Count - 1)
				{
					output += ";";
				}
			}
			return output;
		}

		public void SetFriends(string data)
		{
			string[] friendsList = data.Split(';');
			_friends.Clear();
			for (int i = 0; i < friendsList.Length; i++)
			{
				string[] sFriendEntry = friendsList[i].Split(',');
				if (sFriendEntry.Length == 2)
				{
					_friends.Add(new ItemMultiTextEntry(sFriendEntry[0], sFriendEntry[1]));
				}
			}
		}

	}
}


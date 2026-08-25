using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace yourvrexperience.Social
{
	public class GoogleAuth : MonoBehaviour
	{
		private static GoogleAuth _instance;

		public static GoogleAuth Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(GoogleAuth)) as GoogleAuth;
				}
				return _instance;
			}
		}

		[SerializeField] string backendBaseUrl = "https://your-backend.example.com";

		[SerializeField] string AppScheme = "com.yourvrexperience.museum";

		const string PrefsToken = "session_jwt";
		const string PrefsState = "oauth_state";

		public event Action<string> OnSignedIn;
		public event Action<string> OnSignInError;
		public event Action OnSignedOut;

		public string SessionToken => PlayerPrefs.GetString(PrefsToken, null);
		public bool IsSignedIn => !string.IsNullOrEmpty(SessionToken);
		public UserProfile Profile { get; private set; }

		public void Initialize()
		{
			Application.deepLinkActivated += OnDeepLink;

			if (!string.IsNullOrEmpty(Application.absoluteURL))
				OnDeepLink(Application.absoluteURL);
		}

		void OnDestroy() => Application.deepLinkActivated -= OnDeepLink;

		public void SignIn()
		{
			string state = Guid.NewGuid().ToString("N");

			PlayerPrefs.SetString(PrefsState, state);
			PlayerPrefs.Save();

			string url = $"{backendBaseUrl}/auth/start?state={UnityWebRequest.EscapeURL(state)}";
			Application.OpenURL(url);
		}

		public void SignOut()
		{
			PlayerPrefs.DeleteKey(PrefsToken);
			PlayerPrefs.Save();
			Profile = null;
			OnSignedOut?.Invoke();
		}

		void OnDeepLink(string url)
		{
			if (string.IsNullOrEmpty(url) || !url.StartsWith($"{AppScheme}://auth"))
				return;

			Uri uri;
			try { uri = new Uri(url); }
			catch { OnSignInError?.Invoke("bad_deeplink"); return; }

			var q = ParseQuery(uri.Query);

			if (q.TryGetValue("error", out var err))
			{
				OnSignInError?.Invoke(err);
				return;
			}

			string expected = PlayerPrefs.GetString(PrefsState, null);
			if (!q.TryGetValue("state", out var state) ||
				string.IsNullOrEmpty(expected) || state != expected)
			{
				OnSignInError?.Invoke("state_mismatch");
				return;
			}
			PlayerPrefs.DeleteKey(PrefsState);   // single use

			if (!q.TryGetValue("code", out var oneTimeCode))
			{
				OnSignInError?.Invoke("no_code");
				return;
			}

			StartCoroutine(Exchange(oneTimeCode));
		}

		IEnumerator Exchange(string oneTimeCode)
		{
			string body = JsonUtility.ToJson(new ExchangeReq { code = oneTimeCode });

			using var req = new UnityWebRequest($"{backendBaseUrl}/auth/exchange", "POST");
			req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
			req.downloadHandler = new DownloadHandlerBuffer();
			req.SetRequestHeader("Content-Type", "application/json");

			yield return req.SendWebRequest();

			if (req.result != UnityWebRequest.Result.Success)
			{
				OnSignInError?.Invoke($"exchange_failed ({req.responseCode}): {req.error}");
				yield break;
			}

			ExchangeRes res;
			try { res = JsonUtility.FromJson<ExchangeRes>(req.downloadHandler.text); }
			catch { OnSignInError?.Invoke("bad_response"); yield break; }

			PlayerPrefs.SetString(PrefsToken, res.token);
			PlayerPrefs.Save();
			Profile = res.user;
			OnSignedIn?.Invoke(res.token);
		}

		public void PostAuthedJson(string path, string jsonBody,
								Action<string> onSuccess, Action<long, string> onError = null)
			=> StartCoroutine(SendAuthed("POST", path, jsonBody, null, onSuccess, onError));

		public void GetAuthedJson(string path,
								Action<string> onSuccess, Action<long, string> onError = null)
			=> StartCoroutine(SendAuthed("GET", path, null, null, onSuccess, onError));

		public void PostAuthedBinary(string path, string jsonBody,
									Action<byte[]> onSuccess, Action<long, string> onError = null)
			=> StartCoroutine(SendAuthed("POST", path, jsonBody, onSuccess, null, onError));

		IEnumerator SendAuthed(string method, string path, string jsonBody,
							Action<byte[]> onBytes, Action<string> onText,
							Action<long, string> onError)
		{
			using var req = new UnityWebRequest($"{backendBaseUrl}{path}", method);
			req.downloadHandler = new DownloadHandlerBuffer();

			if (!string.IsNullOrEmpty(jsonBody))
			{
				req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
				req.SetRequestHeader("Content-Type", "application/json");
			}

			string token = SessionToken;
			if (!string.IsNullOrEmpty(token))
				req.SetRequestHeader("Authorization", "Bearer " + token);

			yield return req.SendWebRequest();

			long status = req.responseCode;

			if (req.result == UnityWebRequest.Result.Success)
			{
				onBytes?.Invoke(req.downloadHandler.data);
				onText?.Invoke(req.downloadHandler.text);
				yield break;
			}

			if (status == 401)
				SignOut();

			onError?.Invoke(status, req.error);
		}

		static Dictionary<string, string> ParseQuery(string query)
		{
			var dict = new Dictionary<string, string>();
			query = query.TrimStart('?');
			foreach (var pair in query.Split('&'))
			{
				if (string.IsNullOrEmpty(pair)) continue;
				var kv = pair.Split(new[] { '=' }, 2);
				string key = UnityWebRequest.UnEscapeURL(kv[0]);
				string val = kv.Length > 1 ? UnityWebRequest.UnEscapeURL(kv[1]) : "";
				dict[key] = val;
			}
			return dict;
		}

		[Serializable] class ExchangeReq { public string code; }
		[Serializable] class ExchangeRes { public string token; public UserProfile user; }

		[Serializable]
		public class UserProfile
		{
			public string sub;
			public string email;
			public string name;
			public string picture;
		}
	}
}
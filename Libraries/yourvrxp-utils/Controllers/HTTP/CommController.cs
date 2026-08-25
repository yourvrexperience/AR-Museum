using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace yourvrexperience.Utils
{
	public class CommController : StateMachine
	{
		public const char TOKEN_SEPARATOR_COMA = ',';
        public const string TOKEN_SEPARATOR_BLOCKS = "<block>";
        public const string TOKEN_SEPARATOR_EVENTS = "<par>";
		public const string TOKEN_SEPARATOR_LINES = "<line>";
        public const string TOKEN_SEPARATOR_USER_DATA = "<udata>";

        public const string EVENT_COMM_GET_FILE_DATA       = "yourvrexperience.Utils.GetFileDataHTTP";

#if UNITY_EDITOR
		public const bool DEBUG_LOG = true;
#else
		public const bool DEBUG_LOG = false;
#endif

		public const int STATE_IDLE = 0;
		public const int STATE_COMMUNICATION = 1;

		private static CommController _instance;

		public static CommController Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(CommController)) as CommController;
				}
				return _instance;
			}
		}
		public bool ReloadXML = false;

		private string _event;
		private IHTTPComms _commRequest;
		private List<CommEventData> _listTimedEvents = new List<CommEventData>();
		private List<CommEventData> _listQueuedEvents = new List<CommEventData>();
		private List<CommEventData> _priorityQueuedEvents = new List<CommEventData>();

		private string _inGameLog = "";
		private UnityWebRequest _currentWWW;

		public static string FilterSpecialTokens(string _text)
		{
			string output = _text;

			string[] arrayEvents = output.Split(new string[] { TOKEN_SEPARATOR_EVENTS }, StringSplitOptions.None);
			output = "";
			for (int i = 0; i < arrayEvents.Length; i++)
			{
				output += arrayEvents[i];
				if (i + 1 < arrayEvents.Length)
				{
					output += " ";
				}
			}


			string[] arrayLines = output.Split(new string[] { TOKEN_SEPARATOR_LINES }, StringSplitOptions.None);
			output = "";
			for (int i = 0; i < arrayLines.Length; i++)
			{
				output += arrayLines[i];
				if (i + 1 < arrayLines.Length)
				{
					output += " ";
				}
			}

			return output;
		}

		private CommController()
		{
			ChangeState(STATE_IDLE);
		}

		public void Init()
		{
			ChangeState(STATE_IDLE);
		}

        void OnDestroy()
        {
#if DESTROY_MEMORY_ALLOCATED
            Destroy();
#endif
        }

        public void Destroy()
		{
			if (_instance != null)
			{
				_currentWWW = null;				
				Destroy(_instance.gameObject);
				_instance = null;
			}
		}

		public void StopCurrentComm()
        {
			try
			{
				if (_commRequest != null)
                {
					_commRequest.CancelResponse();
				}
				_listTimedEvents.Clear();
				_listQueuedEvents.Clear();
				_priorityQueuedEvents.Clear();
				ChangeState(STATE_IDLE);
			}
			catch (Exception err) { }
		}

        public void RequestHeader(string _event, List<ItemMultiTextEntry> _headers, bool _isBinaryResponse, params object[] _list)
        {
            if (_state != STATE_IDLE)
            {
                QueuedRequest(_event, _headers, _isBinaryResponse, _list);
                return;
            }

            RequestReal(_event, _headers, _isBinaryResponse, _list);
        }

		public void RequestPriorityHeader(string _event, List<ItemMultiTextEntry> _headers, bool _isBinaryResponse, params object[] _list)
		{
			if (_state != STATE_IDLE)
			{
				InsertRequest(_event, _headers, _isBinaryResponse, _list);
				return;
			}

			RequestReal(_event, _headers, _isBinaryResponse, _list);
		}

		public void Request(string _event, bool _isBinaryResponse, params object[] _list)
		{
            if (_state != STATE_IDLE)
			{
				QueuedRequest(_event, _isBinaryResponse, _list);
				return;
			}

            RequestReal(_event, null, _isBinaryResponse, _list);
		}

		public void RequestPriority(string _event, bool _isBinaryResponse, params object[] _list)
		{
			if (_state != STATE_IDLE)
			{
				InsertRequest(_event, _isBinaryResponse, _list);
				return;
			}

			RequestReal(_event, null, _isBinaryResponse, _list);
		}

		public void RequestNoQueue(string _event, bool _isBinaryResponse, params object[] _list)
		{
			if (_state != STATE_IDLE)
			{
				return;
			}

			RequestReal(_event, null, _isBinaryResponse, _list);
		}

		private void RequestReal(string _event, List<ItemMultiTextEntry> _headers, bool _isBinaryResponse, params object[] _list)
		{
            this._event = _event;
            _commRequest = (IHTTPComms)Activator.CreateInstance(Type.GetType(this._event));

			ChangeState(STATE_COMMUNICATION);
			string data = _commRequest.Build(_list);
			if (DEBUG_LOG)
			{
                yourvrexperience.Utils.Utilities.DebugLogColor("CommController::RequestReal:URL=" + _commRequest.UrlRequest, Color.red);
                yourvrexperience.Utils.Utilities.DebugLogColor("CommController::RequestReal:data=" + data, Color.red);
			}
			UnityWebRequest www;
			switch (_commRequest.Method)
			{
				case BaseDataHTTP.METHOD_GET:
					www = UnityWebRequest.Get(_commRequest.UrlRequest + data);
					_currentWWW = www;
					if (_headers != null)
					{
						for (int i = 0; i < _headers.Count; i++)
						{
							if (_headers[i].Items.Count > 1)
							{
								string type = _headers[i].Items[0];
								string content = _headers[i].Items[1];
								www.SetRequestHeader(type, content);
							}
						}
					}                

					if (_isBinaryResponse)
					{
						StartCoroutine(WaitForUnityWebRequest(www));
					}
					else
					{
						StartCoroutine(WaitForUnityWebStringRequest(www));
					}
					break;

				case BaseDataHTTP.METHOD_POST:
					if ((_commRequest.RawData != null) && (_commRequest.RawData.Length > 0))
					{
						www = new UnityWebRequest(_commRequest.UrlRequest, "POST");					
						_currentWWW = www;
						www.uploadHandler = new UploadHandlerRaw(_commRequest.RawData);
						www.uploadHandler.contentType = "application/octet-stream";
						www.downloadHandler = new DownloadHandlerBuffer();
						www.disposeDownloadHandlerOnDispose = true;
						www.disposeUploadHandlerOnDispose = true;
						www.disposeCertificateHandlerOnDispose = true;
						if (_headers != null)
						{
							for (int i = 0; i < _headers.Count; i++)
							{
								if (_headers[i].Items.Count > 1)
								{
									string type = _headers[i].Items[0];
									string content = _headers[i].Items[1];
									www.SetRequestHeader(type, content);
								}
							}
						}

						if (_isBinaryResponse)
						{
							StartCoroutine(WaitForUnityWebRequest(www));
						}
						else
						{
							StartCoroutine(WaitForUnityWebStringRequest(www));
						}
					}
					else
					{
						www = null;
						if (_commRequest.FormPost != null)
						{
							www = UnityWebRequest.Post(_commRequest.UrlRequest, _commRequest.FormPost);
						}
						else
						{
							www = UnityWebRequest.Post(_commRequest.UrlRequest, _commRequest.FormData);
						}

						_currentWWW = www;
						if (_headers != null)
						{
							for (int i = 0; i < _headers.Count; i++)
							{
								if (_headers[i].Items.Count > 1)
								{
									string type = _headers[i].Items[0];
									string content = _headers[i].Items[1];
									www.SetRequestHeader(type, content);
								}
							}
						}                

						if (_isBinaryResponse)
						{
							StartCoroutine(WaitForUnityWebRequest(www));
						}
						else
						{
							StartCoroutine(WaitForUnityWebStringRequest(www));
						}
					}
					break;

				case BaseDataHTTP.METHOD_CANCEL:
					ChangeState(STATE_IDLE);
					ProcesQueuedComms();
					break;
			}
		}

		public void DelayRequest(string _nameEvent, bool _isBinaryResponse, float _time, params object[] _list)
		{
			_listTimedEvents.Add(new CommEventData(_nameEvent, null, _isBinaryResponse, _time, _list));
		}

		public void DelayRequestHeader(string _nameEvent, List<ItemMultiTextEntry> _headers, bool _isBinaryResponse, float _time, params object[] _list)
		{
			_listTimedEvents.Add(new CommEventData(_nameEvent, _headers, _isBinaryResponse, _time, _list));
		}

		public void QueuedRequest(string _nameEvent, bool _isBinaryResponse, params object[] _list)
		{
            _listQueuedEvents.Add(new CommEventData(_nameEvent, null, _isBinaryResponse, 0, _list));
		}

        public void QueuedRequest(string _nameEvent, List<ItemMultiTextEntry> _headers, bool _isBinaryResponse, params object[] _list)
        {
            _listQueuedEvents.Add(new CommEventData(_nameEvent, _headers, _isBinaryResponse, 0, _list));
        }

        public void InsertRequest(string _nameEvent, bool _isBinaryResponse, params object[] _list)
		{
			_priorityQueuedEvents.Insert(0, new CommEventData(_nameEvent, null, _isBinaryResponse, 0, _list));
		}
		public void InsertRequest(string _nameEvent, List<ItemMultiTextEntry> _headers, bool _isBinaryResponse, params object[] _list)
		{
			_priorityQueuedEvents.Insert(0, new CommEventData(_nameEvent, _headers, _isBinaryResponse, 0, _list));
		}

		IEnumerator WaitForRequest(WWW www)
		{
			yield return www;

			// check for errors
			try
			{
				if (www.error == null)
				{
					if (DEBUG_LOG) Debug.Log("WWW Ok!: " + www.text);
					_commRequest.Response("Error::" + www.text);
				}
				else
				{
					if (DEBUG_LOG) Debug.LogError("WWW Error: " + www.error);
					_commRequest.Response(Encoding.ASCII.GetBytes(www.error));
				}
			}
			catch (Exception err)
			{
				if (DEBUG_LOG) yourvrexperience.Utils.Utilities.DebugLogColor("CommController::WaitForRequest::stacktrace=" + err.StackTrace, Color.red);
			}

			ChangeState(STATE_IDLE);
			ProcesQueuedComms();
		}

        IEnumerator WaitForUnityWebRequest(UnityWebRequest www)
        {
            yield return www.SendWebRequest();

			try
			{
				if (www.isNetworkError || www.isHttpError)
				{
					if (DEBUG_LOG) Debug.LogError("WWW Error: " + www.error);
					_commRequest.Response("Error::" + www.error);
				}
				else
				{
					if (DEBUG_LOG) Debug.Log("WWW Ok!: " + www.downloadHandler.text);
					_commRequest.Response(www.downloadHandler.data);
				}
			}
			catch (Exception err)
			{
				if (DEBUG_LOG) yourvrexperience.Utils.Utilities.DebugLogColor("CommController::WaitForUnityWebRequest::stacktrace=" + err.StackTrace, Color.red);
			}

			if (_currentWWW == www)
			{
				_currentWWW = null;
			}
            ChangeState(STATE_IDLE);
            ProcesQueuedComms();
        }
        

        IEnumerator WaitForStringRequest(WWW www)
		{
			yield return www;

			// check for errors
			try
			{
				if (www.error == null)
				{
					if (DEBUG_LOG) Debug.Log("WWW Ok!: " + www.text);
					_commRequest.Response("Error::" + www.text);
				}
				else
				{
					if (DEBUG_LOG) Debug.LogError("WWW Error: " + www.error);
					_commRequest.Response(Encoding.ASCII.GetBytes(www.error));
				}
			}
			catch (Exception err)
			{
				if (DEBUG_LOG) yourvrexperience.Utils.Utilities.DebugLogColor("CommController::WaitForStringRequest::stacktrace=" + err.StackTrace, Color.red);
			}

			ChangeState(STATE_IDLE);
			ProcesQueuedComms();
		}

        IEnumerator WaitForUnityWebStringRequest(UnityWebRequest www)
        {
            yield return www.SendWebRequest();

			try
			{
				if (www.isNetworkError || www.isHttpError)
				{
					if (DEBUG_LOG) Debug.LogError("WWW Error: " + www.error);
					_commRequest.Response("Error::" + www.error);
				}
				else
				{
					if (DEBUG_LOG) Debug.Log("WWW Ok!: " + www.downloadHandler.text);
					_commRequest.Response(www.downloadHandler.text);
				}
			}
			catch (Exception err)
			{
				if (DEBUG_LOG) yourvrexperience.Utils.Utilities.DebugLogColor("CommController::WaitForUnityWebStringRequest::stacktrace=" + err.StackTrace, Color.red);
			}

			if (_currentWWW == www)
			{
				_currentWWW = null;
			}

            ChangeState(STATE_IDLE);
            ProcesQueuedComms();
        }

        public void DisplayLog(string _data)
		{
			_inGameLog = _data + "\n";
			if (DEBUG_LOG)
			{
				Debug.Log("CommController::DisplayLog::DATA=" + _data);
			}
		}

		public void ClearLog()
		{
			_inGameLog = "";
		}

		private bool m_enableLog = true;

		void OnGUI()
		{
			if (DEBUG_LOG)
			{
				if (!m_enableLog)
				{
					if (_inGameLog.Length > 0)
					{
						ClearLog();
					}
				}

				if (m_enableLog)
				{
					if (_inGameLog.Length > 0)
					{
						GUILayout.BeginScrollView(Vector2.zero);
						if (GUILayout.Button(_inGameLog))
						{
							ClearLog();
						}
						GUILayout.EndScrollView();
					}
					else
					{
						switch (_state)
						{
							case STATE_IDLE:
								break;

							case STATE_COMMUNICATION:
								GUILayout.BeginScrollView(Vector2.zero);
								GUILayout.Label("COMMUNICATION::Event=" + _event);
								GUILayout.EndScrollView();
								break;
						}
					}
				}
			}
		}

		private void ProcessTimedEvents()
		{
			switch (_state)
			{
				case STATE_IDLE:
					for (int i = 0; i < _listTimedEvents.Count; i++)
					{
						CommEventData eventData = _listTimedEvents[i];
						eventData.Time -= Time.deltaTime;
						if (eventData.Time <= 0)
						{
							_listTimedEvents.RemoveAt(i);							
							RequestReal(eventData.NameEvent, eventData.Headers, eventData.IsBinaryResponse, eventData.List);
							eventData.Destroy();
							break;
						}
					}
					break;
			}
		}

		private void ProcesQueuedComms()
		{
			// PRIORITY QUEUE
			if (_priorityQueuedEvents.Count > 0)
			{
				int i = 0;
				CommEventData eventData = _priorityQueuedEvents[i];
				_priorityQueuedEvents.RemoveAt(i);
				RequestReal(eventData.NameEvent, eventData.Headers, eventData.IsBinaryResponse, eventData.List);
				eventData.Destroy();
				return;
			}
			// NORMAL QUEUE
			if (_listQueuedEvents.Count > 0)
			{
				int i = 0;
				CommEventData eventData = _listQueuedEvents[i];
				_listQueuedEvents.RemoveAt(i);
				RequestReal(eventData.NameEvent, eventData.Headers, eventData.IsBinaryResponse, eventData.List);
				eventData.Destroy();
				return;
			}
		}

		private void ProcessQueueEvents()
		{
			switch (_state)
			{
				case STATE_IDLE:
					break;

				case STATE_COMMUNICATION:
					break;
			}
		}

		public void GetFileData(string url)
		{			
			Request(EVENT_COMM_GET_FILE_DATA, false, url);
		}

		public void DownloadURL(string eventName, string url)
		{
			StartCoroutine(DownloadURLCouroutine(eventName, url));
		}

		IEnumerator DownloadURLCouroutine(string eventName, string url)
		{			
			using (UnityWebRequest www = UnityWebRequest.Get(url))
			{
				yield return www.SendWebRequest();

				if (www.result != UnityWebRequest.Result.Success)
				{
					SystemEventController.Instance.DispatchSystemEvent(eventName, false);
					yield break;
				}

				byte[] receivedBytes = www.downloadHandler.data;

				if ((receivedBytes == null) || (receivedBytes.Length == 0))
				{
					SystemEventController.Instance.DispatchSystemEvent(eventName, false);
				}
				else
				{
					SystemEventController.Instance.DispatchSystemEvent(eventName, true, System.Text.Encoding.UTF8.GetString(receivedBytes));
				}
			}
		}

		public void Update()
		{
			ProcessTimedEvents();
			ProcessQueueEvents();
		}
	}

}
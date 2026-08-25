using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace yourvrexperience.Utils
{
    public class SystemEventController : MonoBehaviour, IDontDestroy
    {
		public const string EventSystemEventControllerDontDestroyOnLoad = "EventSystemEventControllerDontDestroyOnLoad";
		public const string EventSystemEventControllerReleaseAllResources = "EventSystemEventControllerReleaseAllResources";

		public const string EventSystemEventControllerClearInGameLog = "EventSystemEventControllerClearInGameLog";
		public const string EventSystemEventControllerAddTextLog = "EventSystemEventControllerAddTextLog";

		private static SystemEventController _instance;
        public static SystemEventController Instance
        {
            get
            {
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(SystemEventController)) as SystemEventController;
				}
				return _instance;
            }
        }

		public bool CanDestroyOnLoad 
		{
			get { return false; }
		}

		public delegate void SystemEvent(string nameEvent, params object[] parameters);

        public event SystemEvent Event;

		private List<TimedEventData> _listEvents = new List<TimedEventData>();

        public void DispatchSystemEvent(string nameEvent, params object[] parameters)
        {
            if (Event != null) Event(nameEvent, parameters);
        }

		public void DelaySystemEvent(string nameEvent, float time, params object[] list)
		{
            if (_instance == null) return;

            _listEvents.Add(new TimedEventData(nameEvent, -1, -1, time, list));
		}

		public void ClearSystemEvents(params string[] events)
		{
			if ((events == null) || (events.Length == 0))
			{
				for (int k = 0; k < _listEvents.Count; k++)
				{
					_listEvents[k].Destroy();
				}
				_listEvents.Clear();
			}
			else
			{
				for (int i = 0; i < events.Length; i++)
				{
					string nameEvent = events[i];
					List<TimedEventData> eventToRemove = _listEvents.Where(x => x.NameEvent == nameEvent).ToList();
					for (int j = 0; j < eventToRemove.Count; j++)
					{
						eventToRemove[j].Destroy();
					}
					_listEvents.RemoveAll(x => x.NameEvent == nameEvent);
				}
			}
		}

		void Start()
		{
			DontDestroyOnLoad(this.gameObject);
			Event += OnSystemEvent;
		}

		void OnDestroy()
		{
			Event -= OnSystemEvent;
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(EventSystemEventControllerReleaseAllResources))
			{
				_instance = null;
				GameObject.Destroy(this.gameObject);
			}
			if (nameEvent.Equals(EventSystemEventControllerDontDestroyOnLoad))
			{
				if (Instance)
				{
					DontDestroyOnLoad(Instance.gameObject);
				}
			}
		}

		void Update()
        {
            if (_instance == null) return;

            // DELAYED EVENTS
            for (int i = 0; i < _listEvents.Count; i++)
            {
                TimedEventData eventData = _listEvents[i];
                if (eventData.Time == -1000)
                {
                    eventData.Destroy();
                    _listEvents.RemoveAt(i);
                    break;
                }
                else
                {
                    eventData.Time -= Time.deltaTime;
                    if (eventData.Time <= 0)
                    {
						_listEvents.RemoveAt(i);
						if ((eventData != null) && (Event != null))
                        {
                            Event(eventData.NameEvent, eventData.Parameters);
                            eventData.Destroy();
                        }                        
                        break;
                    }
                }
            }
        }
    }
}
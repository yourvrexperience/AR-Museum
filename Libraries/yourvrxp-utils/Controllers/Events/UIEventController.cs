using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace yourvrexperience.Utils
{
    public class UIEventController : MonoBehaviour, IDontDestroy
    {
        private static UIEventController _instance;
        public static UIEventController Instance
        {
            get
            {
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(UIEventController)) as UIEventController;
				}
				return _instance;
            }
        }

		public bool CanDestroyOnLoad 
		{
			get { return false; }
		}

        public delegate void UIEvent(string nameEvent, params object[] parameters);

        public event UIEvent Event;

		private List<TimedEventData> m_listEvents = new List<TimedEventData>();

        public void DispatchUIEvent(string nameEvent, params object[] parameters)
        {
            if (Event != null) Event(nameEvent, parameters);
        }

		public void DelayUIEvent(string nameEvent, float time, params object[] list)
		{
            m_listEvents.Add(new TimedEventData(nameEvent, -1, -1, time, list));
		}

		public void ClearUIEvents(params string[] events)
		{
			if ((events == null) || (events.Length == 0))
			{
				for (int k = 0; k < m_listEvents.Count; k++)
				{
					m_listEvents[k].Destroy();
				}
				m_listEvents.Clear();
			}
			else
			{
				for (int i = 0; i < events.Length; i++)
				{
					string nameEvent = events[i];
					List<TimedEventData> eventToRemove = m_listEvents.Where(x => x.NameEvent == nameEvent).ToList();
					for (int j = 0; j < eventToRemove.Count; j++)
					{
						eventToRemove[j].Destroy();
					}
					m_listEvents.RemoveAll(x => x.NameEvent == nameEvent);
				}
			}
		}
		void Start()
		{
			DontDestroyOnLoad(this.gameObject);
		 	SystemEventController.Instance.Event += OnSystemEvent;
		}

		void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerReleaseAllResources))
			{
				_instance = null;
				GameObject.Destroy(this.gameObject);
			}
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))
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
            for (int i = 0; i < m_listEvents.Count; i++)
            {
                TimedEventData eventData = m_listEvents[i];
                if (eventData.Time == -1000)
                {
                    eventData.Destroy();
                    m_listEvents.RemoveAt(i);
                    break;
                }
                else
                {
                    eventData.Time -= Time.deltaTime;
                    if (eventData.Time <= 0)
                    {
						m_listEvents.RemoveAt(i);
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

using System;
using UnityEngine;

namespace yourvrexperience.Utils
{
    public class ObjectReporter : MonoBehaviour
    {
		public const string EventObjectReporterResponse = "EventObjectReporterResponse";
		public const string EventObjectReporterRequest = "EventObjectReporterRequest";

		[SerializeField] private string Data;

        void Start()
        {
			SystemEventController.Instance.Event += OnSystemEvent;
            SystemEventController.Instance.DispatchSystemEvent(EventObjectReporterResponse, this.gameObject, Data);
        }

		void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(EventObjectReporterRequest))
			{
				SystemEventController.Instance.DispatchSystemEvent(EventObjectReporterResponse, this.gameObject, Data);
			}
		}
	}
}
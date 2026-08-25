using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.Utils
{
	public class ButtonOpenLink : MonoBehaviour
	{
		public static bool IsEnabled = true;

        public const string EventButtonOpenLinkChangeURL = "EventButtonOpenLinkChangeURL";

        [SerializeField]
		private string url;

        private void Awake()
        {
			this.gameObject.SetActive(IsEnabled);
			SystemEventController.Instance.Event += OnSystemEvent;
        }

        private void OnDestroy()
        {
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(EventButtonOpenLinkChangeURL))
            {
                url = (string)parameters[0];
            }
        }

        public void OpenLink()
		{
			OpenLinkController.OpenLinkJSPluginNewTab(url);
		}
	}
}
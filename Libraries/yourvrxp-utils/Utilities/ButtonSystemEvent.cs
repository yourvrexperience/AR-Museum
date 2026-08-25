using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.Utils
{
	public class ButtonSystemEvent : MonoBehaviour
	{
        public const string EventButtonSystemEventClicked = "EventButtonSystemEventClicked";

        [SerializeField]
        private Button button;

        [SerializeField]
		private string eventName;

        public Button Button
        {
            get { return button; }
            set { button = value; }
        }
        public string EventName
        {
            get { return eventName; }
            set { eventName = value; }
        }

        private void Awake()
        {
            if (button != null)
            {
                Initialize();
            }            
        }

        public void Initialize()
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            SystemEventController.Instance.DispatchSystemEvent(EventButtonSystemEventClicked, eventName, button);
        }
	}
}
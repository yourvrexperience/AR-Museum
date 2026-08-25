using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace yourvrexperience.VR
{
	public abstract class HandMenuBase : MonoBehaviour, IHandMenu
	{
		public const string EventHandMenuBaseBack = "EventHandMenuBaseBack";

        public event HandMenuBaseEvent Event;

        public void DispatchHandMenuBaseEvent(string nameEvent, params object[] parameters)
        {
            if (Event != null) Event(nameEvent, parameters);
        }

		[SerializeField] protected GameObject content;
		[SerializeField] protected Button backButton;

		protected virtual void Start()
		{
			backButton.onClick.AddListener(OnBackPressed);
		}

		protected virtual void OnBackPressed()
		{
			DispatchHandMenuBaseEvent(EventHandMenuBaseBack);
		}

		public virtual void Activation(bool activate)
		{
			content.SetActive(activate);
		}
	}
}
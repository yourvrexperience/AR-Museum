using yourvrexperience.Utils;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace yourvrexperience.template6dof
{
	public class NavigationLineView : MonoBehaviour
	{
		public const string EventNavigationLineViewDestroy = "EventNavigationLineViewDestroy";

		void Start()
		{
			SystemEventController.Instance.Event += OnSystemEvent;
		}

		void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(MainController.EventMainControllerReleaseGameResources))
			{
				GameObject.Destroy(this.gameObject);
			}
			if (nameEvent.Equals(EventNavigationLineViewDestroy))
			{
				GameObject.Destroy(this.gameObject);
			}
        }
    }
}

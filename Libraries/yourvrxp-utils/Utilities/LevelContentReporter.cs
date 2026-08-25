using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.Utils
{
	public class LevelContentReporter : MonoBehaviour
	{
		public const string EventLevelContentReporterStarted = "EventLevelContentReporterStarted";
		
		public GameObject[] ActivateObjects;
		
		void Start()
		{
			SystemEventController.Instance.DispatchSystemEvent(EventLevelContentReporterStarted, this.gameObject);
			Invoke("ActivationObjects", 0.25f);
		}

		private void ActivationObjects()
		{
			if (ActivateObjects != null)
			{
				if (ActivateObjects.Length > 0)
				{
					foreach (GameObject item in ActivateObjects)
					{
						if (item != null)
						{
							if (!item.activeSelf)
							{
								item.SetActive(true);
							}
						}
					}
				}
			}
		}
	}
}
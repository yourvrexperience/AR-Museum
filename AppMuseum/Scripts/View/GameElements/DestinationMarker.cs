using yourvrexperience.Utils;
using UnityEngine;
using System;
using System.Collections.Generic;
using yourvrexperience.Narration;

namespace yourvrexperience.template6dof
{
	[RequireComponent(typeof(FaceCamera))]
	public class DestinationMarker : MonoBehaviour
	{
		public const string EventDestinationMarkerDestroy = "EventDestinationMarkerDestroy";

        public void Initialize(Transform target)
        {
			this.transform.position = target.transform.position;
		}

		void Start()
		{			
			SystemEventController.Instance.Event += OnSystemEvent;
		}

		private void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(EventDestinationMarkerDestroy))
			{
				GameObject.Destroy(this.gameObject);
			}
        }

        void Update()
        {
			if (MainController.Instance.PlayerView != null)
            {
				float distanceToPlayer = 0;
				if (MainController.Instance.IsNormalAxis)
				{
					distanceToPlayer = yourvrexperience.Utils.Utilities.DistanceXZ(MainController.Instance.PlayerView.gameObject.transform.position, this.transform.position);
				}
				else
				{
					distanceToPlayer = yourvrexperience.Utils.Utilities.DistanceXY(MainController.Instance.PlayerView.gameObject.transform.position, this.transform.position);
				}
				if (distanceToPlayer < GameLevelData.Instance.DistanceToTriggerGuide)
				{					
					SystemEventController.Instance.DispatchSystemEvent(EventDestinationMarkerDestroy);					
				}
			}
		}
	}
}

using yourvrexperience.Utils;
using UnityEngine;
using System;
using System.Collections.Generic;
using yourvrexperience.Narration;

namespace yourvrexperience.template6dof
{
	public class WaypointToNextTarget : MonoBehaviour
	{
		[SerializeField] private MeshRenderer mesh;
		[SerializeField] private GameObject nextTarget;

		private bool _activated = false;

        private void Start()
        {
			if (mesh != null)
			{
#if UNITY_EDITOR
				mesh.enabled = true;
#else
				mesh.enabled = false;
#endif
			}
		}

		public void GoToWaypoint()
        {
			_activated = true;
		}

		void Update()
        {
			if (_activated && MainController.Instance.PlayerView != null)
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
				if (nextTarget != null)
                {
					if (distanceToPlayer < GameLevelData.Instance.DistanceToTriggerGuide)
					{
						SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventShowArrowPath, nextTarget);
					}
				}
			}
		}
	}
}

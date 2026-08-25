using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace yourvrexperience.Utils
{
	public class RefocusScreen : MonoBehaviour
    {
		private Camera _targetCamera;
		private bool _refocus = false;
		private float _timeToRefocus = 0;
		private float _timeoutToRefocus = 4;	
		private float _timeAnimation = 1;	
		private float _distance;

        public void Activate(Camera targetCamera, float distance, float timeoutToRefocus = 4, float timeAnimation = 1)
		{
			_targetCamera = targetCamera;
			_distance = distance;
			_refocus = true;
			_timeToRefocus = 0;
			_timeoutToRefocus = timeoutToRefocus;
			_timeAnimation = timeAnimation;
		}

		private void UpdateForward()
		{
			this.transform.forward = _targetCamera.transform.forward;
		}

		void Update()
		{
			if (_refocus)
			{
				_timeToRefocus += Time.deltaTime;
				if (_timeToRefocus > _timeoutToRefocus)
				{
					Bounds canvasBounds = new Bounds(this.gameObject.transform.position, Vector3.one);					
					if ((!yourvrexperience.Utils.Utilities.IsVisibleFrom(canvasBounds, _targetCamera)) || (Vector3.Distance(_targetCamera.transform.position, this.transform.position) > _distance * 2))
					{
						_timeToRefocus = -_timeAnimation;
						Vector3 position = _targetCamera.transform.position + _targetCamera.transform.forward * _distance;
						iTween.MoveTo(gameObject, iTween.Hash("x", position.x, "y", position.y, "z", position.z, "time", _timeAnimation, "onupdate", "UpdateForward"));
					}
					else
					{
						_timeToRefocus = 0;
					}
				}
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;

namespace yourvrexperience.Utils
{
	public class InterpolatorController : MonoBehaviour
	{
		public const string EventInterpolateStarted   = "EventInterpolateStarted";
        public const string EventInterpolateCompleted = "EventInterpolateCompleted";
        public const string EventInterpolateFreeze    = "EventInterpolateFreeze";
        public const string EventInterpolateResume    = "EventInterpolateResume";


        public const int TypeInterpolatePosition = 0;
        public const int TypeInterpolateForward  = 1;
        public const int TypeInterpolateRotation  = 2;
        public const int TypeInterpolateScale  = 3;

		private static InterpolatorController _instance;
		public static InterpolatorController Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(InterpolatorController)) as InterpolatorController;
					if (!_instance)
					{
						GameObject container = new GameObject();
						container.name = "InterpolatorController";
						_instance = container.AddComponent(typeof(InterpolatorController)) as InterpolatorController;
					}
				}
				return _instance;
			}
		}

		public delegate void InterpolationEvent(string nameEvent, params object[] parameters);

        public event InterpolationEvent Event;

        public void DispatchInterpolationEvent(string nameEvent, params object[] parameters)
        {
            if (Event != null) Event(nameEvent, parameters);
        }

        public bool EnableOnUpdate = true;

        private List<IInterpolateData> _inteporlateObjects = new List<IInterpolateData>();
        private List<IInterpolateData> _inteporlateQueue = new List<IInterpolateData>();

        void OnDestroy()
		{
			Destroy();
		}

		public void Destroy()
		{
			if (_instance != null)
			{
				GameObject.Destroy(_instance);
			}
			_instance = null;
		}

		public bool Stop(GameObject actor)
		{
			bool found = false;
			for (int i = 0; i < _inteporlateObjects.Count; i++)
			{
                IInterpolateData item = _inteporlateObjects[i];
				if (item.GameActor == actor)
				{
					item.Destroy();
					_inteporlateObjects.RemoveAt(i);
					found = true;
				}
			}
			return found;
		}

        public bool StopAll()
        {
            for (int i = 0; i < _inteporlateObjects.Count; i++)
            {
                IInterpolateData item = _inteporlateObjects[i];
                item.Destroy();
                _inteporlateObjects.RemoveAt(i);
            }
            return false;
        }

        public void Interpolate(GameObject actor, Vector3 goal, float time, bool setTargetWhenFinished = false, bool loop = false)
        {
            _inteporlateQueue.Add(new InterpolatePositionData(actor, actor.transform.position, goal, time, 0, setTargetWhenFinished, loop));
        }

        public void InterpolatePosition(GameObject actor, Vector3 goal, float time, bool setTargetWhenFinished = false, bool loop = false)
		{
            _inteporlateQueue.Add(new InterpolatePositionData(actor, actor.transform.position, goal, time, 0, setTargetWhenFinished, loop));
		}

        public void InterpolateForward(GameObject actor, Vector3 goal, float time, bool setTargetWhenFinished = false)
        {
            _inteporlateQueue.Add(new InterpolateForwardData(actor, actor.transform.forward, goal, time, 0, setTargetWhenFinished));
        }

        public void InterpolateEuler(GameObject actor, Vector3 goal, float time, bool setTargetWhenFinished = false)
        {
            _inteporlateQueue.Add(new InterpolateEulerData(actor, actor.transform.eulerAngles, goal, time, 0, setTargetWhenFinished));
        }

        public void InterpolateRotation(GameObject actor, Quaternion goal, float time, bool setTargetWhenFinished = false)
        {
            _inteporlateQueue.Add(new InterpolateRotationData(actor, actor.transform.rotation, goal, time, 0, setTargetWhenFinished));
        }

        public void InterpolateScale(GameObject actor, Vector3 goal, float time, bool setTargetWhenFinished = false)
        {
            _inteporlateQueue.Add(new InterpolateScaleData(actor, actor.transform.localScale, goal, time, 0, setTargetWhenFinished));
        }

        public void Logic()
        {
            try
            {
                for (int i = 0; i < _inteporlateObjects.Count; i++)
                {
                    IInterpolateData itemData = _inteporlateObjects[i];
                    if (itemData.Inperpolate())
                    {
                        itemData.Destroy();
                        _inteporlateObjects.RemoveAt(i);
                        i--;
                    }
                }
            }
            catch (Exception err) { };
            for (int j = 0; j < _inteporlateQueue.Count; j++)
            {
                IInterpolateData newItem = _inteporlateQueue[j];
                bool found = false;
                for (int i = 0; i < _inteporlateObjects.Count; i++)
                {
                    IInterpolateData item = _inteporlateObjects[i];
                    if ((item.GameActor == newItem.GameActor) && (item.TypeData == newItem.TypeData))
                    {
                        item.ResetData(newItem.GameActor.transform, newItem.Goal, newItem.TotalTime, 0);
                        found = true;
                    }
                }
                if (!found)
                {
                    _inteporlateObjects.Add(newItem);
                }
                else
                {
                    newItem.Destroy();
                    newItem = null;
                }
            }
            _inteporlateQueue.Clear();
        }

        void Update()
		{
            if (EnableOnUpdate) Logic();
        }
	}
}
using System;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.Utils
{

    public class InterpolateEulerData : IEquatable<InterpolatePositionData>, IInterpolateData
	{
        private GameObject _gameActor;
		private Vector3 _origin;
		private Vector3 _goal;
		private float _totalTime;
		private float _timeDone;
        private bool _activated;
        private bool _setTargetWhenFinished;
        private bool _firstRun = true;

        public GameObject GameActor
		{
			get { return _gameActor; }
		}
		public object Goal
		{
			get { return _goal; }
			set { _goal = (Vector3)value; }
		}
		public float TotalTime
		{
			get { return _totalTime; }
			set { _totalTime = value; }
		}
		public float TimeDone
		{
			get { return _timeDone; }
			set { _timeDone = 0; }
		}
        public bool SetTargetWhenFinished
        {
            get { return _setTargetWhenFinished; }
            set { _setTargetWhenFinished = value; }
        }
        public int TypeData
        {
            get { return InterpolatorController.TypeInterpolateForward; }
        }

        public InterpolateEulerData(GameObject actor, Vector3 origin, Vector3 goal, float totalTime, float timeDone, bool setTargetWhenFinished)
		{
			_gameActor = actor;
            _activated = true;
            _setTargetWhenFinished = setTargetWhenFinished;

            ResetData(_gameActor.transform, goal, totalTime, timeDone);

            InterpolatorController.Instance.Event += OnInterpolatorEvent;
		}

        public void ResetData(Transform origin, object goal, float totalTime, float timeDone)
		{
			_origin = new Vector3(origin.eulerAngles.x, origin.eulerAngles.y, origin.eulerAngles.z);
            Vector3 sgoal = (Vector3)goal;
            _goal = new Vector3(sgoal.x, sgoal.y, sgoal.z);
			_totalTime = totalTime;
			_timeDone = timeDone;
            _activated = true;
        }

		public void Destroy()
		{
			_gameActor = null;
            InterpolatorController.Instance.Event -= OnInterpolatorEvent;
        }

		public bool Inperpolate()
		{
            if (!_activated) return false;
			if (_gameActor == null) return true;

            if (_firstRun)
            {
                _firstRun = false;
                InterpolatorController.Instance.DispatchInterpolationEvent(InterpolatorController.EventInterpolateStarted, _gameActor);
            }

            _timeDone += Time.deltaTime;
            if (_timeDone <= _totalTime)
			{
				Vector3 eulerTarget = (_goal - _origin);
				float increaseFactor = (1 - ((_totalTime - _timeDone) / _totalTime));
				_gameActor.transform.eulerAngles = _origin + (increaseFactor * eulerTarget);
				return false;
			}
			else
			{
				if (_timeDone <= _totalTime)
				{
					return false;
				}
				else
				{
                    if (_setTargetWhenFinished)
                    {
                        _gameActor.transform.forward = _goal;
                    }
					InterpolatorController.Instance.DispatchInterpolationEvent(InterpolatorController.EventInterpolateCompleted, _gameActor);
					return true;
				}
			}
		}

		public bool Equals(InterpolatePositionData other)
		{
			return _gameActor == other.GameActor;
		}

        private void OnInterpolatorEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(InterpolatorController.EventInterpolateFreeze))
            {
                GameObject target = (GameObject)parameters[0];
                if (target == _gameActor)
                {
                    _activated = false;
                }
            }
            if (nameEvent.Equals(InterpolatorController.EventInterpolateResume))
            {
                GameObject target = (GameObject)parameters[0];
                if (target == _gameActor)
                {
                    _activated = true;
                }
            }
        }
    }
}
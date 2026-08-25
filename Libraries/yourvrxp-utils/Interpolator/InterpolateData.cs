using System;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.Utils
{
	public class InterpolateData : IEquatable<InterpolateData>
	{
        private GameObject _gameActor;
		private Vector3 _origin;
		private Vector3 _goal;
		private float _totalTime;
		private float _timeDone;
        private bool _activated;
        private bool _setTargetWhenFinished;
        private bool _firstRun = true;
        private float _delay = 0;

        public GameObject GameActor
		{
			get { return _gameActor; }
		}
		public Vector3 Goal
		{
			get { return _goal; }
			set { _goal = value; }
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

        public InterpolateData(GameObject actor, Vector3 origin, Vector3 goal, float totalTime, float timeDone, bool setTargetWhenFinished, float delay = 0)
		{
			_gameActor = actor;
            _activated = true;
            _setTargetWhenFinished = setTargetWhenFinished;
            _delay = delay;

            ResetData(origin, goal, totalTime, timeDone);

            InterpolatorController.Instance.Event += OnInterpolateEvent;
		}

        public void ResetData(Vector3 origin, Vector3 goal, float totalTime, float timeDone)
		{
			_origin = new Vector3(origin.x, origin.y, origin.z);
			_goal = new Vector3(goal.x, goal.y, goal.z);
			_totalTime = totalTime;
			_timeDone = timeDone;
            _activated = true;
        }

		public void Destroy()
		{
			_gameActor = null;
            InterpolatorController.Instance.Event -= OnInterpolateEvent;
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

            if (_delay > 0)
            {
                _delay -= Time.deltaTime;
                return false;
            }

            _timeDone += Time.deltaTime;
            if (_timeDone <= _totalTime)
			{
				Vector3 forwardTarget = (_goal - _origin);
				float increaseFactor = (1 - ((_totalTime - _timeDone) / _totalTime));
				_gameActor.transform.position = _origin + (increaseFactor * forwardTarget);
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
                        _gameActor.transform.position = _goal;
                    }
					InterpolatorController.Instance.DispatchInterpolationEvent(InterpolatorController.EventInterpolateCompleted, _gameActor);
					return true;
				}
			}
		}

		public bool Equals(InterpolateData other)
		{
			return _gameActor == other.GameActor;
		}

        private void OnInterpolateEvent(string nameEvent, object[] parameters)
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
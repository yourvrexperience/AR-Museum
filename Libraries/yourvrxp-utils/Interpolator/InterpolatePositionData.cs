using System;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.Utils
{
	public class InterpolatePositionData : IEquatable<InterpolatePositionData>, IInterpolateData
    {
        private GameObject _gameActor;
		private Vector3 _origin;
		private Vector3 _goal;
		private float _totalTime;
		private float _timeDone;
        private bool _activated;
        private bool _setTargetWhenFinished;
        private bool _firstRun = true;
		private bool _loop = false;

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
            get { return InterpolatorController.TypeInterpolatePosition; }
        }

        public InterpolatePositionData(GameObject actor, Vector3 origin, Vector3 goal, float totalTime, float timeDone, bool setTargetWhenFinished, bool loop = false)
		{
			_gameActor = actor;
            _activated = true;
			_loop = loop;
            _setTargetWhenFinished = setTargetWhenFinished;

            ResetData(_gameActor.transform, goal, totalTime, timeDone);

			InterpolatorController.Instance.Event += OnInterpolatorEvent;
		}

        public void ResetData(Transform origin, object goal, float totalTime, float timeDone)
		{
			_origin = new Vector3(origin.position.x, origin.position.y, origin.position.z);
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
					if (_loop)
                    {
						_gameActor.transform.position = _origin;
						ResetData(_gameActor.transform, _goal, _totalTime, 0);
					}
					else
                    {
						InterpolatorController.Instance.DispatchInterpolationEvent(InterpolatorController.EventInterpolateCompleted, _gameActor);
						return true;
					}
					return false;
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
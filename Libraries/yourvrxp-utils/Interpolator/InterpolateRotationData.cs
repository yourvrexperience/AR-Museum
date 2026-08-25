using System;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.Utils
{
    public class InterpolateRotationData : IEquatable<InterpolatePositionData>, IInterpolateData
    {
        private GameObject _gameActor;
		private Vector4 _origin;
		private Vector4 _goal;
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
			set { _goal = (Vector4)value; }
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
            get { return InterpolatorController.TypeInterpolateRotation; }
        }

        public InterpolateRotationData(GameObject actor, Quaternion origin, Quaternion goal, float totalTime, float timeDone, bool setTargetWhenFinished)
		{
			_gameActor = actor;
            _activated = true;
            _setTargetWhenFinished = setTargetWhenFinished;

            ResetData(_gameActor.transform, new Vector4(goal.x, goal.y, goal.z, goal.w), totalTime, timeDone);
            
            InterpolatorController.Instance.Event += OnInterpolatorEvent;
		}

        public void ResetData(Transform origin, object goal, float totalTime, float timeDone)
		{
			_origin = new Vector4(origin.rotation.x, origin.rotation.y, origin.rotation.z, origin.rotation.w);
            Vector4 sgoal = (Vector4)goal;
            _goal = new Vector4(sgoal.x, sgoal.y, sgoal.z, sgoal.w);
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
				Vector4 quaternionTarget = (_goal - _origin);
				float increaseFactor = (1 - ((_totalTime - _timeDone) / _totalTime));
                Vector4 newRotation = _origin + (increaseFactor * quaternionTarget);
                _gameActor.transform.rotation = new Quaternion(newRotation.x, newRotation.y, newRotation.z, newRotation.w);
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
                        _gameActor.transform.rotation = new Quaternion(_goal.x, _goal.y, _goal.z, _goal.w);
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
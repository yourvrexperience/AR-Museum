using System;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.Utils
{
    public class AlphaData : IEquatable<AlphaData>
	{
        private GameObject  _gameActor;
		private float _origin;
		private float _goal;
		private float _totalTime;
		private float _timeDone;
        private bool _activated;
        private bool _setTargetWhenFinished;
        private bool _loop;

        public GameObject GameActor
		{
			get { return  _gameActor; }
		}
		public float Goal
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

        public AlphaData(GameObject actor, float origin, float goal, float totalTime, float timeDone, bool setTargetWhenFinished, bool loop)
		{
			 _gameActor = actor;
            _activated = true;
            _setTargetWhenFinished = setTargetWhenFinished;
            _loop = loop;

            ResetData(origin, goal, totalTime, timeDone);

            InterpolatorController.Instance.Event += OnInterpolateEvent;
		}

        public void ResetData(float origin, float goal, float totalTime, float timeDone)
		{
			_origin = origin;
			_goal = goal;
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
			if ( _gameActor == null) return true;

			_timeDone += Time.deltaTime;
            if (_timeDone <= _totalTime)
			{
				float forwardTarget = (_goal - _origin);
				float increaseFactor = (1 - ((_totalTime - _timeDone) / _totalTime));
				 _gameActor.GetComponent<CanvasGroup>().alpha = _origin + (increaseFactor * forwardTarget);
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
                         _gameActor.GetComponent<CanvasGroup>().alpha = _goal;
                    }
                    if (_loop)
                    {
                        _timeDone = 0;
                         _gameActor.GetComponent<CanvasGroup>().alpha = _origin;
                        return false;
                    }	
                    else
                    {
                        InterpolatorController.Instance.DispatchInterpolationEvent(InterpolatorController.EventInterpolateCompleted,  _gameActor);
                    }
					return true;
				}
			}
		}

		public bool Equals(AlphaData other)
		{
			return  _gameActor == other.GameActor;
		}


        private void OnInterpolateEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent == InterpolatorController.EventInterpolateFreeze)
            {
                GameObject target = (GameObject)parameters[0];
                if (target ==  _gameActor)
                {
                    _activated = false;
                }
            }
            if (nameEvent == InterpolatorController.EventInterpolateResume)
            {
                GameObject target = (GameObject)parameters[0];
                if (target ==  _gameActor)
                {
                    _activated = true;
                }
            }
        }
    }
}
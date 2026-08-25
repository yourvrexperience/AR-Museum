using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.Utils
{
    public class StateMachine : MonoBehaviour
    {
        protected int _state = 0;
        protected int _previousState = 0;

        protected float _timeCounter = 0;
		protected int _iterator = 0;

        public int State
        {
            get { return _state; }
            set { _state = value; }
        }

        protected virtual void ChangeState(int newState)
        {
            _previousState = _state;
            _state = newState;
            _timeCounter = 0;
			_iterator = 0;
        }

        protected virtual void RestorePreviousState()
        {
            _state = _previousState;
            _timeCounter = 0;
			_iterator = 0;
        }
    }
}
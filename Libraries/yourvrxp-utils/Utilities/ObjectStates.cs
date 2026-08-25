
using System;
using UnityEngine;

namespace yourvrexperience.Utils
{
	[RequireComponent(typeof(Collider))]
    public class ObjectStates : MonoBehaviour
    {
		public const string EventObjectStatesChangeState = "EventObjectStatesChangeState";

		public delegate void CollisionEnterDetectorEvent(GameObject collider, GameObject other);
		public delegate void CollisionExitDetectorEvent(GameObject collider, GameObject other);

		public event CollisionEnterDetectorEvent CollisionEnterEvent;
		public event CollisionExitDetectorEvent CollisionExitEvent;

		[SerializeField] private GameObject[] States;

		private int _currentState = 0;
		private string _name;

		public string Name
        {
            get { return _name; }
			set { _name = value; }
        }
		public int TotalStates
        {
			get { 
				if (States == null)
                {
					return 0;
                }
				else
                {
					return States.Length;
				}				
			}
        }

		void Start()
        {
			SystemEventController.Instance.Event += OnSystemEvent;
			_currentState = 0;
			EnableState(_currentState);
		}

		void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		public void DispatchCollisionEnterEvent(GameObject collider, GameObject other)
		{
			if (CollisionEnterEvent != null)
			{
				CollisionEnterEvent(collider, other);
			}
		}
		public void DispatchCollisionExitEvent(GameObject collider, GameObject other)
		{
			if (CollisionExitEvent != null)
			{
				CollisionExitEvent(collider, other);
			}
		}

		void OnTriggerEnter(Collider collision)
		{
			DispatchCollisionEnterEvent(this.gameObject, collision.gameObject);
		}

		void OnTriggerExit(Collider collision)
		{
			DispatchCollisionExitEvent(this.gameObject, collision.gameObject);
		}

		void OnCollisionEnter(Collision collision)
		{
			DispatchCollisionEnterEvent(this.gameObject, collision.gameObject);
		}

		void OnCollisionExit(Collision collision)
		{
			DispatchCollisionExitEvent(this.gameObject, collision.gameObject);
		}

		public void EnableState(int state)
        {
			_currentState = state;
			if (TotalStates > 0)
			{
				if (_currentState < 0)
				{
					for (int i = 0; i < TotalStates; i++)
					{
						States[i].SetActive(false);
					}
				}
				else
				{
					if (_currentState >= TotalStates)
					{
						_currentState = 0;
					}
					for (int i = 0; i < TotalStates; i++)
					{
						States[i].SetActive(false);
					}
					States[_currentState].SetActive(true);
				}
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(EventObjectStatesChangeState))
			{
				string nameTarget = (string)parameters[0];
				if (nameTarget.Equals(_name))
                {
					int stateTarget = (int)parameters[1];
					EnableState(stateTarget);
				}
			}
		}
	}
}
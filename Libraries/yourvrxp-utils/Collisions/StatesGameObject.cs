using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace yourvrexperience.Utils
{
	public class StatesGameObject : MonoBehaviour
	{
		public const string EventStatesGameObjectStarted = "EventStatesGameObjectStarted";

		public delegate void StateChangedEvent(StatesGameObject statesGameObject, int state);
		public delegate void CollisionChildEvent(StatesGameObject statesGameObject, bool enterCollision, GameObject parent, GameObject collider, GameObject other);

		public event StateChangedEvent StateEvent;
		public event CollisionChildEvent CollisionEvent;

		public void DispatchStateChangedEvent()
		{
			if (StateEvent != null)
				StateEvent(this, _state);
		}

		public void DispatchCollisionChildEvent(bool enterCollision, GameObject collider, GameObject other)
		{
			if (CollisionEvent != null)
			{
				GameObject stateParent = CheckInObject(collider);
				CollisionEvent(this, enterCollision, stateParent, collider, other);
			}
		}

		[SerializeField] private GameObject[] statesGO;
		[SerializeField] private int DefaultState = 0;
		
		private int _state = -1;

		public int State 
		{
			get { return _state; }
			set { 
				if ((value >= 0) && (_state < statesGO.Length) && (_state != value))
				{
					bool isDefaultSetUp = (_state == -1);
					_state = value; 
					for (int i = 0; i < statesGO.Length; i++)
					{
						statesGO[i].SetActive(_state == i);
					}
					if (!isDefaultSetUp)
					{
						DispatchStateChangedEvent();
					}					
				}				
			}
		}
		public string NameEvent
		{
			get { return this.gameObject.name; }
		}

		public int StatesLength()
		{
			return statesGO.Length;
		}

		void Start()
		{
			State = DefaultState;
			RegisterColliders(this.gameObject);
			SystemEventController.Instance.DispatchSystemEvent(EventStatesGameObjectStarted, this);
		}

		private void RegisterColliders(GameObject target)
		{
			if (target.GetComponent<Collider>() != null)
			{
				ColliderDetector collider = target.AddComponent<ColliderDetector>();
				collider.CollisionEnterEvent += OnCollisionEnterEvent;
				collider.CollisionExitEvent += OnCollisionExitEvent;
			}
			else
			{
				foreach (Transform item in target.transform)
				{
					RegisterColliders(item.gameObject);
				}
			}
		}

		private void OnCollisionEnterEvent(GameObject collider, GameObject other)
		{
			DispatchCollisionChildEvent(true, collider, other);
		}

		private void OnCollisionExitEvent(GameObject collider, GameObject other)
		{
			DispatchCollisionChildEvent(false, collider, other);
		}

		public GameObject GetStateGameObject()
		{
			if (_state <= statesGO.Length)
			{
				return statesGO[_state];
			}
			else
			{
				return null;
			}			
		}

		public GameObject CheckInObject(GameObject target)
		{
			GameObject output = null;
			foreach(GameObject stateGO in statesGO)
			{
				output = CheckInObjectWithParent(stateGO, this.gameObject, target);
				if (output != null)
				{
					break;
				}
			}
			return output;
		}

		private GameObject CheckInObjectWithParent(GameObject parent, GameObject current, GameObject target)
		{
			GameObject output = null;
			if (current == target)
			{
				output = parent;
			}
			else
			{
				foreach (Transform item in current.transform)
				{
					output = CheckInObjectWithParent(parent, item.gameObject, target);
					if (output != null)
					{
						break;
					}
				}
			}
			return output;
		}
	}
}
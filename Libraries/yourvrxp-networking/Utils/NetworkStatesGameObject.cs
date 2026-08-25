using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.Networking
{
	[RequireComponent(typeof(StatesGameObject))]
	public class NetworkStatesGameObject : MonoBehaviour
	{
		public const string EventNetworkStatesGameObjectRequestStateChange = "EventNetworkStatesGameObjectRequestStateChange";
		public const string EventNetworkStatesGameObjectConfirmStateChanged = "EventNetworkStatesGameObjectConfirmStateChanged";

		public static void InitializeStatesGameObject(GameObject target, string uniqueIdentifier)
		{
			if ((target != null) && (target.GetComponent<StatesGameObject>() != null))
			{
				NetworkStatesGameObject netStateGO = target.GetComponent<NetworkStatesGameObject>();
				if (netStateGO == null)
				{
					netStateGO = target.AddComponent<NetworkStatesGameObject>();
				}
				netStateGO.Initialize(uniqueIdentifier);
			}
			else
			{
				foreach (Transform item in target.transform)
				{
					InitializeStatesGameObject(item.gameObject, uniqueIdentifier);
				}
			}
		}

		[SerializeField] private string UniqueIdentificator = "";

		private StatesGameObject _statesGameObject;

		public void Initialize(string uniqueIdentifier)
		{
			UniqueIdentificator = uniqueIdentifier;
		}

		void Start()
		{
			_statesGameObject = this.GetComponent<StatesGameObject>();
			_statesGameObject.StateEvent += OnStateChangedEvent;
			_statesGameObject.CollisionEvent += OnCollisionDetectionEvent;

			NetworkController.Instance.NetworkEvent += OnNetworkEvent;
		}

		void OnDestroy()
		{
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;	
			if (_statesGameObject != null)
			{
				_statesGameObject.StateEvent -= OnStateChangedEvent;
				_statesGameObject.CollisionEvent -= OnCollisionDetectionEvent;
			}
		}

		private void OnCollisionDetectionEvent(StatesGameObject statesGameObject, bool enterCollision, GameObject parent, GameObject collider, GameObject other)
		{
			
		}

		private void OnStateChangedEvent(StatesGameObject statesGameObject, int state)
		{
			if (!NetworkController.Instance.IsServer)
			{
				NetworkController.Instance.DispatchNetworkEvent(EventNetworkStatesGameObjectRequestStateChange, -1, -1, UniqueIdentificator, this.gameObject.name, state);
			}
			else
			{
				NetworkController.Instance.DispatchNetworkEvent(EventNetworkStatesGameObjectConfirmStateChanged, -1, -1, UniqueIdentificator, this.gameObject.name, state);
			}
		}

		private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			if (nameEvent.Equals(EventNetworkStatesGameObjectRequestStateChange))
			{
				if (NetworkController.Instance.IsServer)
				{
					string uid = (string)parameters[0];
					string nameStateObject = (string)parameters[1];
					int newState = (int)parameters[2];
					if ((UniqueIdentificator == uid) && (nameStateObject == this.gameObject.name))
					{
						_statesGameObject.State = newState;
					}
				}
			}
			if (nameEvent.Equals(EventNetworkStatesGameObjectConfirmStateChanged))
			{
				if (!NetworkController.Instance.IsServer)
				{
					string uid = (string)parameters[0];
					string nameStateObject = (string)parameters[1];
					int newState = (int)parameters[2];
					if ((UniqueIdentificator == uid) && (nameStateObject == this.gameObject.name))
					{
						_statesGameObject.State = newState;
					}
				}
			}
			if (nameEvent.Equals(NetworkController.EventNetworkControllerClientLevelReady))
			{
				bool isRequestFromClient = (bool)parameters[0];
				if (isRequestFromClient)
				{
					if (NetworkController.Instance.IsServer)
					{
						NetworkController.Instance.DelayNetworkEvent(EventNetworkStatesGameObjectConfirmStateChanged, 0.5f, -1, -1, UniqueIdentificator, this.gameObject.name, _statesGameObject.State);
					}
				}
			}
		}
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.Networking
{
	[RequireComponent(typeof(PatrolWaypoints))]
	public class NetworkPatrolWaypoints : MonoBehaviour, INetworkInitialData
	{		
		public const string EventNetworkPatrolWaypointsData = "EventNetworkPatrolWaypointsData";

		public const string Separator = "<nw>";

		private NetworkPrefab _networkPrefab;
		private PatrolWaypoints _patrolWaypoints;

		public string ProviderName
		{
			get { return "NetworkPatrolWaypoints"; }
		}		
		public PatrolWaypoints PatrolWaypointsComponent
		{
			get {
				if (_patrolWaypoints == null)
				{
					_patrolWaypoints = this.GetComponent<PatrolWaypoints>();
				}
				return _patrolWaypoints;
			}
		}
		public NetworkPrefab NetworkPrefabComponent
		{
			get {
				if (_networkPrefab == null)
				{
					_networkPrefab = this.GetComponent<NetworkPrefab>();
				}
				return _networkPrefab;
			}
		}


		public string GetInitialData()
		{
			return PatrolWaypointsComponent.Pack();
		}

		public void ApplyInitialData(string data, bool linkedToLevel)
		{
			PatrolWaypointsComponent.UnPack(data);
		}

		void Start()
		{
			NetworkController.Instance.NetworkEvent += OnNetworkEvent;
		}

		void OnDestroy()
		{
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
		}

		protected void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			if (nameEvent.Equals(NetworkController.EventNetworkControllerClientLevelReady))
			{
				if (NetworkController.Instance.IsServer)
				{
					NetworkController.Instance.DispatchNetworkEvent(EventNetworkPatrolWaypointsData, -1, -1, NetworkPrefabComponent.NetworkGameIDView.GetViewID(), PatrolWaypointsComponent.Pack());
				}
			}
			if (nameEvent.Equals(EventNetworkPatrolWaypointsData))
			{
				int viewID = (int)parameters[0];
				if (!NetworkController.Instance.IsServer)
				{
					if (NetworkPrefabComponent.NetworkGameIDView.GetViewID() == viewID)
					{
						PatrolWaypointsComponent.UnPack((string)parameters[1]);
					}
				}
			}
		}
	}
}
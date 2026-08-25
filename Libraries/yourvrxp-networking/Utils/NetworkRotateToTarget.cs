using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.Networking
{
	[RequireComponent(typeof(RotateToTarget))]
	public class NetworkRotateToTarget : MonoBehaviour, INetworkInitialData
	{		
		public const string EventNetworkRotateToTargetData = "EventNetworkRotateToTargetData";

		public const string Separator = "<nw>";

		private NetworkPrefab _networkPrefab;
		private RotateToTarget _rotateToTarget;

		public string ProviderName
		{
			get { return "NetworkRotateToTarget"; }
		}		
		public RotateToTarget RotateToTargetComponent
		{
			get {
				if (_rotateToTarget == null)
				{
					_rotateToTarget = this.GetComponent<RotateToTarget>();
				}
				return _rotateToTarget;
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
			return RotateToTargetComponent.Pack();
		}

		public void ApplyInitialData(string data, bool linkedToLevel)
		{
			RotateToTargetComponent.UnPack(data);
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
					NetworkController.Instance.DispatchNetworkEvent(EventNetworkRotateToTargetData, -1, -1, NetworkPrefabComponent.NetworkGameIDView.GetViewID(), RotateToTargetComponent.Pack());
				}
			}
			if (nameEvent.Equals(EventNetworkRotateToTargetData))
			{
				int viewID = (int)parameters[0];
				if (!NetworkController.Instance.IsServer)
				{
					if (NetworkPrefabComponent.NetworkGameIDView.GetViewID() == viewID)
					{
						RotateToTargetComponent.UnPack((string)parameters[1]);
					}
				}
			}
		}
	}
}
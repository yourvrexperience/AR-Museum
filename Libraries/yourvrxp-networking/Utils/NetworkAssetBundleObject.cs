using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.Networking
{
	public class NetworkAssetBundleObject : MonoBehaviour
	{
		public const string EventNetworkAssetBundleObjectCreated = "EventNetworkAssetBundleObjectCreated";

		private string _uidNetwork;
		private string _nameAsset;
		
		public string UIDNetwork
		{
			get { return _uidNetwork; }
		}

		public void Initialize(string uidNetwork, string nameAsset)
		{
			_uidNetwork = uidNetwork;
			_nameAsset = nameAsset;
		}

		void Start()
		{
			NetworkController.Instance.NetworkEvent += OnNetworkEvent;	
			if (NetworkController.Instance.IsServer)
			{
				NetworkController.Instance.DispatchNetworkEvent(EventNetworkAssetBundleObjectCreated, -1, -1, _uidNetwork, _nameAsset, this.transform.position);
			}			
		}

		void OnDestroy()
		{
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;	
		}

		private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			if (nameEvent.Equals(NetworkController.EventNetworkControllerClientLevelReady))
			{
				bool isRequestFromClient = (bool)parameters[0];
				if (isRequestFromClient)
				{
					if (NetworkController.Instance.IsServer)
					{
						NetworkController.Instance.DelayNetworkEvent(EventNetworkAssetBundleObjectCreated, 0.5f, -1, -1, _uidNetwork, _nameAsset, this.transform.position);
					}
				}
			}
		}
	}
}
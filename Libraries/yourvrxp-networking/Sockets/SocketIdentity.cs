using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace yourvrexperience.Networking
{
	public class SocketIdentity : MonoBehaviour
	{
		public int Owner;
		public int NetID;
		public int IndexPrefab;

#if ENABLE_SOCKETS
		void Awake()
		{
			Owner = -1;
			NetID = -1;
			IndexPrefab = -1;
		}

		void Start()
		{
			NetworkController.Instance.NetworkEvent += OnNetworkEvent;
		}

		void OnDestroy()
		{
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
		}

		private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			if (nameEvent.Equals(NetworkController.EventNetworkControllerPlayerDisconnected))
			{
				int uidDisconnected = (int)parameters[0];
				if ((Owner == uidDisconnected) || (NetID == uidDisconnected))
				{
					GameObject.Destroy(this.gameObject);
				}
			}
		}

		public void Set(int owner, int netID, int indexPrefab)
		{
			Owner = owner;
			NetID = netID;
			IndexPrefab = indexPrefab;
		}
#endif		
	}
}

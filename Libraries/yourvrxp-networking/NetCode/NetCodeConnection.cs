using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;
using System;
#if ENABLE_NETCODE
using Unity.Netcode;
#endif

namespace yourvrexperience.Networking
{
	public class NetCodeConnection : 
#if ENABLE_NETCODE	
	NetworkBehaviour
#else
	MonoBehaviour
#endif	
	{
#if ENABLE_NETCODE			
		private void Start()
		{
			if (IsLocalPlayer)
			{
				SystemEventController.Instance.DispatchSystemEvent(NetCodeController.EventNetcodeControllerLocalConnection, this, (int)NetworkObjectId);
			}
			else
			{
				SystemEventController.Instance.DelaySystemEvent(NetCodeController.EventNetcodeControllerNewClientConnection,  0.2f, (int)NetworkObjectId);
			}
			SystemEventController.Instance.Event += OnSystemEvent;			
		}

		private NetworkObject GetNetworkObject(int networkID)
		{
			NetworkObject[] networkObjects = GameObject.FindObjectsOfType<NetworkObject>();
			foreach (NetworkObject netObject in networkObjects)
			{
				if ((int)netObject.NetworkObjectId == networkID)
				{
					return netObject;
				}
			}
			return null;
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (NetworkController.Instance != null)	NetworkController.Instance.DispatchEvent(NetworkController.EventNetworkControllerPlayerDisconnected, (int)NetworkObjectId);
		}

		[ServerRpc]
		public void NetworkObjectServerRpc(string uniqueNetworkName, string prefab, Vector3 position, int owner, string data, string types)
		{
			GameObject networkGO = Instantiate(Resources.Load(prefab) as GameObject);
			networkGO.transform.position = position;
			networkGO.name = uniqueNetworkName;
			networkGO.GetComponent<NetworkObjectID>().InstanceID = new NetworkVariable<int>(NetCodeController.Instance.InstanceCounter);
			networkGO.GetComponent<NetworkObjectID>().OwnerID = new NetworkVariable<int>(owner);
			networkGO.GetComponent<NetworkObjectID>().NameObject = uniqueNetworkName;
			networkGO.GetComponent<NetworkObject>().SpawnWithOwnership((ulong)owner);
			yourvrexperience.Utils.Utilities.FixObject(networkGO);
			if (NetworkController.Instance.IsMultipleScene) DontDestroyOnLoad(networkGO);
			NetCodeController.Instance.InstanceCounter++;
		}

		[ServerRpc]
		public void MessageFromClientsToServerServerRpc(string nameEvent, int origin, int target, string data, string types)
		{
			MessageFromServerToClientsClientRpc(nameEvent, origin, target, data, types);
		}

		[ClientRpc]
		private void MessageFromServerToClientsClientRpc(string nameEvent, int origin, int target, string data, string types)
		{
			List<object> parameters = new List<object>();
			NetworkUtils.Deserialize(parameters, data, types);
			NetworkController.Instance.DispatchEvent(nameEvent, origin, target, parameters.ToArray());
		}

		[ServerRpc]
		public void AssignNetworkAuthorityServerRpc(int networkID, int newOwnerID)
		{
			NetworkObject netObject = GetNetworkObject(networkID);
			if (netObject != null)
			{
				if ((int)netObject.OwnerClientId != newOwnerID)
				{
					netObject.ChangeOwnership((ulong)newOwnerID);
					NetworkController.Instance.DispatchNetworkEvent(NetworkObjectID.EventNetworkObjectIDTransferOwnership, -1, -1, (int)networkID, (int)newOwnerID);
				}
			}
		}

		[ServerRpc]
		public void DestroyServerRpc(int networkID)
		{
			NetworkObject netObject = GetNetworkObject(networkID);
			if (netObject != null)
			{
				netObject.Despawn();
			}			
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))
			{
				DontDestroyOnLoad(this.gameObject);
			}
		}
#endif		
	}
}

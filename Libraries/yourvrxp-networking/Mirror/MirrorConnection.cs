#if ENABLE_MIRROR
using Mirror;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.Networking
{
	public class MirrorConnection : 
#if ENABLE_MIRROR	
	NetworkBehaviour
#else
	MonoBehaviour
#endif
	{
#if ENABLE_MIRROR			
		private void Start()
		{
			int netID = (int)this.netId;
			syncInterval = 0.033f;

			if (isLocalPlayer)
			{
				SystemEventController.Instance.DispatchSystemEvent(MirrorController.EventMirrorNetworkLocalConnection, this, netID);
			}
			else
			{
				SystemEventController.Instance.DispatchSystemEvent(MirrorController.EventMirrorNetworkNewClientConnection, netID);
			}
			if (MirrorController.DebugMessages) Debug.LogError("MirrorConnection::CONNECTION PREFAB CREATED");
			SystemEventController.Instance.Event += OnSystemEvent;
			if (NetworkController.Instance.IsMultipleScene)
			{
				DontDestroyOnLoad(this.gameObject);
			}
		}

		void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (NetworkController.Instance != null) NetworkController.Instance.DispatchEvent(NetworkController.EventNetworkControllerPlayerDisconnected, (int)this.netId);
		}

		[Command]
		public void CmdMessageFromClientsToServer(string nameEvent, int origin, int target, string data, string types)
		{
			RpcMessageFromServerToClients(nameEvent, origin, target, data, types);
		}

		[ClientRpc]
		private void RpcMessageFromServerToClients(string nameEvent, int origin, int target, string data, string types)
		{
			List<object> parameters = new List<object>();
			NetworkUtils.Deserialize(parameters, data, types);
			NetworkController.Instance.DispatchEvent(nameEvent, origin, target, parameters.ToArray());
		}

		[Command]
		public void CmdNetworkObject(string uniqueNetworkName, string prefab, Vector3 position, int owner, string data, string types)
		{
			GameObject networkGO = Instantiate(Resources.Load(prefab) as GameObject);
			networkGO.transform.position = position;
			networkGO.name = uniqueNetworkName;
			networkGO.GetComponent<NetworkObjectID>().Owner = owner;
			networkGO.GetComponent<NetworkObjectID>().InstanceID = MirrorController.Instance.InstanceCounter;
			networkGO.GetComponent<NetworkObjectID>().NameObject = uniqueNetworkName;
			yourvrexperience.Utils.Utilities.FixObject(networkGO);
			if (NetworkController.Instance.IsMultipleScene) DontDestroyOnLoad(networkGO);
			MirrorController.Instance.InstanceCounter++;
			NetworkServer.Spawn(networkGO);
		}

		[Command]
		public void CmdAssignNetworkAuthority(NetworkIdentity target, NetworkIdentity clientId)
		{
			if (target.isOwned && target.connectionToClient != clientId.connectionToClient)
			{
				target.RemoveClientAuthority();
				target.AssignClientAuthority(clientId.connectionToClient);
				NetworkController.Instance.DispatchNetworkEvent(NetworkObjectID.EventNetworkObjectIDTransferOwnership, -1, -1, (int)target.netId, (int)clientId.netId);
			}
			else
			{
				if (!target.isOwned)
				{
					target.RemoveClientAuthority();
					target.AssignClientAuthority(clientId.connectionToClient);
					NetworkController.Instance.DispatchNetworkEvent(NetworkObjectID.EventNetworkObjectIDTransferOwnership, -1, -1, (int)target.netId, (int)clientId.netId);
				}
			}
		}

		[Command]
		public void CmdDestroy(NetworkIdentity target)
		{
			if (target != null)
			{
				if (target.gameObject != null)
				{
					GameObject.Destroy(target.gameObject);
				}				
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

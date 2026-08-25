using System;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.Networking
{

	[RequireComponent(typeof(SocketIdentity))]
    public class SocketTransform : MonoBehaviour
    {
		public const string EventSocketTransformNew = "EventSocketTransformNew";

		[SerializeField] private float timeToUpdate = 0.2f;

		private SocketIdentity  _socketIdentity;

		private float _timeToUpdate = 0;
		
#if ENABLE_SOCKETS
		private bool _enabled = false;

        void Start()
        {
			 _socketIdentity = GetComponent<SocketIdentity>();
            
			SocketsController.Instance.SocketEvent += OnSocketsTransform;
			NetworkController.Instance.NetworkEvent += OnNetworkEvent;

			SystemEventController.Instance.DispatchSystemEvent(EventSocketTransformNew, _socketIdentity);

			_enabled = true;
			SocketsController.Instance.SendTransform( _socketIdentity.Owner,  _socketIdentity.NetID,  _socketIdentity.IndexPrefab, this.transform.position, this.transform.rotation, this.transform.localScale);
        }

		void OnDestroy()
		{
			if (SocketsController.Instance != null) SocketsController.Instance.SocketEvent -= OnSocketsTransform;
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
		}

		private void OnSocketsTransform(int owner, int netID, int indexPrefab, Vector3 position, Quaternion rotation, Vector3 scale)
		{
			if (NetworkController.Instance.UniqueNetworkID !=  _socketIdentity.Owner)
			{
				if (netID ==  _socketIdentity.NetID)
				{
					this.transform.localScale = scale;

					InterpolatorController.Instance.Stop(this.gameObject);
					InterpolatorController.Instance.InterpolatePosition(this.gameObject, position, timeToUpdate);
					InterpolatorController.Instance.InterpolateRotation(this.gameObject, rotation, timeToUpdate);
				}
			}
		}

		private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			if (nameEvent.Equals(NetworkController.EventNetworkControllerDisconnected))
			{
				_enabled = false;
			}
		}

		void Update()
		{
			if (_enabled)
			{
				if (NetworkController.Instance != null)
				{
					if (NetworkController.Instance.IsConnected)
					{
						if (NetworkController.Instance.UniqueNetworkID != -1)
						{
							if (NetworkController.Instance.UniqueNetworkID == _socketIdentity.Owner)
							{
								_timeToUpdate += Time.deltaTime;
								if (_timeToUpdate >= timeToUpdate)
								{
									_timeToUpdate = 0;
									if (_socketIdentity.NetID != -1)
									{
										SocketsController.Instance.SendTransform( _socketIdentity.Owner,  _socketIdentity.NetID,  _socketIdentity.IndexPrefab, this.transform.position, this.transform.rotation, this.transform.localScale);
									}							
								}
							}
						}
					}
				}
			}
		}
#endif		
    }
}
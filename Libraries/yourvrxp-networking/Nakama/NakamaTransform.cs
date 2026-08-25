using System;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.Networking
{

	[RequireComponent(typeof(NakamaIdentity))]
    public class NakamaTransform : MonoBehaviour
    {
		public const string EventNakamaTransformNew = "EventNakamaTransformNew";

		[SerializeField] private float timeToUpdate = 0.2f;

		private NakamaIdentity  _nakamaIdentity;

		private float _timeToUpdate = 0;
		
#if ENABLE_NAKAMA
		private bool _enabled = false;

        void Start()
        {
			 _nakamaIdentity = GetComponent<NakamaIdentity>();
            
			NakamaController.Instance.NakamaEvent += OnNakamaTransform;
			NetworkController.Instance.NetworkEvent += OnNetworkEvent;

			SystemEventController.Instance.DispatchSystemEvent(EventNakamaTransformNew,  _nakamaIdentity);

			_enabled = true;
			NakamaController.Instance.SendTransform( _nakamaIdentity.Owner,  _nakamaIdentity.NetID,  _nakamaIdentity.IndexPrefab, this.transform.position, this.transform.rotation, this.transform.localScale);
        }

		void OnDestroy()
		{
			if (NakamaController.Instance != null) NakamaController.Instance.NakamaEvent -= OnNakamaTransform;
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
		}

		private void OnNakamaTransform(int owner, int netID, int indexPrefab, Vector3 position, Quaternion rotation, Vector3 scale)
		{
			if (NetworkController.Instance.UniqueNetworkID !=  _nakamaIdentity.Owner)
			{
				if (netID ==  _nakamaIdentity.NetID)
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
							if (NetworkController.Instance.UniqueNetworkID ==  _nakamaIdentity.Owner)
							{
								_timeToUpdate += Time.deltaTime;
								if (_timeToUpdate >= timeToUpdate)
								{
									_timeToUpdate = 0;
									if (_nakamaIdentity.NetID != -1)
									{
										NakamaController.Instance.SendTransform( _nakamaIdentity.Owner,  _nakamaIdentity.NetID,  _nakamaIdentity.IndexPrefab, this.transform.position, this.transform.rotation, this.transform.localScale);	
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
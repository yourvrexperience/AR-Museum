#if ENABLE_AVATAR_OCULUS
using Oculus.Avatar2;
using yourvrexperience.Networking;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using yourvrexperience.Utils;

namespace yourvrexperience.VR
{
    public class OculusMetaAvatarEntity : 
#if ENABLE_AVATAR_OCULUS	
	OvrAvatarEntity
#else	
	MonoBehaviour
#endif	
    {
		public const string EventOculusMetaAvatarEntitySendData = "EventOculusMetaAvatarEntitySendData";

		public const float TimeToUpdateAvatar = 0.2f;
		
#if ENABLE_AVATAR_OCULUS

		private bool _imOwner = false;
		private float _timer = 0;

		private NetworkObjectID _networkObjectID;

		void Awake()
		{
			NetworkController.Instance.NetworkEvent += OnNetworkEvent;
			base.Awake();			
		}

		void OnDestroy()
		{
			base.OnDestroy();
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
		}

		public void InitFirstPersonLocalAvatar(NetworkObjectID networkObjectID)
		{
			_networkObjectID = networkObjectID;
			OvrAvatarInputManager ovrAvatarInputManager = GameObject.FindObjectOfType<OvrAvatarInputManager>();
			if (ovrAvatarInputManager)
			{
				 _creationInfo.features = Oculus.Avatar2.CAPI.ovrAvatar2EntityFeatures.Preset_Default;				 
				SetBodyTracking(ovrAvatarInputManager);
				ForceStreamLod(StreamLOD.High);
				SetActiveView(CAPI.ovrAvatar2EntityViewFlags.FirstPerson);
				SetIsLocal(true);
				_imOwner = true;
			}
		}

		public void InitThirdPersonRemoteAvatar(NetworkObjectID networkObjectID)
		{
			_networkObjectID = networkObjectID;
			_creationInfo.features = Oculus.Avatar2.CAPI.ovrAvatar2EntityFeatures.Preset_Remote;
			ForceStreamLod(StreamLOD.High);
			SetIsLocal(false);
			SetActiveView(CAPI.ovrAvatar2EntityViewFlags.ThirdPerson);
			_imOwner = false; 
		}

		private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			if (nameEvent.Equals(EventOculusMetaAvatarEntitySendData))
			{
				if (!_imOwner)				
				{
					if (_networkObjectID != null)
					{
						if (_networkObjectID.GetViewID() == (int)parameters[0])
						{
							string packetAvatar = (string)parameters[1];
							byte[] bytesAvatar = Convert.FromBase64String(packetAvatar);
							ApplyStreamData(bytesAvatar);
						}
					}
				}
			}
		}

		void Update()
		{
			if (_imOwner)
			{
				_timer += Time.deltaTime;
				if (_timer > TimeToUpdateAvatar)
				{
					_timer = 0;
					byte[] bytesAvatar = RecordStreamData(activeStreamLod);
					string packetAvatar = Convert.ToBase64String(bytesAvatar);
					NetworkController.Instance.DispatchNetworkEvent(EventOculusMetaAvatarEntitySendData, -1, -1, _networkObjectID.GetViewID(), packetAvatar);
				}
			}
		}

#endif
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Networking;
using yourvrexperience.Utils;
using UnityEngine;
#if ENABLE_VIVOX
using VivoxUnity;
#endif

namespace yourvrexperience.Voice
{
	public class VoiceController : MonoBehaviour
	{
		private static VoiceController _instance;
        public static VoiceController Instance
        {
            get
            {
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(VoiceController)) as VoiceController;
				}
                return _instance;
            }
        }

		[SerializeField] private GameObject DissonanceVoice;
		[SerializeField] private GameObject DissonanceWorld;
		[SerializeField] private GameObject VivoxVoice;

#if ENABLE_DISSONANCE_MIRROR				
		private GameObject _dissonanceVoice;
		private GameObject _dissonanceWorld;
#elif ENABLE_VIVOX
		private GameObject _vivoxVoice;
#endif				

		void Start()
		{
			NetworkController.Instance.NetworkEvent += OnNetworkEvent;
			SystemEventController.Instance.Event += OnSystemEvent;
		}

		void OnDestroy()
		{
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
#if ENABLE_VIVOX
			if (nameEvent.Equals(VivoxVoiceController.EventVivoxVoiceControllerInitialized))
            {
                string nameToLogin = yourvrexperience.Utils.Utilities.RandomCodeGeneration(NetworkController.Instance.NameRoom);
                VivoxVoiceController.Instance.Login(nameToLogin);
            }
            if (nameEvent.Equals(VivoxVoiceController.EventVivoxVoiceControllerLoggedIn))
            {
                VivoxVoiceController.Instance.JoinChannel(NetworkController.Instance.NameRoom, ChannelType.NonPositional, VivoxVoiceController.ChatCapability.AudioOnly);
            }
            if (nameEvent.Equals(VivoxVoiceController.EventVivoxVoiceControllerNewParticipant))
            {
                string username = (string)parameters[0];
                string channel = (string)parameters[1];
                bool participantCanTransmit = (bool)parameters[2];
            }
#endif		
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))
			{
				if (Instance)
				{
					DontDestroyOnLoad(Instance.gameObject);
#if ENABLE_DISSONANCE_MIRROR				
					if (_dissonanceVoice != null) DontDestroyOnLoad(_dissonanceVoice);
					if (_dissonanceWorld != null) DontDestroyOnLoad(_dissonanceWorld);
#elif ENABLE_VIVOX
					if (_vivoxVoice != null) DontDestroyOnLoad(_vivoxVoice);
#endif				
				}
			}
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerReleaseAllResources))
			{
				if (Instance)
				{
					_instance = null;
					GameObject.Destroy(this.gameObject);					
#if ENABLE_DISSONANCE_MIRROR				
					if (_dissonanceVoice != null) GameObject.Destroy(_dissonanceVoice);
					if (_dissonanceWorld != null) GameObject.Destroy(_dissonanceWorld);
#elif ENABLE_VIVOX
					if (_vivoxVoice != null) GameObject.Destroy(_vivoxVoice);
#endif				
				}
			}
		}

		protected void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			if (nameEvent.Equals(NetworkController.EventNetworkControllerConnectionWithRoom))
			{
#if !UNITY_WEBGL || UNITY_EDITOR
				Debug.Log(Microphone.devices.Length);
				Debug.Log(Application.internetReachability.ToString());
#endif

#if ENABLE_DISSONANCE_MIRROR
				_dissonanceVoice = Instantiate(DissonanceVoice) as GameObject;
				_dissonanceWorld = Instantiate(DissonanceWorld) as GameObject;
				if (NetworkController.Instance.IsMultipleScene)
				{
					if (_dissonanceVoice != null) DontDestroyOnLoad(_dissonanceVoice);
					if (_dissonanceWorld != null) DontDestroyOnLoad(_dissonanceWorld);
				}
#elif ENABLE_VIVOX
				_vivoxVoice = Instantiate(VivoxVoice) as GameObject;
				_vivoxVoice.GetComponent<VivoxVoiceController>().Initialize();
				if (NetworkController.Instance.IsMultipleScene)
				{
					if (_vivoxVoice != null) DontDestroyOnLoad(_vivoxVoice);
				}
#endif
			}
		}
	}
}
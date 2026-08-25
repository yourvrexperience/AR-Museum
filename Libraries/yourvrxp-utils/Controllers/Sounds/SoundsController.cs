using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace yourvrexperience.Utils
{
	public class SoundsController : MonoBehaviour
	{
		public const string EventSoundsControllerFadeCompleted = "EventSoundsControllerFadeCompleted";
		public const string EventSoundsControllerErrorRemoteFormat = "EventSoundsControllerErrorRemoteFormat";

		public const bool FORCE_VOLUME_ZERO = false;

		public enum ChannelsAudio { Background = 0, FX1, FX2, FX3 }

		private static SoundsController _instance;
		public static SoundsController Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType<SoundsController>();
				}
				return _instance;
			}
		}

		public AudioClip[] Sounds;
		public bool EnableSound = false;		

		private AudioSource[] _audioSources;
		private AudioSource _audioBackground;

		private string _currentAudioMelodyPlaying = "";

		public string CurrentAudioMelodyPlaying
		{
			get { return _currentAudioMelodyPlaying; }
		}

        public object WaveFileWriter { get; private set; }

        void Awake()
		{
			_audioSources = GetComponents<AudioSource>();
			_audioBackground = _audioSources[(int)ChannelsAudio.Background];
		}

		void Start()
		{
		}

		void OnDestroy()
		{
			_instance = null;
		}

        public void StopAllSounds()
		{
			StopSoundBackground();
			StopSoundsFx();
		}

		public void SwitchMuteAll(bool shouldMute)
		{ 
			foreach (AudioSource audioSource in _audioSources)
            {
				if (audioSource != null)
                {
					audioSource.volume = (shouldMute ? 0 : 1);
				}
            }
		}

		public AudioSource GetChannelAudioSource(ChannelsAudio channelAudio)
		{
			return _audioSources[(int)channelAudio];
		}

		public void PlaySoundClipBackground(AudioClip audio, bool loop, float volume, bool is3D = false)
		{
			if (!EnableSound) return;

			_currentAudioMelodyPlaying = audio.name;
			_audioBackground.clip = audio;
			_audioBackground.loop = loop;
			_audioBackground.volume = volume;
			_audioBackground.Play();
			_audioBackground.spatialBlend = (is3D?1:0);

			if (FORCE_VOLUME_ZERO) _audioBackground.volume = 0;
		}
		public void SetVolume(ChannelsAudio channel, float volume)
		{
			_audioSources[(int)channel].volume = volume;
			if (FORCE_VOLUME_ZERO) _audioSources[(int)channel].volume = 0;
		}
		public void PauseSoundBackground()
		{
			_audioBackground.Pause();
		}
		public void PauseSoundFX(ChannelsAudio channel)
		{
			_audioSources[(int)channel].Pause();
		}
		public void ResumeSoundBackground()
		{
			_audioBackground.UnPause();
		}
		public void ResumeSoundFX(ChannelsAudio channel)
		{
			_audioSources[(int)channel].UnPause();
		}
		public void StopSoundBackground()
		{
			_currentAudioMelodyPlaying = "";
			_audioBackground.clip = null;
			_audioBackground.Stop();
		}

		public void SeekPosition(ChannelsAudio channel, AudioClip clip, float progress, float extraShift = 0)
        {
			_audioSources[(int)channel].time = (clip.length * progress) + extraShift;
		}

		public void PlaySoundBackground(string audioName, bool loop, float volume)
		{
			for (int i = 0; i < Sounds.Length; i++)
			{
				if (Sounds[i].name == audioName)
				{
					PlaySoundClipBackground(Sounds[i], loop, volume);
				}
			}
		}

		public void PlaySoundClipFx(ChannelsAudio channel, AudioClip audio, bool loop, float volume, bool is3D = false)
		{
			if (!EnableSound) return;
			if ((int)channel >= _audioSources.Length) return;

			_audioSources[(int)channel].clip = audio;
			_audioSources[(int)channel].loop = loop;
			_audioSources[(int)channel].volume = volume;
			_audioSources[(int)channel].Play();
			_audioSources[(int)channel].spatialBlend = (is3D?1:0);

			if (FORCE_VOLUME_ZERO) _audioSources[(int)channel].volume = 0;
		}

		public void PlayOneShootSoundClipFx(ChannelsAudio channel, AudioClip audio, bool loop, float volume, bool is3D = false)
		{
			if (!EnableSound) return;
			if ((int)channel >= _audioSources.Length) return;

			_audioSources[(int)channel].loop = loop;
			_audioSources[(int)channel].volume = volume;
			_audioSources[(int)channel].spatialBlend = (is3D ? 1 : 0);
			_audioSources[(int)channel].PlayOneShot(audio);

			if (FORCE_VOLUME_ZERO) _audioSources[(int)channel].volume = 0;
		}

		public void StopSoundsFx()
		{
			if ((int)ChannelsAudio.FX1 < _audioSources.Length) 
			{
				_audioSources[(int)ChannelsAudio.FX1].clip = null;
				_audioSources[(int)ChannelsAudio.FX1].Stop();
			}
			if ((int)ChannelsAudio.FX2 < _audioSources.Length) 
			{
				_audioSources[(int)ChannelsAudio.FX2].clip = null;
				_audioSources[(int)ChannelsAudio.FX2].Stop();
			}
			if ((int)ChannelsAudio.FX3 < _audioSources.Length)
			{
				_audioSources[(int)ChannelsAudio.FX3].clip = null;
				_audioSources[(int)ChannelsAudio.FX3].Stop();
			}
		}

		public void StopSoundFx(ChannelsAudio channel)
		{
			_audioSources[(int)channel].clip = null;
			_audioSources[(int)channel].Stop();
		}

		public void PlaySoundFX(string audioName, bool loop, float volume)
		{
			if (volume > 0)
            {
				for (int i = 0; i < Sounds.Length; i++)
				{
					if (Sounds[i] != null)
					{
						if (Sounds[i].name == audioName)
						{
							PlaySoundClipFx(ChannelsAudio.FX1, Sounds[i], loop, volume);
						}
					}
				}
			}
		}

		public void PlaySoundFX(ChannelsAudio channel, string audioName, bool loop, float volume)
		{
			for (int i = 0; i < Sounds.Length; i++)
			{
				if (Sounds[i].name == audioName)
				{
					PlaySoundClipFx(channel, Sounds[i], loop, volume);
				}
			}
		}


        public void PlayRemoteBackground(AudioType audioType, ChannelsAudio channel, string audioURL, bool loop, float volume, bool is3D = false)
        {
            StartCoroutine(LoadAudioFromServer(audioType, audioURL, true, channel, loop, volume, is3D));
        }

        public void PlayRemoteFX(AudioType audioType, ChannelsAudio channel, string audioURL, bool loop, float volume, bool is3D = false)
        {
            StartCoroutine(LoadAudioFromServer(audioType, audioURL, false, channel, loop, volume, is3D));
        }

        private IEnumerator LoadAudioFromServer(AudioType audioType, string url, bool isBackground, ChannelsAudio channel, bool loop, float volume, bool is3D)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    Debug.LogError("Error while receiving audio clip: " + www.error);
                }
                else
                {
					AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
					if (audioClip.length == 0)
                    {
						SystemEventController.Instance.DispatchSystemEvent(EventSoundsControllerErrorRemoteFormat, url);
					}
					else
                    {

						if (isBackground)
						{
							PlaySoundClipBackground(audioClip, loop, volume, is3D);
						}
						else
						{
							PlaySoundClipFx(channel, audioClip, loop, volume, is3D);
						}
					}
				}
            }
        }

        public void Play3DSound(AudioClip audioClip, Vector3 position, float volume, GameObject objectSound = null, bool loop = false)
        {
            if (!EnableSound) return;
            if (audioClip == null) return;

            AudioSource audioSource;
            GameObject soundGameObject = null;
            if (objectSound == null)
            {
                soundGameObject = new GameObject("One Shot Sound");
            }
            else
            {
                soundGameObject = new GameObject("Passenger Object");
                soundGameObject.AddComponent<PassengerObject>();
                soundGameObject.GetComponent<PassengerObject>().MainObject = objectSound;
                soundGameObject.transform.position = objectSound.transform.position;
            }

            soundGameObject.AddComponent<AudioSource>();
            soundGameObject.transform.position = position;
            audioSource = soundGameObject.GetComponent<AudioSource>();

            audioSource.clip = audioClip;
            audioSource.volume = volume;
            audioSource.spatialBlend = 1;
            audioSource.loop = loop;

            audioSource.Play();

            if (objectSound == null)
            {
                GameObject.Destroy(soundGameObject, audioSource.clip.length);
            }                
        }

		public AudioClip CreateFromBytes(string name, byte[] data, int channels = 1, int frequency = 48000)
		{
			float[] samples = yourvrexperience.Utils.Utilities.ConvertByteToFloat(data);
			var clip = AudioClip.Create(name, samples.Length, channels, frequency, false);
			clip.SetData(samples, 0);
			return clip;
		}

		public void DownloadAudioFile(string eventName, int id, string extension, string urlData, bool shouldReport)
		{
			StartCoroutine(LoadMusic(eventName, id, extension, urlData, shouldReport));
		}

		public void LoadSoundDataBytes(byte[] receivedBytes, string eventName, int id, string extension, bool shouldReport)
		{
#if USE_NVORBIS
			StartCoroutine(CreateAudioclipBytes(receivedBytes, eventName, id, extension, shouldReport));
#endif
		}

#if USE_NVORBIS
		public IEnumerator CreateAudioclipBytes(byte[] receivedBytes, string eventName, int id, string extension, bool shouldReport)
        {
			// Decode OGG data using NVorbis
			using (var memStream = new MemoryStream(receivedBytes))
			using (var vorbisReader = new NVorbis.VorbisReader(memStream, true))
			{
				int channels = vorbisReader.Channels;
				int sampleRate = vorbisReader.SampleRate;

				List<float> sampleList = new List<float>(); 
				float[] buffer = new float[1024];

				// Read all samples into the list
				while (true)
				{
					int read = vorbisReader.ReadSamples(buffer, 0, buffer.Length);
					if (read == 0) break;

					sampleList.AddRange(buffer.Take(read));
				}

				// Convert to array
				float[] samples = sampleList.ToArray();
				int totalSamples = samples.Length;

				// Create an AudioClip
				string clipName = "Audio_" + yourvrexperience.Utils.Utilities.RandomCodeGeneration(10);
				AudioClip audioClip = AudioClip.Create(clipName, (int)totalSamples / channels, channels, sampleRate, false);

				// Set the audio data
				if (!audioClip.SetData(samples, 0))
				{
					Debug.LogError("Failed to set audio data on AudioClip.");
					yield break;
				}

				// Use the AudioClip (e.g., assign to an AudioSource)
#if UNITY_EDITOR
				Debug.Log($"AudioClip {clipName} created successfully.");
#endif

				if (audioClip != null)
				{
					if (audioClip.samples == 0)
					{
						SystemEventController.Instance.DispatchSystemEvent(eventName, false, id);
					}
					else
					{
						SystemEventController.Instance.DispatchSystemEvent(eventName, true, id, shouldReport, extension, audioClip, receivedBytes);
					}
				}
				else
				{
					SystemEventController.Instance.DispatchSystemEvent(eventName, false, id);
				}
			}
		}
#endif

		private IEnumerator LoadMusic(string eventName, int id, string extension, string urlAudioPath, bool shouldReport)
		{
			AudioType typeAudio = AudioType.UNKNOWN;
			if ((extension.IndexOf("mp3") != -1) || (extension.IndexOf(".mp3") != -1))
			{
				typeAudio = AudioType.MPEG;
			}
			else if ((extension.IndexOf("wav") != -1) || (extension.IndexOf(".wav") != -1))
			{
				typeAudio = AudioType.WAV;
			}
			else if ((extension.IndexOf("ogg") != -1) || (extension.IndexOf(".ogg") != -1))
			{
				typeAudio = AudioType.OGGVORBIS;
			}
			if (typeAudio == AudioType.UNKNOWN)
			{
				SystemEventController.Instance.DispatchSystemEvent(eventName, false, id);
			}
			else
			{
				using (UnityWebRequest www = UnityWebRequest.Get(urlAudioPath))
				{
					yield return www.SendWebRequest();

					if (www.result != UnityWebRequest.Result.Success)
					{
						SystemEventController.Instance.DispatchSystemEvent(eventName, false, id);
						yield break;
					}

					byte[] receivedBytes = www.downloadHandler.data;

					if ((receivedBytes == null) || (receivedBytes.Length == 0))
					{
						SystemEventController.Instance.DispatchSystemEvent(eventName, false, id);
					}
					else
					{
						try
						{
							LoadSoundDataBytes(receivedBytes, eventName, id, extension, shouldReport);
						}
						catch (Exception err) 
						{ 
							SystemEventController.Instance.DispatchSystemEvent(eventName, false, id);
						}
					}
				}
			}
		}

#if UNITY_WEBGL
		public void PlayJSAudio(string url)
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			PlayJSAudioFromURL(url);
#else
			Debug.Log("Audio playback is only available in WebGL builds.");
#endif
		}

		public void StopJS()
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			StopJSAudio();
#endif
		}

		[DllImport("__Internal")]
		private static extern void PlayJSAudioFromURL(string url);

		[DllImport("__Internal")]
		private static extern void StopJSAudio();
#endif
	}
}
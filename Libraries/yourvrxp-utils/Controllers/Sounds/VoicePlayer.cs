using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.Utils
{
    public class VoicePlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource audioPOI;

        public void SetAudioClip(AudioClip audio)
        {
            audioPOI.clip = audio;
        }

        public void Play()
        {
            audioPOI.Play();
        }

        public void Stop()
        {
            audioPOI.Stop();
        }
    }
}

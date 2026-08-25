using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
#if ENABLE_MICROPHONE_OGG
using Utilities.Audio;
using Utilities.Encoding.OggVorbis;
#endif

namespace yourvrexperience.Utils
{
    public class WebMicrophoneRecorder : MonoBehaviour
    {
        public const string EventWebMicrophoneRecorderCompleted = "EventWebMicrophoneRecorderCompleted";

        private static WebMicrophoneRecorder _instance;

        public static WebMicrophoneRecorder Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(WebMicrophoneRecorder)) as WebMicrophoneRecorder;
                }

                return _instance;
            }
        }

        public void StartRecording()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            startOggVorbisRecording(this.gameObject.name, "OnRecordingReady");
#else
            Debug.LogWarning("Microphone recording only works in WebGL build.");
#endif
        }

        public void StopRecording()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            stopOggVorbisRecording();
#else
            byte[] audioBytes = null;
            SystemEventController.Instance.DelaySystemEvent(EventWebMicrophoneRecorderCompleted, 0.2f, audioBytes);
#endif
        }

        public float[] ConvertFloat32PCM(byte[] byteRaw)
        {
            if (byteRaw.Length % 4 != 0)
            {
                Debug.LogWarning("Byte array length is not a multiple of 4.");
                return null;
            }

            int sampleCount = byteRaw.Length / 4;
            float[] samples = new float[sampleCount];

            Buffer.BlockCopy(byteRaw, 0, samples, 0, byteRaw.Length);
            return samples;
        }

        public static float[] Resample(float[] input, int inputRate, int outputRate)
        {
            int outputLength = Mathf.RoundToInt((float)input.Length * outputRate / inputRate);
            float[] output = new float[outputLength];

            for (int i = 0; i < outputLength; i++)
            {
                float interp = (float)i * inputRate / outputRate;
                int index = Mathf.FloorToInt(interp);
                float frac = interp - index;

                float sampleA = input[Mathf.Clamp(index, 0, input.Length - 1)];
                float sampleB = input[Mathf.Clamp(index + 1, 0, input.Length - 1)];
                output[i] = Mathf.Lerp(sampleA, sampleB, frac);
            }

            return output;
        }

        public void OnRecordingReady(string data)
        {
            string[] fullData = data.Split('|');
            string frequencyRecorded = fullData[0];
            string base64 = fullData[1];
            byte[] byteRaw = Convert.FromBase64String(base64);

            // Read PCM bytes
            var samples = ConvertFloat32PCM(byteRaw);

            // FADE IN SOUND FROM 50% to 100% IN 2 SECONDS
            int sampleRate = 48000;
            float startingVolume = 0.4f;
            float totalRecovereVolume = 1 - startingVolume;
            int fadeLength = Mathf.FloorToInt(sampleRate * 2f);
            for (int i = 0; i < Mathf.Min(fadeLength, samples.Length); i++)
            {
                float t = i / (float)fadeLength;
                float gain = startingVolume + totalRecovereVolume * t; // fade from 'startingVolume' to 1.0
                samples[i] *= gain;
            }

            var resampled = Resample(samples, 48000, 44100);
            AudioClip audioClip = AudioClip.Create("Float32Audio", resampled.Length, 1, 44100, false);
            audioClip.SetData(resampled, 0);

#if ENABLE_MICROPHONE_OGG
            // Encode to OGG
            var encodedBytes = audioClip.EncodeToOggVorbis();

            SystemEventController.Instance.DispatchSystemEvent(EventWebMicrophoneRecorderCompleted, encodedBytes);
#endif            
        }

        
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void startOggVorbisRecording(string gameObjectName, string outputMethod);
        [DllImport("__Internal")] private static extern void stopOggVorbisRecording();
#endif
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.Utils
{
    public class SoundSpectrumAnalyzer : MonoBehaviour
    {
        private const int SamplesForSpectrum = 40;

		private static SoundSpectrumAnalyzer _instance;
		public static SoundSpectrumAnalyzer Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType<SoundSpectrumAnalyzer>();
				}
				return _instance;
			}
		}

        private AudioSource _audioSource;
        private float[] _samples = new float[512]; // Use 512, 1024, 2048 etc. depending on your needs
        private float _spectrumValue;
        private bool _isPlaying = false;

        public float SpectrumValue
        {
            get { return _spectrumValue; }
        }

        public void Play(AudioSource audioSource)
        {
            _audioSource = audioSource;
            _isPlaying = true;
        }

        public void Stop()
        {
            _isPlaying = false;
        }

        void Update()
        {
            if (_isPlaying)
            {
                if (_audioSource.isPlaying)
                {
                    GetSpectrumAudioSource();
                    CalculateSpectrumValue();
                }
            }
        }

        void GetSpectrumAudioSource()
        {
            _audioSource.GetSpectrumData(_samples, 0, FFTWindow.Blackman);
        }

        void CalculateSpectrumValue()
        {
            float sum = 0;

            for (int i = 0; i < SamplesForSpectrum; i++)
            {
                sum += _samples[i];
            }

            _spectrumValue = (sum / SamplesForSpectrum) * 100;
        }
    }
}

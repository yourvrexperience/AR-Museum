using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.Utils
{
	public class ImageFader : MonoBehaviour
	{
		public const string EventImageFaderStopFaders = "EventImageFaderStopFaders";
		public const string EventImageFaderFadeCompleted = "EventImageFaderFadeCompleted";

		private bool _activateFadeOut;
		private bool _activateFadeIn;
		private float _timeToFade = 0;
		private float _originalTimeToFade = 0;
		private float _target;

		private CanvasGroup _image;

		public static void CreateImageFader(Transform image, bool isFadeIn, float timeToFade, float target)
		{
			GameObject soundFader = new GameObject();
			soundFader.name = "IMAGE_FADER";
			ImageFader fader = soundFader.AddComponent<ImageFader>();
			CanvasGroup canvasImage = image.GetComponent<CanvasGroup>();
			if (canvasImage == null)
            {
				canvasImage = image.gameObject.AddComponent<CanvasGroup>();
			}
			fader.Init(canvasImage);
			if (isFadeIn)
			{
				fader.FadeIn(timeToFade, target);
			}
			else
			{
				fader.FadeOut(timeToFade, target);
			}
		}

		public void Init(CanvasGroup image)
		{
			_image = image;
			_activateFadeOut = false;
			_activateFadeIn = false;
			_timeToFade = 0;

			SystemEventController.Instance.Event += OnSystemEvent;
		}

		private void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			_image = null;
		}

        public void FadeOut(float timeToFade, float target)
		{
			_activateFadeOut = true;
			_timeToFade = timeToFade;
			_originalTimeToFade = _timeToFade;
			_target = target;
			_image.alpha = _target;
		}

		public void FadeIn(float timeToFade, float target)
		{
			_activateFadeIn = true;
			_timeToFade = timeToFade;
			_originalTimeToFade = _timeToFade;
			_target = target;
			_image.alpha = 0;
		}

		private void FadeInUpdate()
		{
			if (_activateFadeIn)
			{
				if (_image != null)
				{
					_timeToFade -= Time.fixedDeltaTime;
					if (_timeToFade > 0)
					{
						_image.alpha = _target * ((_originalTimeToFade - _timeToFade) / _originalTimeToFade);
					}
					else
					{
						_image.alpha = _target;
						_activateFadeIn = false;
						SystemEventController.Instance.DispatchSystemEvent(EventImageFaderFadeCompleted);
						GameObject.Destroy(this.gameObject);
					}
				}
				else
				{
					_image = null;
					GameObject.Destroy(this.gameObject);
				}
			}
		}

		private void FadeOutUpdate()
		{
			if (_activateFadeOut)
			{
				if (_image != null)
				{
					_timeToFade -= Time.fixedDeltaTime;
					if (_timeToFade > 0)
					{
						_image.alpha = _target * (_timeToFade / _originalTimeToFade);
					}
					else
					{
						_image.alpha = 0;
						_activateFadeOut = false;
						_image = null;
						SystemEventController.Instance.DispatchSystemEvent(EventImageFaderFadeCompleted);
						GameObject.Destroy(this.gameObject);
					}
				}
				else
				{
					_image = null;
					GameObject.Destroy(this.gameObject);
				}
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(EventImageFaderStopFaders))
			{
				if (_image != null) _image.alpha = 1;
				GameObject.Destroy(this.gameObject);
			}
		}


		void FixedUpdate()
		{
			FadeInUpdate();
			FadeOutUpdate();
		}
	}
}

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using yourvrexperience.Narration;
using yourvrexperience.Networking;
using yourvrexperience.Utils;
using static yourvrexperience.Narration.NarrationController;

namespace yourvrexperience.template6dof
{
	public class ScreenHUDEasterEggView : BaseScreenView, IScreenView
	{
		public const string EventScreenHUDEasterEggViewEnable = "EventScreenHUDEasterEggViewEnable";
		public const string EventScreenHUDEasterEggViewTriggerResume = "EventScreenHUDEasterEggViewTriggerResume";
		public const string ScreenName = "ScreenHUDEasterEggView";

		[SerializeField] private Button buttonResume;

		[SerializeField] private GameObject LabelSubtitles;
		[SerializeField] private GameObject LabelNoSubtitles;
		[SerializeField] private TextMeshProUGUI description;
		[SerializeField] private TextMeshProUGUI title;

		[SerializeField] private Image containerImage;
		[SerializeField] private Button maximizeImage;
		[SerializeField] private Button previousImage;
		[SerializeField] private Button nextImage;

		[SerializeField] private POIVideoController videoController;
		
		private float _timeAcum = 0;
		private float _totalTime = 0;
		private bool _isVisible = true;
		private string _videoName = "";
		private int _currentImage = 0;
		private List<Sprite> _images;

		private int CurrentImage
		{
			get { return _currentImage; }
			set
			{
				_currentImage = value;
				if (_currentImage < _images.Count)
				{
					if (_currentImage < 0)
					{
						_currentImage = _images.Count - 1;
						containerImage.overrideSprite = _images[_currentImage];
					}
				}
				else
				{
					_currentImage = 0;
				}
				containerImage.overrideSprite = _images[_currentImage];
			}
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			_totalTime = (int)parameters[0];

			if ((parameters.Length > 1) && (title != null))
			{
				title.text = (string)parameters[1];
			}

			SystemEventController.Instance.Event += OnSystemEvent;
			UIEventController.Instance.Event += OnUIEvent;
#if ENABLE_NETWORKING			
			NetworkController.Instance.NetworkEvent += OnNetworkEvent;
#endif
			buttonResume.onClick.AddListener(OnButtonResume);
			buttonResume.gameObject.SetActive(false);

			LabelSubtitles.gameObject.SetActive(false);
			LabelNoSubtitles.gameObject.SetActive(false);
			description.text = "";

			videoController.gameObject.SetActive(false);			

			containerImage.gameObject.SetActive(false);
			maximizeImage.onClick.AddListener(OnMaximizeImageClicked);			
			previousImage.onClick.AddListener(OnPreviousImageClicked);
			nextImage.onClick.AddListener(OnNextImageClicked);
		}

        public override void Destroy()
		{
			base.Destroy();

			DisableAllContent();

			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
#if ENABLE_NETWORKING
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
#endif
		}

        private void OnNextImageClicked()
        {
            CurrentImage++;
        }

        private void OnPreviousImageClicked()
        {
            CurrentImage--;
        }

		private void OnMaximizeVideoClicked()
		{
			
		}
		
		private void OnMaximizeImageClicked()
		{
			SystemEventController.Instance.DispatchSystemEvent(POIPhotoGalleryController.EventPOIPhotoGalleryControllerMinimize, CurrentImage);
			containerImage.gameObject.SetActive(false);
		}

		private void DisableAllContent()
		{
			if (videoController.IsPlaying()) videoController.Pause();
			containerImage.gameObject.SetActive(false);
			videoController.gameObject.SetActive(false);			
			_images = null;
		}
		
		private void OnButtonResume()
		{
			SystemEventController.Instance.DispatchSystemEvent(LevelView.EventLevelViewDestroyEasterEgg);
			SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerDestroyNoMainNarrations);
			SystemEventController.Instance.DispatchSystemEvent(NarrationToken.EventNarrationTokenDestroyNarrationObject, true);
			UIEventController.Instance.DispatchUIEvent(ScreenPauseView.EventScreenPauseViewResumeGame);
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(GameStateRun.EventGameStateRunShowResumeButtonInEasterEgg))
            {
				buttonResume.gameObject.SetActive(true);
			}
			if (nameEvent.Equals(EventScreenHUDEasterEggViewEnable))
			{
				bool visible = (bool)parameters[0];
				_isVisible = visible;
				buttonResume.gameObject.SetActive(_isVisible);
				LabelSubtitles.gameObject.SetActive(_isVisible);
				LabelNoSubtitles.gameObject.SetActive(_isVisible);
				description.text = "";
			}
			if (nameEvent.Equals(EventScreenHUDEasterEggViewTriggerResume))
			{
				OnButtonResume();
			}
			if (nameEvent.Equals(POIPhotoGalleryController.EventPOIPhotoGalleryControllerMaximize))
			{
				bool isEasterEgg = (bool)parameters[0];
				if (isEasterEgg)
				{					
					_images = (List<Sprite>)parameters[1];
					containerImage.gameObject.SetActive(true);
					CurrentImage = (int)parameters[2];
					if (_images.Count > 1)
					{
						previousImage.gameObject.SetActive(true);
						nextImage.gameObject.SetActive(true);
					}
					else
					{
						previousImage.gameObject.SetActive(false);
						nextImage.gameObject.SetActive(false);
					}					
				}
			}
			if (nameEvent.Equals(POIVideoController.EventPOIVideoControllerMaximize))
			{
				bool isEasterEgg = (bool)parameters[0];
				if (isEasterEgg)
				{
					string video = (string)parameters[1];
					string videoURL = (string)parameters[2];
					float time = (float)parameters[3];
					float duration = (float)parameters[4];
					videoController.gameObject.SetActive(true);
					videoController.PlayScreenspace(video, videoURL, time, duration);
				}
			}		
			if (nameEvent.Equals(POIVideoController.EventPOIVideoControllerMinimize))
			{
				videoController.Pause();
				videoController.gameObject.SetActive(false);
			}
		}

		private void OnUIEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(NarrationToken.NarrationTokenViewUpdateText))
			{
				bool isMainNarration = (bool)parameters[0];
				if (!isMainNarration)
				{
					if (_isVisible)
					{
						if (GameLevelData.Instance.SubtitlesActivated)
						{
							LabelSubtitles.gameObject.SetActive(true);
							LabelNoSubtitles.gameObject.SetActive(false);
							description.text = (string)parameters[1];
						}
						else
						{
							LabelSubtitles.gameObject.SetActive(false);
							LabelNoSubtitles.gameObject.SetActive(true);
							description.text = "";
						}
					}
				}
			}
        }

#if ENABLE_NETWORKING
        private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
        {
			if (nameEvent.Equals(NarrationToken.EventNarrationTokenDestroyNarrationObject))
			{
				OnButtonResume();
			}
		}
#endif

		private void ResumeInFarAway()
		{
			float distanceToTarget = Vector3.Distance(MainController.Instance.PlayerView.transform.position, MainController.Instance.ReferenceEasterEgg.transform.position);			
			if (distanceToTarget > MainController.Instance.RangeDetectionEasterEggPlaying)
			{
				OnButtonResume();
			}
		}

		void Update()
        {
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL || ENABLE_NIANTICXR || ENABLE_QR_XR || ENABLE_QR_META)
			this.transform.forward =  (this.transform.position - MainController.Instance.PlayerView.transform.position).normalized;
#endif

#if (!ENABLE_QR_MODE && !ENABLE_QR_XR && !ENABLE_QR_META) || UNITY_EDITOR
			ResumeInFarAway();
#endif			
			if (_isVisible)
			{			
				if (_totalTime > 0)
				{
					_timeAcum += Time.deltaTime;
					if (_timeAcum > _totalTime)
					{
						_totalTime = -1;
						buttonResume.gameObject.SetActive(true);
					}
				}
			}
#if UNITY_EDITOR
			if (Input.GetKeyDown(KeyCode.Space))
			{
				OnButtonResume();
			}
#endif
		}
	}
}
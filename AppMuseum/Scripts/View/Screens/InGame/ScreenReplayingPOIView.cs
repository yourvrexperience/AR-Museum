using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using yourvrexperience.Narration;
using yourvrexperience.Networking;
using yourvrexperience.Utils;
using static yourvrexperience.Narration.NarrationController;
using static yourvrexperience.template6dof.LevelView;

namespace yourvrexperience.template6dof
{
	public class ScreenReplayingPOIView : BaseScreenView, IScreenView
	{	
		public const string EventScreenReplayingPOIViewDestroy = "EventScreenReplayingPOIViewDestroy";	
		public const string EventScreenReplayingPOIViewSignalDestruction = "EventScreenReplayingPOIViewSignalDestruction";	
		public const string EventScreenReplayingPOIViewNetworkReplayCompleted = "EventScreenReplayingPOIViewNetworkReplayCompleted";	
		public const string EventScreenReplayingPOIViewNetworkPlayButton = "EventScreenReplayingPOIViewNetworkPlayButton";	
		public const string EventScreenReplayingPOIViewReplay = "EventScreenReplayingPOIViewReplay";	
		
		public const string ScreenName = "ScreenReplayingPOIView";

		[SerializeField] private Button buttonResume;
		[SerializeField] private TextMeshProUGUI titleScreen;
		[SerializeField] private TextMeshProUGUI descriptionScreen;
		[SerializeField] private Image ProgressBar;

		[SerializeField] private Image containerImage;
		[SerializeField] private Button maximizeImage;
		[SerializeField] private Button previousImage;
		[SerializeField] private Button nextImage;

		[SerializeField] private VideoPlayer videoPlayer;
		[SerializeField] private Button previousVideo;
		[SerializeField] private Button maximizeVideo;
		[SerializeField] private Button nextVideo;	

		[SerializeField] private Image iconPause;	
		[SerializeField] private Image iconWait;	
		[SerializeField] private Image iconPlay;
		[SerializeField] private Image iconWalk;

		[SerializeField] protected Button buttonSkip;
		[SerializeField] protected Button buttonRestart;		
	
		private NarrationData _narrationReplaying;
		private bool _checkTimeProgress = false;
		private float _totalTimeNarration = 0;
		private float _currentTimeNarration = -1;
		private int _poiIndex;
		private NarrationController _narrationController = null;
		private POIData _replayPOI;
		private bool _isPlayingPOI = false;

		public override string NameScreen
		{
			get { return ScreenName; }
		}

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

			_poiIndex = (int)parameters[0];
			_narrationReplaying = (NarrationData)parameters[1];
			_replayPOI = (POIData)parameters[2];
			titleScreen.text = LanguageController.Instance.GetText(_narrationReplaying.TitleNarration);
			descriptionScreen.text = "";

			SystemEventController.Instance.Event += OnSystemEvent;
			UIEventController.Instance.Event += OnUIEvent;
			NetworkController.Instance.NetworkEvent += OnNetworkEvent;

			buttonResume.onClick.AddListener(OnButtonPlay);
			buttonSkip.onClick.AddListener(OnButtonResume);
			buttonRestart.onClick.AddListener(OnRestart);

			videoPlayer.gameObject.SetActive(false);
			previousVideo.onClick.AddListener(OnPreviousVideoClicked);
			maximizeVideo.onClick.AddListener(OnMaximizeVideoClicked);
			nextVideo.onClick.AddListener(OnNextVideoClicked);

			containerImage.gameObject.SetActive(false);
			maximizeImage.onClick.AddListener(OnMaximizeImageClicked);
			previousImage.onClick.AddListener(OnPreviousImageClicked);
			nextImage.onClick.AddListener(OnNextImageClicked);					

			MainController.Instance.GuideTourView.SetPositionOutsideNarration(_replayPOI.GOPosition.transform.position);			

			_narrationController = MainController.Instance.CreateNarrationPreviousPOI(_narrationReplaying);			
			
			iconWalk.gameObject.SetActive(false);

			AudioClip audioNarration = SpeechDatabaseController.Instance.GetSpeechDataByID(-1, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _poiIndex, 0, LanguageController.Instance.CodeLanguage);
			if (audioNarration == null)
			{
				List<NarrationToken> firstSegment = new List<NarrationToken>();
				firstSegment.Add(_narrationReplaying.Segments[0]);
				SystemEventController.Instance.DispatchSystemEvent(EventNarrationControllerDownloadPOIAudios, _poiIndex, firstSegment);
				buttonResume.interactable = false;
				buttonSkip.interactable = false;
				buttonRestart.interactable = false;				
				iconPause.gameObject.SetActive(false);
				iconPlay.gameObject.SetActive(false);
				iconWait.gameObject.SetActive(true);
				MainController.Instance.GuideTourView.RunAnimationIdle();
			}
			else
			{
				_narrationController.Play(0);
				_isPlayingPOI = true;
				iconPause.gameObject.SetActive(true);
				iconWait.gameObject.SetActive(false);
				iconPlay.gameObject.SetActive(false);
				MainController.Instance.GuideTourView.RunAnimationTalk();
			}

			if (MainController.Instance.IsMultiplayer)
			{
				if (!NetworkController.Instance.IsServer)
				{
					buttonResume.interactable = false;
					buttonSkip.interactable = false;
					buttonRestart.interactable = false;
				}
			}
		}

		private void OnButtonPlay()
		{
			if (iconWalk.gameObject.activeSelf)
			{
				OnButtonResume();
			}
			else
			{				
				if (MainController.Instance.IsMultiplayer)
				{
					if (NetworkController.Instance.IsServer)
					{
						NetworkController.Instance.DispatchNetworkEvent(EventScreenReplayingPOIViewNetworkPlayButton, -1, -1, _isPlayingPOI);
					}
				}
				else
				{					
					RunPlayButton();
				}				
			}
		}

		private void RunPlayButton()
		{
			_isPlayingPOI = !_isPlayingPOI;
			if (_isPlayingPOI)
			{
				iconPause.gameObject.SetActive(true);
				iconWait.gameObject.SetActive(false);
				iconPlay.gameObject.SetActive(false);
				_narrationController.Resume();
				_checkTimeProgress = true;
				MainController.Instance.GuideTourView.RunAnimationTalk();
			}
			else
			{
				iconPause.gameObject.SetActive(false);
				iconWait.gameObject.SetActive(false);
				iconPlay.gameObject.SetActive(true);
				_narrationController.Pause();
				_checkTimeProgress = false;
				MainController.Instance.GuideTourView.RunAnimationIdle();
			}
		}

        private void OnButtonResume()
        {			
			if (MainController.Instance.IsMultiplayer)
			{
				if (NetworkController.Instance.IsServer)
				{
					NetworkController.Instance.DispatchNetworkEvent(EventScreenReplayingPOIViewNetworkReplayCompleted, -1, -1);
				}
			}
			else
			{
				OnButtonRealResume();
			}
		}

		private void OnRestart()
		{
			int	poiIndex = _poiIndex;
			NarrationData narrationReplaying = _narrationReplaying;

			SystemEventController.Instance.DelaySystemEvent(EventScreenReplayingPOIViewReplay, 0.2f, poiIndex, narrationReplaying);
			OnButtonRealResume();
		}

		private void OnButtonRealResume()
		{
			POIData replayPOI = _replayPOI;
			MainController.Instance.GuideTourView.RunAnimationIdle();
			SystemEventController.Instance.DispatchSystemEvent(EventScreenReplayingPOIViewSignalDestruction, replayPOI);
			SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerDestroyNoMainNarrations);
			SystemEventController.Instance.DispatchSystemEvent(NarrationToken.EventNarrationTokenDestroyNarrationObject, true);
			UIEventController.Instance.DispatchUIEvent(ScreenPauseView.EventScreenPauseViewResumeGame);
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)							
			SystemEventController.Instance.DelaySystemEvent(ScreenInfoNextButtonView.EventScreenInfoNextButtonViewVisibilityContent, 0.1f, false);
#endif			
        }

		public override void Destroy()
		{
			base.Destroy();
			if (_narrationController != null)
			{
				_narrationController.Destroy();
				_narrationController = null;
			}
			_narrationReplaying = null;
			_replayPOI = null;
			DisableAllContent();
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
			SystemEventController.Instance.DispatchSystemEvent(GameStateRun.EventGameStateRunNavigateToCurrentPOI);
		}

		private void OnMaximizeVideoClicked()
		{
			SystemEventController.Instance.DispatchSystemEvent(POIVideoController.EventPOIVideoControllerMinimize, (float)videoPlayer.time);
			videoPlayer.Pause();
			videoPlayer.gameObject.SetActive(false);
			videoPlayer.gameObject.SetActive(false);
		}
		
		private void OnMaximizeImageClicked()
		{
			SystemEventController.Instance.DispatchSystemEvent(POIPhotoGalleryController.EventPOIPhotoGalleryControllerMinimize, CurrentImage);
			containerImage.gameObject.SetActive(false);
		}

		private void DisableAllContent()
		{
			if (videoPlayer.isPlaying) videoPlayer.Pause();
			containerImage.gameObject.SetActive(false);
			videoPlayer.gameObject.SetActive(false);	
			_images = null;		
		}
		
		private void OnNextImageClicked()
        {
            CurrentImage++;
        }

        private void OnPreviousImageClicked()
        {
            CurrentImage--;
        }

		private void OnNextVideoClicked()
		{
			if (videoPlayer.time + POIVideoController.JumpTime < videoPlayer.clip.length)
            {
				videoPlayer.time += POIVideoController.JumpTime;
			}			
		}

		private void OnPreviousVideoClicked()
		{
			if (videoPlayer.time - POIVideoController.JumpTime < 0)
			{
				videoPlayer.time = 0;
			}
			else
            {
				videoPlayer.time -= POIVideoController.JumpTime;
			}
		}

		private void OnUIEvent(string nameEvent, object[] parameters)
		{
			 if (nameEvent.Equals(NarrationToken.NarrationTokenViewUpdateText))
			{
				bool isMainNarration = (bool)parameters[0];
				if (!isMainNarration)
				{
					if (GameLevelData.Instance.SubtitlesActivated)
					{
						descriptionScreen.text = (string)parameters[1];
					}
					else
					{
						descriptionScreen.text = "";
					}
				}
			}
			if (nameEvent.Equals(ScreenNarrationNextButtonView.EventScreenNarrationNextButtonViewButtonVisibility))
			{
				bool isMainNarration = (bool)parameters[0];
				if (!isMainNarration)
				{
					bool activatedButton = (bool)parameters[1];
					buttonResume.gameObject.SetActive(activatedButton);
					if (activatedButton)
					{
						ProgressBar.gameObject.SetActive(false);
					}
				}
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(SpeechDatabaseController.EventSpeechDatabaseControllerAvailableSpeech))
			{
				AudioClip audioNarration = SpeechDatabaseController.Instance.GetSpeechDataByID(-1, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _poiIndex, 0, LanguageController.Instance.CodeLanguage);
				if (buttonResume.interactable == false)
				{
					if (audioNarration != null)
					{
						buttonResume.interactable = true;
						buttonSkip.interactable = true;
						buttonRestart.interactable = true;						
						_narrationController.Play(0);	
						_isPlayingPOI = true;					
						iconPause.gameObject.SetActive(true);
						iconWait.gameObject.SetActive(false);
						iconPlay.gameObject.SetActive(false);	
						MainController.Instance.GuideTourView.RunAnimationTalk();					
					}
					if (MainController.Instance.IsMultiplayer)
					{
						if (!NetworkController.Instance.IsServer)
						{
							buttonResume.interactable = false;
							buttonSkip.interactable = false;
							buttonRestart.interactable = false;
						}
					}
				}
			}
			if (nameEvent.Equals(EventScreenReplayingPOIViewDestroy))
			{ 				
				OnButtonResume();
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerPlayInfo))
			{
				bool mainNarration = (bool)parameters[0];
				if (!mainNarration)
				{					
					if (_currentTimeNarration == -1)
					{
						_totalTimeNarration = (float)parameters[1];
						_checkTimeProgress = true;
						_currentTimeNarration = 0;
					}
				}
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
					if (!_videoName.Equals(video))
					{
						_videoName = video;
						VideoClip videoClip = AssetBundleController.Instance.CreateVideoclip(_videoName);
						if (videoClip != null)
						{
							videoPlayer.clip = videoClip;
						}
					}
					float time = (float)parameters[2];
					videoPlayer.gameObject.SetActive(true);
					videoPlayer.time = time;
					videoPlayer.Play();
				}
			}			
        }

		private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
        {
            if (nameEvent.Equals(EventScreenReplayingPOIViewNetworkReplayCompleted))
			{
				OnButtonRealResume();
			}
			if (nameEvent.Equals(EventScreenReplayingPOIViewNetworkPlayButton))
			{
				_isPlayingPOI = (bool)parameters[0];
				RunPlayButton();
			}
        }

		private void UpdateProgressBar(float progress)
        {
			ProgressBar.fillAmount = progress;
		}

		private void ResumeInFarAway()
		{
			float distanceToTarget = Vector3.Distance(MainController.Instance.PlayerView.transform.position, _replayPOI.GOPosition.transform.position);
			float limitDistance = GameLevelData.Instance.DistanceToTriggerGuide;
			if (distanceToTarget > limitDistance)
			{
				OnButtonResume();
			}
		}

		void Update()
        {
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)				
			Vector3 forwardGuide = -MainController.Instance.GuideTourView.GetModel().transform.forward;
			GameObject contentVRScreen = MainController.Instance.GuideTourView.ScreenVR;
			Vector3 posScreen = contentVRScreen.transform.position;
			this.transform.position = posScreen;
			this.transform.forward = forwardGuide;
#endif

			if (_checkTimeProgress)
			{
				if (_totalTimeNarration > 0)
				{
					_currentTimeNarration += Time.deltaTime;
					float progress = _currentTimeNarration / _totalTimeNarration;
					UpdateProgressBar(progress);
					if (progress > 1)
					{
						_checkTimeProgress = false;						
						iconWalk.gameObject.SetActive(true);
						iconPause.gameObject.SetActive(false);
						iconPlay.gameObject.SetActive(false);
						iconWait.gameObject.SetActive(false);
						MainController.Instance.GuideTourView.RunAnimationIdle();
					}
				}
			}
			else
			{
				if (_currentTimeNarration > 0)
				{
					if (!MainController.Instance.IsMultiplayer)
					{
						// ResumeInFarAway();
					}
				}				
			}
		}
    }
}
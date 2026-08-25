using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Narration;
using yourvrexperience.Networking;
using yourvrexperience.Utils;
using static yourvrexperience.template6dof.LevelView;
using static yourvrexperience.Narration.NarrationController;
using System.Collections.Generic;
using UnityEngine.Video;

namespace yourvrexperience.template6dof
{
	public class ScreenInfoNextButtonView : ScreenNarrationNextButtonView, IScreenView
	{		
		public const string EventScreenInfoNextButtonViewVisibilityContent = "EventScreenInfoNextButtonViewVisibilityContent";

		public const string ScreenName = "ScreenInfoNextButtonView";

		[SerializeField] private Image containerImage;
		[SerializeField] private Button maximizeImage;
		[SerializeField] private Button previousImage;
		[SerializeField] private Button nextImage;

		[SerializeField] private VideoPlayer videoPlayer;
		[SerializeField] private Button previousVideo;
		[SerializeField] private Button maximizeVideo;
		[SerializeField] private Button nextVideo;

		private POIData _currentPOI;
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
		
		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			videoPlayer.gameObject.SetActive(false);
			previousVideo.onClick.AddListener(OnPreviousVideoClicked);
			maximizeVideo.onClick.AddListener(OnMaximizeVideoClicked);
			nextVideo.onClick.AddListener(OnNextVideoClicked);

			containerImage.gameObject.SetActive(false);
			maximizeImage.onClick.AddListener(OnMaximizeImageClicked);
			previousImage.onClick.AddListener(OnPreviousImageClicked);
			nextImage.onClick.AddListener(OnNextImageClicked);			
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

		protected override void UpdateIconButton(TypeActionNext action)
        {
			base.UpdateIconButton(action);
			switch (_action)
            {
				case TypeActionNext.Play:
					SystemEventController.Instance.DispatchSystemEvent(TourGuideView.EventTourGuideViewSpeakActivation, false);
					SystemEventController.Instance.DispatchSystemEvent(POIReplayView.EventPOIReplayViewEnablePOIs, true);
					break;
					
				case TypeActionNext.Pause:
					SystemEventController.Instance.DispatchSystemEvent(TourGuideView.EventTourGuideViewSpeakActivation, true);
					SystemEventController.Instance.DispatchSystemEvent(POIReplayView.EventPOIReplayViewEnablePOIs, false);
					break;

				case TypeActionNext.Walk:
					SystemEventController.Instance.DispatchSystemEvent(TourGuideView.EventTourGuideViewSpeakActivation, false);
					SystemEventController.Instance.DispatchSystemEvent(POIReplayView.EventPOIReplayViewEnablePOIs, false);
					break;
			}
        }

		protected override void OnButtonPause()
		{
			if (!_isMultiplayer)
			{
				UIEventController.Instance.DispatchUIEvent(GameStateRun.EventGameStateRunTriggerPause);            
			}
			else
			{
				if (_enablePauseAccess)
				{
					UIEventController.Instance.DispatchUIEvent(GameStateRun.EventGameStateRunTriggerPause);
				}
			}
		}

		protected override void OnButtonAIInteraction()
		{
			UIEventController.Instance.DispatchUIEvent(GameStateRun.EventGameStateRunAIInteraction);            
		}

		protected override void OnSkipNext()
		{
			SystemEventController.Instance.DispatchSystemEvent(POIReplayView.EventPOIReplayViewEnablePOIs, true);
			SystemEventController.Instance.DispatchSystemEvent(LevelView.EventLevelViewUnlockEasterEggs);
			if (_isMultiplayer)
			{
				if (NetworkController.Instance.IsServer)
				{
					NetworkController.Instance.DelayNetworkEvent(GameStateRun.EventGameStateRunEndCurrentNarration, 0.01f, -1, -1);
				}
			}
			else
			{
				UIEventController.Instance.DispatchUIEvent(GameStateRun.EventGameStateRunEndCurrentNarration, _currentPOI);
			}			
		}

        protected override void OnRestart()
        {
			if (_isMultiplayer)
			{
				if (NetworkController.Instance.IsServer)
				{
					NetworkController.Instance.DelayNetworkEvent(NarrationController.EventNarrationControllerDoRestart, 0.01f, -1, -1);
				}
			}
			else
			{
				SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerDoRestart, _currentPOI);
			}
        }

		public override void Destroy()
		{
			base.Destroy();
            _currentPOI = null;
		}
		
		protected override void OnSystemEvent(string nameEvent, object[] parameters)
        {
			base.OnSystemEvent(nameEvent, parameters);

			if (nameEvent.Equals(EventScreenInfoNextButtonViewVisibilityContent))
			{				
				Content.gameObject.SetActive((bool)parameters[0]);
			}			
			if (nameEvent.Equals(NarrationController.EventNarrationControllerRequestButtonNextAction))
			{
				DisableAllContent();
			}			
			if (nameEvent.Equals(NarrationToken.EventNarrationTokenStart))
			{
				if ((bool)parameters[0])
				{
					RefreshNetworkVisibility();
                    int poiIndex = (int)parameters[1];
                    float totalTimeNarration = (float)parameters[2];
                    string startEventNarration = (string)parameters[3];
					if (startEventNarration.Equals(TourGuideView.EventPOI0Animate01))
					{					
						HideAllIcons();
						IconsInfo[0].gameObject.SetActive(true);
					}
					if (startEventNarration.Equals(TourGuideView.EventPOI0Animate02))
					{
						HideAllIcons();
						IconsInfo[1].gameObject.SetActive(true);
					}
					if (startEventNarration.Equals(TourGuideView.EventPOI0Animate03))
					{
						HideAllIcons();
					}
				}
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerResponseAction))
			{
				_action = (TypeActionNext)parameters[0];
				if (_hasRequestedNextButton)
				{
					_hasRequestedNextButton = false;
					SystemEventController.Instance.DispatchSystemEvent(LevelView.EventLevelViewUnlockEasterEggs);
					if (_isMultiplayer)
					{
						if (NetworkController.Instance.IsServer)
						{
							NetworkController.Instance.DelayNetworkEvent(GameStateRun.EventGameStateRunNetworkStartNarration, 0.01f, -1, -1, _action.ToString());
						}
					}
					else
					{
						UIEventController.Instance.DispatchUIEvent(GameStateRun.EventGameStateRunTriggerNextButton, _action);  
					}					
				}				
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerPlayInfo))
			{
				bool mainNarration = (bool)parameters[0];
				if (mainNarration)
				{
					_currentPOI = MainController.Instance.LevelView.CurrentPOI;
				}
			}
			if (nameEvent.Equals(TourGuideView.EventTourGuideViewReachedTarget))
            {
                SystemEventController.Instance.DelaySystemEvent(TourGuideView.EventTourGuideViewSpeakActivation, 0.1f, true);				
            }
			if (nameEvent.Equals(EventLevelViewDestroy))
			{
				UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);				
			}	
			if (nameEvent.Equals(POIPhotoGalleryController.EventPOIPhotoGalleryControllerMaximize))
			{
				bool isEasterEgg = (bool)parameters[0];
				if (!isEasterEgg)
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
				if (!isEasterEgg)
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

        protected override void OnButtonNext()
        {
			if (_action == TypeActionNext.Walk)
			{
				SystemEventController.Instance.DispatchSystemEvent(POIReplayView.EventPOIReplayViewEnablePOIs, true);
			}
			base.OnButtonNext();
        }

		protected override void Update()
        {
#if UNITY_EDITOR
            if (_content.gameObject.activeSelf)
            {
				if (Input.GetKeyDown(KeyCode.Space))
				{
					OnButtonNext();
				}
				if (Input.GetKeyDown(KeyCode.Space) && Input.GetKey(KeyCode.LeftShift))
				{
					UIEventController.Instance.DispatchUIEvent(GameStateRun.EventGameStateRunEndCurrentNarration, _currentPOI);
				}
			}
#endif
			base.Update();

			switch (_action)
			{
				case TypeActionNext.Play:
					if (!MainController.Instance.MainNarrationPlaying)
					{
						float distanceToTarget = Vector3.Distance(MainController.Instance.PlayerView.transform.position, MainController.Instance.LevelView.CurrentPOI.Root.transform.position);
						float distanceReference = GameLevelData.Instance.DistanceToTriggerGuide * 1.5f;
						CheckDistanceToPlayer(distanceToTarget, distanceReference);
					}
					break;
			}
		}
	}
}
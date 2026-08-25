using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using yourvrexperience.Narration;
using yourvrexperience.Utils;

namespace yourvrexperience.template6dof
{
	public class ScreenHUDEggVideoView : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenHUDEggVideoView";
		public const string EventScreenHUDEggVideoViewForceMinimize = "EventScreenHUDEggVideoViewForceMinimize";

		[SerializeField] private Button buttonResume;

		[SerializeField] private VideoPlayer videoPlayer;
		[SerializeField] private Button previous;
		[SerializeField] private Button maximize;
		[SerializeField] private Button next;

		private string _video = "";
		private float _timeAcum = 0;
		private float _totalTime = 0;
		private bool _isVisible = true;
		private bool _isEasterEgg = false;

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			_totalTime = (int)parameters[0];
			_isEasterEgg = (bool)parameters[1];

			SystemEventController.Instance.Event += OnSystemEvent;

			buttonResume.onClick.AddListener(OnButtonResume);
			buttonResume.gameObject.SetActive(false);

			previous.onClick.AddListener(OnPreviousClicked);
			maximize.onClick.AddListener(OnMaximizeClicked);
			next.onClick.AddListener(OnNextClicked);

			videoPlayer.gameObject.SetActive(false);
		}

        public override void Destroy()
		{
			base.Destroy();

			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		private void OnButtonResume()
		{
			if (_isEasterEgg)
			{
				SystemEventController.Instance.DispatchSystemEvent(LevelView.EventLevelViewDestroyEasterEgg);
				UIEventController.Instance.DispatchUIEvent(ScreenPauseView.EventScreenPauseViewResumeGame);
			}
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
		}

		private void OnMaximizeClicked()
		{
			SystemEventController.Instance.DispatchSystemEvent(POIVideoController.EventPOIVideoControllerMinimize, (float)videoPlayer.time);
			videoPlayer.Pause();
			videoPlayer.gameObject.SetActive(false);
		}

		private void OnNextClicked()
		{
			if (videoPlayer.time + POIVideoController.JumpTime < videoPlayer.clip.length)
            {
				videoPlayer.time += POIVideoController.JumpTime;
			}			
		}

		private void OnPreviousClicked()
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

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(POIVideoController.EventPOIVideoControllerMaximize))
			{
				bool isEasterEgg = (bool)parameters[0];
				if (isEasterEgg)
				{
					string video = (string)parameters[1];
					if (!_video.Equals(video))
					{
						_video = video;
						VideoClip videoClip = AssetBundleController.Instance.CreateVideoclip(video);
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
			if (nameEvent.Equals(EventScreenHUDEggVideoViewForceMinimize))
			{
				OnMaximizeClicked();
			}
			if (nameEvent.Equals(ScreenHUDEasterEggView.EventScreenHUDEasterEggViewTriggerResume))
			{
				OnButtonResume();
			}			
			if (_isEasterEgg)			
			{
				if (nameEvent.Equals(GameStateRun.EventGameStateRunShowResumeButtonInEasterEgg))
				{
					buttonResume.gameObject.SetActive(true);
				}
				if (nameEvent.Equals(ScreenHUDEasterEggView.EventScreenHUDEasterEggViewEnable))
				{
					bool visible = (bool)parameters[0];
					_isVisible = visible;
					buttonResume.gameObject.SetActive(_isVisible);
				}
			}
		}

		private void ResumeInFarAway()
		{
			if (_isEasterEgg)			
			{
				float distanceToTarget = Vector3.Distance(MainController.Instance.PlayerView.transform.position, MainController.Instance.ReferenceEasterEgg.transform.position);
				float distanceReference = GameLevelData.Instance.DistanceToTriggerGuide * 1.5f;
				if (distanceToTarget > distanceReference)
				{
					OnButtonResume();
				}
			}
		}

		void Update()
        {
			ResumeInFarAway();
			
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
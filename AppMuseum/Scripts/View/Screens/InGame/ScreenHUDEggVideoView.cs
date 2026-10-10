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

		[SerializeField] private POIVideoController videoController;

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

			videoController.gameObject.SetActive(false);
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

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
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
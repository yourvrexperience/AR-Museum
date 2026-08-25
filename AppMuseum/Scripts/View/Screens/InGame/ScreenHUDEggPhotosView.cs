using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using yourvrexperience.Narration;
using yourvrexperience.Utils;
using static yourvrexperience.Narration.NarrationController;

namespace yourvrexperience.template6dof
{
	public class ScreenHUDEggPhotosView : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenHUDEggPhotosView";

		[SerializeField] private Button buttonResume;

		[SerializeField] private Image containerImage;
		[SerializeField] private Button previous;
		[SerializeField] private Button maximize;
		[SerializeField] private Button next;

		private List<Sprite> _images;
		private float _timeAcum = 0;
		private float _totalTime = 0;
		private int _currentImage = 0;
		private bool _isVisible = true;
		private bool _isEasterEgg = false;

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
			_isEasterEgg = (bool)parameters[1];

			SystemEventController.Instance.Event += OnSystemEvent;

			buttonResume.onClick.AddListener(OnButtonResume);
			buttonResume.gameObject.SetActive(false);

			previous.onClick.AddListener(OnPreviousClicked);
			maximize.onClick.AddListener(OnMaximizeClicked);
			next.onClick.AddListener(OnNextClicked);

			containerImage.gameObject.SetActive(false);
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
			SystemEventController.Instance.DispatchSystemEvent(POIPhotoGalleryController.EventPOIPhotoGalleryControllerMinimize, CurrentImage);
			containerImage.gameObject.SetActive(false);
		}

		private void OnNextClicked()
		{
			CurrentImage++;
		}

		private void OnPreviousClicked()
		{
			CurrentImage--;
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(POIPhotoGalleryController.EventPOIPhotoGalleryControllerMaximize))
			{
				bool isEasterEgg = (bool)parameters[0];
				if (isEasterEgg)
				{
					_images = (List<Sprite>)parameters[1];
					CurrentImage = (int)parameters[2];
					containerImage.gameObject.SetActive(true);
					if (_images.Count == 1)
					{
						previous.gameObject.SetActive(false);
						next.gameObject.SetActive(false);
					}
				}
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
			
			if (_isEasterEgg)
			{
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
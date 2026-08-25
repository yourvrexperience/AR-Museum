using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_NETWORKING
using yourvrexperience.Networking;
#endif
using yourvrexperience.Utils;
using static yourvrexperience.Narration.NarrationController;

namespace yourvrexperience.Narration
{
	
	public abstract class ScreenNarrationNextButtonView : BaseScreenView, IScreenView
	{		
		public const string EventScreenNarrationNextButtonViewButtonVisibility = "EventScreenNarrationNextButtonViewButtonVisibility";
		public const string EventScreenNarrationNextButtonViewPauseVisibility = "EventScreenNarrationNextButtonViewPauseVisibility";
		public const string EventScreenNarrationNextButtonSubtitlesChangedActivation = "EventScreenNarrationNextButtonSubtitlesChangedActivation";

		[SerializeField] protected GameObject LabelSubtitles;
		[SerializeField] protected GameObject LabelNoSubtitles;
		[SerializeField] protected GameObject LabelContainerTitle;

		[SerializeField] protected GameObject IconPlay;
		[SerializeField] protected GameObject IconPause;
		[SerializeField] protected GameObject IconWalk;
		[SerializeField] protected Image ProgressBar;
		[SerializeField] protected Image[] IconsInfo;

		[SerializeField] protected Button buttonPause;
		[SerializeField] protected Button buttonAIInteraction;
		[SerializeField] protected TextMeshProUGUI titleScreen;
		[SerializeField] protected Button buttonNext;
		[SerializeField] protected Button buttonSkip;
		[SerializeField] protected Button buttonRestart;
		[SerializeField] protected TextMeshProUGUI labelScreen;
		[SerializeField] protected GameObject labelOff;
		[SerializeField] protected GameObject labelOn;
		[SerializeField] protected GameObject iconOff;
		[SerializeField] protected GameObject iconOn;

		protected float _currentTimeNarration = 0;
		protected float _totalTimeNarration = 0;
		protected bool _checkTimeProgress = true;

		protected string _idLanguageDescription = "";
		protected bool _isNarration = false;
		protected bool _isMultiplayer = false;
		protected bool _enablePauseAccess = false;

		public TypeActionNext _action;
		protected bool _hasRequestedNextButton = false;
		protected bool _enableVisibilityOnClose = true;

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			_isMultiplayer = (bool)parameters[0];
			_enablePauseAccess = (bool)parameters[1];
			_idLanguageDescription = (string)parameters[2];
			titleScreen.text = LanguageController.Instance.GetText(_idLanguageDescription);

			buttonPause.onClick.AddListener(OnButtonPause);
			buttonAIInteraction.onClick.AddListener(OnButtonAIInteraction);
			buttonNext.onClick.AddListener(OnButtonNext);

#if ENABLE_NETWORKING
			if (_isMultiplayer)
			{
				if (!_enablePauseAccess)
				{
					buttonPause.gameObject.SetActive(false);
					buttonAIInteraction.gameObject.SetActive(false);
				}				
				if (!NetworkController.Instance.IsServer)
				{
					buttonSkip.gameObject.SetActive(false);
					buttonRestart.gameObject.SetActive(false);
					buttonNext.interactable = false;
				}
			}
#endif

			UIEventController.Instance.Event += OnUIEvent;
			SystemEventController.Instance.Event += OnSystemEvent;

			HideAllIcons();

			labelOff.SetActive(true);
			labelOn.SetActive(false);
			iconOff.SetActive(true);
			iconOn.SetActive(false);

			RefreshSubtitlesPanel();
			UpdateProgressBar(0);
			UpdateIconButton(TypeActionNext.Play);
			
			buttonSkip.onClick.AddListener(OnSkipNext);
			buttonRestart.onClick.AddListener(OnRestart);
		}

        protected void HideAllIcons()
		{
			foreach(Image icon in IconsInfo)
			{
				if (icon != null)
				{
					icon.gameObject.SetActive(false);
				}				
			}
		}

		protected virtual void UpdateIconButton(TypeActionNext action)
        {
			_action = action;
			SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerSetAction, _action);
			switch (_action)
            {
				case TypeActionNext.Play:
					IconPlay.SetActive(true);
					IconPause.SetActive(false);
					IconWalk.SetActive(false);
					break;
					
				case TypeActionNext.Pause:
					iconOff.SetActive(false);
					iconOn.SetActive(true);		

					labelOff.SetActive(false);
					labelOn.SetActive(true);

					IconPlay.SetActive(false);
					IconPause.SetActive(true);
					IconWalk.SetActive(false);
					break;

				case TypeActionNext.Walk:
					UpdateProgressBar(0);
					IconPlay.SetActive(false);
					IconPause.SetActive(false);
					IconWalk.SetActive(true);
					break;
			}
        }

		protected void RefreshSubtitlesPanel()
        {
			LabelSubtitles.SetActive(GameLevelData.Instance.SubtitlesActivated);
			LabelNoSubtitles.SetActive(!GameLevelData.Instance.SubtitlesActivated);
        }

		protected void UpdateProgressBar(float progress)
        {
			ProgressBar.fillAmount = progress;
		}

        protected abstract void OnButtonPause();

 		protected abstract void OnButtonAIInteraction();

		protected abstract void OnSkipNext();

        protected abstract void OnRestart();

        protected virtual void OnButtonNext()
        {
			if (_isMultiplayer)
			{
#if ENABLE_NETWORKING
				if (NetworkController.Instance.IsServer)
				{
					_hasRequestedNextButton = true;	
					SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerRequestAction);
				}
#endif				
			}
			else
			{
				_hasRequestedNextButton = true;
				SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerRequestAction);
			}
        }

		public override void Destroy()
		{
			base.Destroy();
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}
		
		protected virtual void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(NarrationController.EventNarrationControllerRequestButtonNextAction))
			{
				OnButtonNext();
			}			
			if (nameEvent.Equals(NarrationController.EventNarrationControllerPlayInfo))
			{
				bool mainNarration = (bool)parameters[0];
				if (mainNarration)
				{
					_totalTimeNarration = (float)parameters[1];
					UpdateIconButton(TypeActionNext.Pause);
					_checkTimeProgress = true;
				}
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerPaused))
			{
				bool isUserAction = (bool)parameters[0];
				if (isUserAction)
                {
					UpdateIconButton(TypeActionNext.Play);
				}
				_checkTimeProgress = false;
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerFinished))
			{
				UpdateIconButton(TypeActionNext.Walk);
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerUpdateTexts))
            {
				if (!_isNarration)
                {
					titleScreen.text = LanguageController.Instance.GetText(_idLanguageDescription);
				}				
			}
			if (nameEvent.Equals(EventScreenNarrationNextButtonViewPauseVisibility))
			{
				bool enablePauseAccess = (bool)parameters[0];
				buttonAIInteraction.gameObject.SetActive(enablePauseAccess);
			}		
			if (nameEvent.Equals(NarrationController.EventNarrationControllerConfirmedRestart))	
			{
				bool isPlaying = (bool)parameters[0];
				_currentTimeNarration = 0;
				UpdateProgressBar(_currentTimeNarration);
				UpdateIconButton(isPlaying?TypeActionNext.Pause:TypeActionNext.Play);
				OnButtonNext();
			}
		}

		protected virtual void OnUIEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(NarrationToken.NarrationTokenViewUpdateText))
			{
				bool isMainNarration = (bool)parameters[0];
				if (isMainNarration)
				{
					titleScreen.text = (string)parameters[1];
					labelScreen.text = (string)parameters[2];
					_isNarration = true;
				}
			}
			if (nameEvent.Equals(EventScreenNarrationNextButtonViewButtonVisibility))
			{
                bool isMainNarration = (bool)parameters[0];
                if (isMainNarration)
                {
                    buttonNext.gameObject.SetActive((bool)parameters[1]);
                    buttonPause.gameObject.SetActive((bool)parameters[1]);
					buttonAIInteraction.gameObject.SetActive((bool)parameters[1]);
					buttonSkip.gameObject.SetActive((bool)parameters[1]);
					buttonRestart.gameObject.SetActive((bool)parameters[1]);
                }
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerUpdateTitleLabel))	
			{
				labelScreen.text = (string)parameters[0];
			}
			if (nameEvent.Equals(EventScreenNarrationNextButtonViewPauseVisibility))
			{
				buttonPause.gameObject.SetActive((bool)parameters[0]);
				buttonAIInteraction.gameObject.SetActive((bool)parameters[0]);
			}
        }

		protected void CheckDistanceToPlayer(float distanceToTarget, float distanceReference)
		{
			if (distanceToTarget > distanceReference * 1.2f)
			{
				if (_enableVisibilityOnClose)
				{
					_enableVisibilityOnClose = false;
					buttonNext.gameObject.SetActive(false);
					buttonSkip.gameObject.SetActive(false);
					buttonRestart.gameObject.SetActive(false);
					LabelSubtitles.gameObject.SetActive(false);
					LabelSubtitles.SetActive(false);
					LabelNoSubtitles.SetActive(false);
					LabelContainerTitle.SetActive(false);
				}
			}
			else
			{
				if (distanceToTarget < distanceReference)
				{
					if (!_enableVisibilityOnClose)
					{
						_enableVisibilityOnClose = true;
						RefreshNetworkVisibility();
					}
				}
			}
		}

		protected void RefreshNetworkVisibility()
		{
			bool shouldShowAll = false;
			if (!_isMultiplayer)
			{
				shouldShowAll = true;
			}
			else
			{
#if ENABLE_NETWORKING							
				if (NetworkController.Instance.IsServer)
				{
					shouldShowAll = true;
				}
#endif							
			}
			buttonNext.gameObject.SetActive(true);
			LabelContainerTitle.SetActive(true);
			RefreshSubtitlesPanel();
			if (shouldShowAll)
			{
				buttonSkip.gameObject.SetActive(true);
				buttonRestart.gameObject.SetActive(true);
			}
		}

		protected void UpdateProgressBarInfo()
		{
			if ((_totalTimeNarration > 0) && _checkTimeProgress)
			{
				_currentTimeNarration += Time.deltaTime;
				UpdateProgressBar(_currentTimeNarration / _totalTimeNarration);
			}
		}

		protected virtual void Update()
        {
			switch (_action)
			{
				case TypeActionNext.Play:
					break;

				case TypeActionNext.Pause:
					UpdateProgressBarInfo();
					break;
			}
		}
	}
}
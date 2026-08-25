using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;
using static yourvrexperience.template6dof.LevelView;

namespace yourvrexperience.template6dof
{
	public class ScreenFoundEasterEggView : BaseScreenView, IScreenView
	{
		public const string EventScreenFoundEasterEggViewDestroy = "EventScreenFoundEasterEggViewDestroy";

		public const string ScreenName = "ScreenFoundEasterEggView";

		[SerializeField] private GameObject popUpContainerScreen;
		[SerializeField] private TextMeshProUGUI titleScreen;
		[SerializeField] private TextMeshProUGUI descriptionScreen;

		[SerializeField] private Button buttonPlay;
		[SerializeField] private GameObject labelContainer;
		[SerializeField] private TextMeshProUGUI labelToPlayScreen;
		[SerializeField] private GameObject labelOff;
		[SerializeField] private GameObject labelOn;
		[SerializeField] private GameObject iconOff;
		[SerializeField] private GameObject iconOn;
		[SerializeField] private Button VRButtonInteraction;

		private bool _hasBeenFirstClicked = false;
		private EasterEgg _easterEgg;
		private EasterEgg _easterVREgg;

		public override string NameScreen
		{
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			titleScreen.text = (string)parameters[0];
			descriptionScreen.text = (string)parameters[1];
			_easterEgg = (EasterEgg)parameters[2];

			SystemEventController.Instance.Event += OnSystemEvent;
			UIEventController.Instance.Event += OnUIEvent;

			buttonPlay.gameObject.SetActive(false);
			labelContainer.gameObject.SetActive(false);

			labelOff.SetActive(true);
			labelOn.SetActive(false);
			iconOff.SetActive(true);
			iconOn.SetActive(false);

			if (_easterEgg.Played)
			{
				DisplayPlayButtonReady();
			}

			buttonPlay.onClick.AddListener(OnButtonPlay);
			
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
			VRButtonInteraction.gameObject.SetActive(true);
			VRButtonInteraction.onClick.AddListener(OnButtonVRPlay);
#else
			VRButtonInteraction.gameObject.SetActive(false);
#endif
		}

        private void OnButtonVRPlay()
        {
			if (_easterEgg == null)
			{
				_easterEgg = _easterVREgg;
			}			
            OnButtonPlay();
        }

        private void OnButtonPlay()
        {
			if (_easterEgg != null)
			{
				_easterEgg.Target.SetActive(false);

				labelOff.SetActive(false);
				labelOn.SetActive(true);
				iconOff.SetActive(false);
				iconOn.SetActive(true);

				_easterEgg.SetPlayed(true);
				SystemEventController.Instance.DispatchSystemEvent(LevelView.EventLevelViewPlayEasterEgg, _easterEgg.Index, _easterEgg.ActivationEvent, _easterEgg.Reference);
				_easterVREgg = _easterEgg;
				_easterEgg = null;
			}						
        }

        private void OnButtonPause()
		{
			UIEventController.Instance.DispatchUIEvent(GameStateRun.EventGameStateRunTriggerPause);			
		}

		public override void Destroy()
		{
			base.Destroy();
			_easterEgg = null;
			_easterVREgg = null;
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}

		private void DisplayPlayButtonReady()
		{
			_hasBeenFirstClicked = true;
			popUpContainerScreen.SetActive(false);

#if !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
			buttonPlay.gameObject.SetActive(true);
#endif
			labelContainer.SetActive(true);

			labelToPlayScreen.text = LanguageController.Instance.GetText("screen.easter.egg.found.press.play");
		}

		private void OnUIEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(GameStateRun.EventGameStateRunTriggerPause))
            {
				UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(EventScreenFoundEasterEggViewDestroy))
			{
				UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
			}
        }

		void Update()
		{
			if (_easterEgg != null)
			{
#if !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
			if (Input.GetMouseButtonDown(0))
				{
					if (!_hasBeenFirstClicked)
					{
						DisplayPlayButtonReady();						
					}
					else
					{
#if UNITY_EDITOR
						OnButtonPlay();
#endif					
					}
				}
#endif									
			}
		}
    }
}
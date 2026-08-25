using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Narration;
using yourvrexperience.Networking;
using yourvrexperience.Utils;
using static yourvrexperience.Narration.NarrationController;

namespace yourvrexperience.template6dof
{
	public class ScreenReplayPOIView : BaseScreenView, IScreenView
	{		
		public const string EventScreenReplayPOIViewDestroy = "EventScreenReplayPOIViewDestroy";

		public const string ScreenName = "ScreenReplayPOIView";

		[SerializeField] private Button buttonPlay;
		[SerializeField] private TextMeshProUGUI titleScreen;
		[SerializeField] private TextMeshProUGUI descriptionScreen;
	
		private int _poiIndex;
		private NarrationData _narrationData;

		public override string NameScreen
		{
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			descriptionScreen.text = (string)parameters[0];
			_poiIndex = (int)parameters[1];

			SystemEventController.Instance.Event += OnSystemEvent;
			UIEventController.Instance.Event += OnUIEvent;

			buttonPlay.onClick.AddListener(OnButtonPlay);

			SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerRequestTitleReplay, _poiIndex);
		}

        private void OnButtonPlay()
        {			
			NarrationData narrationDataOutput = _narrationData;
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
			if (!MainController.Instance.IsMultiplayer)
			{
				SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerRunNarrationPOI, _poiIndex, narrationDataOutput);
			}
			else
			{
				if (NetworkController.Instance.IsServer)
				{
					NetworkController.Instance.DispatchNetworkEvent(NarrationController.EventNarrationControllerRequestReplayForAll, -1, -1, _poiIndex);
				}
				return;
			}
#if ENABLE_ANALYTICS
			if (!MainController.Instance.EnableEditionPOIs)
			{
				TourAnalyticsController.Instance.LogPOIReplayEvent(GameLevelData.Instance.Age, _poiIndex);
			}			
#endif
        }

		public override void Destroy()
		{
			base.Destroy();
			_narrationData = null;
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}

		private void OnUIEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(GameStateRun.EventGameStateRunTriggerPause))
            {
				UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerReplayPOIUpdateTitleLabel))	
			{
				_narrationData = (NarrationData)parameters[0];
				titleScreen.text = LanguageController.Instance.GetText(_narrationData.TitleNarration);
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(EventScreenReplayPOIViewDestroy))
			{
				UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
			}
        }

		void Update()
		{
#if UNITY_EDITOR
			if (Input.GetMouseButtonDown(0) || (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R)))
			{
				OnButtonPlay();
			}
#endif		
		}

    }
}
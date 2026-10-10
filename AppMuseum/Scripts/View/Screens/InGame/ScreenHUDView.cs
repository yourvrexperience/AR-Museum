using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Narration;
using yourvrexperience.Utils;
using yourvrexperience.VR;

namespace yourvrexperience.template6dof
{
	public class ScreenHUDView : BaseScreenView, IScreenView
	{
		public const string EventScreenHUDViewCreate = "EventScreenHUDViewCreate";
		public const string EventScreenHUDViewPause = "EventScreenHUDViewPause";
		public const string EventScreenHUDViewSetTextEditionMode = "EventScreenHUDViewSetTextEditionMode";

#if ENABLE_OCULUS || ENABLE_OPENXR
		public const string ScreenName = "ScreenHUDView";
#else
		public const string ScreenName = "ScreenHUDView";
#endif

		[SerializeField] private Button buttonPause;		
		[SerializeField] private Button buttonAIInteraction;

		private RefocusScreen _refocusComponent;
		private bool _enablePauseAccess = true;
		private float _delayToEnablePauseAccess = 1.0f;
		private TextMeshProUGUI _editionModeText;

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			buttonPause.onClick.AddListener(OnButtonPause);
			_editionModeText = buttonPause.GetComponentInChildren<TextMeshProUGUI>();
			_editionModeText.text = "";
			buttonAIInteraction.onClick.AddListener(OnButtonAIInteraction);

			UIEventController.Instance.Event += OnUIEvent;
			SystemEventController.Instance.Event += OnSystemEvent;

			if (MainController.Instance.EnableEditionPOIs)
			{
				buttonAIInteraction.gameObject.SetActive(false);
			}
			
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL || ENABLE_NIANTICXR || ENABLE_QR_XR || ENABLE_QR_META
			_refocusComponent = this.gameObject.GetComponent<RefocusScreen>();
			if (_refocusComponent == null)
			{
				_refocusComponent = this.gameObject.AddComponent<RefocusScreen>();
			}
			_refocusComponent.Activate(VRInputController.Instance.Camera, 3, 1, 0.4f);
#endif			

			UpdateTitleEditionMode();
		}

        public override void Destroy()
		{
			base.Destroy();

			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}

        private void OnButtonPause()
        {
			if (_enablePauseAccess)
			{	
				_enablePauseAccess = false;
				UIEventController.Instance.DispatchUIEvent(GameStateRun.EventGameStateRunTriggerPause);
			}
		}

        private void OnButtonAIInteraction()
        {
            UIEventController.Instance.DispatchUIEvent(GameStateRun.EventGameStateRunAIInteraction);            
        }

		private void UpdateTitleEditionMode()
		{
			if (MainController.Instance.EnableEditionPOIs)
			{
				if (GameLevelData.Instance.EditPOIsMode)
				{
					_editionModeText.text = LanguageController.Instance.GetText("screen.hud.edition.pois");
				}
				else
				{
					_editionModeText.text = LanguageController.Instance.GetText("screen.hud.edition.secrets");
				}
			}				
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(GameLevelData.EventGameLevelDataEditModeChanged))
			{
				UpdateTitleEditionMode();
			}
#if UNITY_EDITOR || ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || UNITY_WEBGL || ENABLE_QR_XR || ENABLE_QR_META			
			if (nameEvent.Equals(VRInputController.EventVRInputControllerMenuTriggered))
			{
				OnButtonPause();
			}
#endif				
		}

        private void OnUIEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(ScreenNarrationNextButtonView.EventScreenNarrationNextButtonViewPauseVisibility))
			{
				bool enablePauseAccess = (bool)parameters[0];
				buttonAIInteraction.gameObject.SetActive(enablePauseAccess);				
			}			
			if (nameEvent.Equals(ScreenPauseView.EventScreenPauseViewResumeGame))
			{
				_delayToEnablePauseAccess = 1.5f;
				_enablePauseAccess = true;
			}
        }

		void Update()
		{
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NIANTICXR	|| ENABLE_QR_XR || ENABLE_QR_META
			if (_enablePauseAccess)
			{
				if (_delayToEnablePauseAccess > 0)
				{
					_delayToEnablePauseAccess -= Time.deltaTime;
					if (_delayToEnablePauseAccess <= 0)
					{
						if (VRInputController.Instance != null) VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerResetAllInputs);
					}
				}
				else
				{
					if (MainController.Instance.GameInputController.ActionMenuPressed())
					{
						OnButtonPause();
					}
				}
			}
#endif
		}
	}
}
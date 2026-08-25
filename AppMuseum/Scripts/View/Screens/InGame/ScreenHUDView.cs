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

#if ENABLE_OCULUS || ENABLE_OPENXR
		public const string ScreenName = "ScreenHUDView";
#else
		public const string ScreenName = "ScreenHUDView";
#endif

		[SerializeField] private Button buttonPause;		
		[SerializeField] private Button buttonAIInteraction;

		private RefocusScreen _refocusComponent;

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			UIEventController.Instance.Event += OnUIEvent;
			SystemEventController.Instance.Event += OnSystemEvent;
			
			buttonPause.onClick.AddListener(OnButtonPause);
			buttonAIInteraction.onClick.AddListener(OnButtonAIInteraction);

			if (MainController.Instance.EnableEditionPOIs)
			{
				buttonAIInteraction.gameObject.SetActive(false);
			}
			
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			_refocusComponent = this.gameObject.GetComponent<RefocusScreen>();
			if (_refocusComponent == null)
			{
				_refocusComponent = this.gameObject.AddComponent<RefocusScreen>();
			}
			_refocusComponent.Activate(VRInputController.Instance.Camera, 3, 1, 0.4f);
#endif			
		}

        public override void Destroy()
		{
			base.Destroy();

			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}

        private void OnButtonPause()
        {
			UIEventController.Instance.DispatchUIEvent(GameStateRun.EventGameStateRunTriggerPause);            
        }

        private void OnButtonAIInteraction()
        {
            UIEventController.Instance.DispatchUIEvent(GameStateRun.EventGameStateRunAIInteraction);            
        }

		private void OnSystemEvent(string nameEvent, object[] parameters)
        {
        }

        private void OnUIEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(ScreenNarrationNextButtonView.EventScreenNarrationNextButtonViewPauseVisibility))
			{
				bool enablePauseAccess = (bool)parameters[0];
				buttonAIInteraction.gameObject.SetActive(enablePauseAccess);
			}			
        }
	}
}
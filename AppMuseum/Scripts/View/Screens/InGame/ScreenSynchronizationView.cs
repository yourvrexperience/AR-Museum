using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;

namespace yourvrexperience.template6dof
{
	public class ScreenSynchronizationView : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenSynchronizationView";

		[SerializeField] private GameObject doorFrame;
		[SerializeField] private GameObject iconInfo;
		[SerializeField] private GameObject iconWalk;
		[SerializeField] private Button buttonPause;
		[SerializeField] private TextMeshProUGUI titleScreen;

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			titleScreen.text = LanguageController.Instance.GetText("screen.synchronization.look.for.frame");
			buttonPause.onClick.AddListener(OnButtonPause);
			iconWalk.SetActive(false);

			SystemEventController.Instance.Event += OnSystemEvent;

#if UNITY_EDITOR
			SystemEventController.Instance.DelaySystemEvent(ARMaxSTController.EventARMaxSTControllerAreaRecognized, 2);
#else
			if (MainController.Instance.IsARMode)
			{
				bool hasBeenDetected = false;
#if ENABLE_VUFORIA
				hasBeenDetected = VuforiaController.Instance.HasAreaBeenDetected;
#elif ENABLE_NIANTIC
				hasBeenDetected = NianticController.Instance.HasAreaBeenDetected;
#else
				hasBeenDetected = ARMaxSTController.Instance.HasAreaBeenDetected;
#endif				
				if (hasBeenDetected)
				{
					HideElements();
				}
			}
			else
			{
				SystemEventController.Instance.DelaySystemEvent(ARMaxSTController.EventARMaxSTControllerAreaRecognized, 1);
			}
#endif
		}

		private void OnButtonPause()
        {
			UIEventController.Instance.DispatchUIEvent(GameStateRun.EventGameStateRunTriggerPause);            
        }

        public override void Destroy()
		{
			base.Destroy();

			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		private void HideElements()
		{
			doorFrame.SetActive(false);
			iconInfo.SetActive(false);
			iconWalk.SetActive(true);
			titleScreen.text = LanguageController.Instance.GetText("screen.synchronization.move.to.door");
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(ARMaxSTController.EventARMaxSTControllerAreaRecognized))
			{
				HideElements();
			}
		}
	}
}
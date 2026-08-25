using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Networking;
using yourvrexperience.Utils;
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
using yourvrexperience.VR;
#endif

namespace yourvrexperience.template6dof
{
	public class ScreenFloorView : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenFloorView";

		public const string EventScreenFloorSelected = "EventScreenFloorSelected";
		public const string EventScreenFloorBack = "EventScreenFloorBack";

		[SerializeField] private TextMeshProUGUI titleScreen;
		[SerializeField] private Button buttonFloor1;
		[SerializeField] private Button buttonFloor2;
		[SerializeField] private Button buttonFloor3;
		[SerializeField] private Button buttonBack;

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			buttonBack.onClick.AddListener(OnButtonBack);
	
			buttonFloor1.onClick.AddListener(OnButtonFloor1);
			buttonFloor2.onClick.AddListener(OnButtonFloor2);
			buttonFloor3.onClick.AddListener(OnButtonFloor3);

			titleScreen.text = LanguageController.Instance.GetText("screen.floor.title.selection");

			buttonFloor1.transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.pause.stairs.1");
			buttonFloor2.transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.pause.stairs.2");
			buttonFloor3.transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.pause.stairs.3");
		}

        public override void Destroy()
		{
			base.Destroy();
		}

        private void OnButtonBack()
        {
			SoundsController.Instance.PlaySoundFX(GameSounds.FxSelection, false, 1);
			UIEventController.Instance.DispatchUIEvent(EventScreenFloorBack);	
        }

        private void OnButtonFloor1()
        {
			UIEventController.Instance.DispatchUIEvent(EventScreenFloorSelected, 0);	
        }

        private void OnButtonFloor2()
        {
			UIEventController.Instance.DispatchUIEvent(EventScreenFloorSelected, 1);	
        }

        private void OnButtonFloor3()
        {
			UIEventController.Instance.DispatchUIEvent(EventScreenFloorSelected, 2);	
        }

		void Update()
		{
			if (Input.GetKeyDown(KeyCode.P))
			{
				OnButtonFloor3();
			}
		}

	}
}
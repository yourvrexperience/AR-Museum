using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Narration;
using yourvrexperience.Networking;
using yourvrexperience.Utils;
using yourvrexperience.VR;
using static yourvrexperience.Narration.NarrationController;
using static yourvrexperience.Narration.NarrationCreator;
using static yourvrexperience.template6dof.LevelView;

namespace yourvrexperience.template6dof
{
	public class ScreenSelectedEditionPOIView : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenSelectedEditionPOIView";

		public const string EventScreenSelectedEditionPOIViewSelected = "EventScreenSelectedEditionPOIViewSelected";
		public const string EventScreenSelectedEditionPOIViewCancelled = "EventScreenSelectedEditionPOIViewCancelled";

		[SerializeField] private Button buttonResume;

		[SerializeField] private Button buttonMovePOI;
		[SerializeField] private Button buttonManageNarration;

		private bool _isPOI = false;
		private EasterEgg _narrationSecret;

		public override string NameScreen
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			_isPOI = (bool)parameters[0];
			if (!_isPOI)
			{
				_narrationSecret = (EasterEgg)parameters[1];
			}

			SystemEventController.Instance.Event += OnSystemEvent;
			UIEventController.Instance.Event += OnUIEvent;

			buttonResume.onClick.AddListener(OnButtonResume);
			
			buttonMovePOI.onClick.AddListener(OnButtonMovePOI);
			buttonManageNarration.onClick.AddListener(OnNarrationPOI);

			buttonMovePOI.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.poi.select.edition.move.poi");
			buttonManageNarration.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.poi.select.edition.narration");

			MainController.Instance.CreateNarrationObjects(_isPOI, _narrationSecret);
		}

        public override void Destroy()
		{
			base.Destroy();

			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;

			_narrationSecret = null;

			SystemEventController.Instance.DispatchSystemEvent(GameLevelData.EventGameLevelDataDestroyNarrationObjects);
			SystemEventController.Instance.DispatchSystemEvent(GameLevelData.EventGameLevelDataRefreshLocalData);
			SoundsController.Instance.StopAllSounds();
		}

        private void OnButtonResume()
        {
			SystemEventController.Instance.DispatchSystemEvent(EventScreenSelectedEditionPOIViewCancelled);
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
        }

        private void OnNarrationPOI()
        {
			SystemEventController.Instance.DispatchSystemEvent(GameLevelData.EventGameLevelDataDestroyNarrationObjects);
			if (_isPOI)
			{
				ScreenController.Instance.CreateScreen(ScreenXMLNarrationNodesView.ScreenName, false, true, true);
			}
			else
			{
				ScreenController.Instance.CreateScreen(ScreenXMLNarrationNodesView.ScreenName, false, true, false, _narrationSecret );
			}  
        }

        private void OnButtonMovePOI()
        {
			SystemEventController.Instance.DispatchSystemEvent(GameLevelData.EventGameLevelDataDestroyNarrationObjects);
            SystemEventController.Instance.DelaySystemEvent(EventScreenSelectedEditionPOIViewSelected, 0.3f);
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
        }

		public override void ActivateContent(bool value)
		{
			if (!Content.gameObject.activeSelf && value)
			{
				MainController.Instance.CreateNarrationObjects(_isPOI, _narrationSecret);
			}
			base.ActivateContent(value);
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
		}

        private void OnUIEvent(string nameEvent, object[] parameters)
        {
        }

		void Update()
		{
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NIANTICXR			
			if (MainController.Instance.GameInputController.ActionMenuPressed())
			{
				OnButtonResume();
			}
#endif			
		}
	}
}
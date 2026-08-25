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

			CreateNarrationObjects();
		}

		private void CreateNarrationObjects()
		{
			SystemEventController.Instance.DispatchSystemEvent(GameLevelData.EventGameLevelDataDestroyNarrationObjects);

			int currentLevel = GameLevelData.Instance.GetLevel(GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel);
			NarrationCreator narrationCreator = new NarrationCreator();
			NarrationCreatorData narrationForCurrentPOI;
			if (_isPOI)
			{
				TextAsset narrationData = GameLevelData.Instance.GetLevelNarration(currentLevel);
				narrationCreator.LoadNarrationTexts(narrationData);
				narrationForCurrentPOI = narrationCreator.Narration[GameLevelData.Instance.IndexPOILevelEdited];
			}
			else
			{
				string contentNarrationSecret = _narrationSecret.Narration;
				if ((_narrationSecret.Narration == null) || (_narrationSecret.Narration.Length == 0))
				{
					contentNarrationSecret = GameLevelData.Instance.GetInitialNarration();
					_narrationSecret.Narration = contentNarrationSecret;
				}
				narrationCreator.LoadNarrationTexts(new TextAsset(contentNarrationSecret));
				narrationForCurrentPOI = narrationCreator.Narration[0];
			}
			
			foreach (NarrationCreatorToken token in narrationForCurrentPOI.Segments)
			{
				foreach (NarrationObject narrationObj in token.Assets)
				{
					switch (narrationObj.Type)
					{
						case TypeObjectNarration.Image:
							string[] photos = narrationObj.AssetName.Split(',');
							POIPhotoGalleryController photoGallery = MainController.Instance.CreatePhotoGalleryController(false, photos, NavMeshController.Instance.AreaMaxST.transform,  narrationObj.Position, narrationObj.Rotation, narrationObj.Scale);
							yourvrexperience.Utils.Utilities.ApplyLayer(photoGallery.gameObject.transform, LayerMask.NameToLayer("Ignore Raycast"));
							yourvrexperience.Utils.Utilities.DisableGraphicRaycaster(photoGallery.gameObject.transform);
							break;

						case TypeObjectNarration.Video:
							POIVideoController videoControl = MainController.Instance.CreateVideoController(false, narrationObj.AssetName, NavMeshController.Instance.AreaMaxST.transform, narrationObj.Position, narrationObj.Rotation, narrationObj.Scale, true, false);
							yourvrexperience.Utils.Utilities.ApplyLayer(videoControl.gameObject.transform, LayerMask.NameToLayer("Ignore Raycast"));
							yourvrexperience.Utils.Utilities.DisableGraphicRaycaster(videoControl.gameObject.transform);
							break;

						case TypeObjectNarration.Model3D:
							POIModel3DController model3D = MainController.Instance.CreateModel3DController(false, narrationObj.AssetName, NavMeshController.Instance.AreaMaxST.transform, narrationObj.Position, narrationObj.Rotation, narrationObj.Scale, narrationObj.Animation);
							yourvrexperience.Utils.Utilities.ApplyLayer(model3D.gameObject.transform, LayerMask.NameToLayer("Ignore Raycast"));
							break;

						case TypeObjectNarration.Interaction:
							GameObject interactable = MainController.Instance.CreateInteractable(narrationObj.AssetName, NavMeshController.Instance.AreaMaxST.transform, narrationObj.Position, narrationObj.Rotation, narrationObj.Scale);
							interactable.GetComponent<IGameInteractables>().SetEditionMode();
							yourvrexperience.Utils.Utilities.ApplyLayer(interactable.gameObject.transform, LayerMask.NameToLayer("Ignore Raycast"));
							break;

						case TypeObjectNarration.Waypoints:
							GameObject waypoint = MainController.Instance.CreateWaypoint(narrationObj.AssetName, NavMeshController.Instance.AreaMaxST.transform, narrationObj.Position, narrationObj.Rotation, narrationObj.Scale);
							waypoint.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
							yourvrexperience.Utils.Utilities.ApplyLayer(waypoint.gameObject.transform, LayerMask.NameToLayer("Ignore Raycast"));
							break;

						case TypeObjectNarration.Sound:						
							AudioClip audioSegment = AssetBundleController.Instance.CreateAudioclip(narrationObj.AssetName);
							SoundsController.Instance.PlaySoundClipFx(SoundsController.ChannelsAudio.FX3, audioSegment, false, 0.5f);
							break;
					}
				}
			}
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
				CreateNarrationObjects();
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
		}
	}
}
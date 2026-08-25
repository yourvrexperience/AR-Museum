using System;
using UnityEngine;
using UnityEngine.Assertions;
using yourvrexperience.Narration;
using yourvrexperience.Networking;
using yourvrexperience.UserManagement;
using yourvrexperience.Utils;
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
using yourvrexperience.VR;
#endif
using static yourvrexperience.Narration.GameLevelData;
using static yourvrexperience.template6dof.LevelView;

namespace yourvrexperience.template6dof
{
	public class GameSubStateEditPOI : IGameState
    {
		public const string EventGameStateRunSaveEditionPOIs = "EventGameStateRunSaveEditionPOIs";
		public const string EventGameStateRunPublishEditionPOIs = "EventGameStateRunPublishEditionPOIs";

		public const string SubEventGameStateRunConfirmDiscardAndPublish = "SubEventGameStateRunConfirmDiscardAndPublish";	
		public const string SubEventGameStateRunFinalConfirmationPublication = "SubEventGameStateRunFinalConfirmationPublication";	

		public enum StatesEditionPOIs { Idle = 0, Selected, RePosition, WaitToIdle }

		private StatesEditionPOIs _stateEditPOIs = StatesEditionPOIs.Idle;
		private Transform _currentEditedPOI;
		private Vector3 _originalBackupEditedPOI;
		private GameObject _highlightedPOI;
		private float _timeAcum = 0;

		public void Initialize()
		{
			UIEventController.Instance.Event += OnUIEvent;
			SystemEventController.Instance.Event += OnSystemEvent;
		}

        public void Destroy()
		{
			_highlightedPOI = null;
			_currentEditedPOI = null;
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}

		public void Reset()
		{
			if (_currentEditedPOI != null)
			{
				_currentEditedPOI.transform.position = _originalBackupEditedPOI;
				_currentEditedPOI = null;
			}
			ChangeSubState(StatesEditionPOIs.Idle);
		}

        private void OnUIEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(SubEventGameStateRunConfirmDiscardAndPublish))
			{
				ScreenInformationResponses userResponse = (ScreenInformationResponses)parameters[1];
				if (userResponse == ScreenInformationResponses.Confirm)
				{
					GameLevelData.Instance.HasBeenEditionModified = false;
					SystemEventController.Instance.DispatchSystemEvent(EventGameStateRunPublishEditionPOIs);
				}
			}
			if (nameEvent.Equals(SubEventGameStateRunFinalConfirmationPublication))
			{
				ScreenInformationResponses userResponse = (ScreenInformationResponses)parameters[1];
				if (userResponse == ScreenInformationResponses.Confirm)
				{
					string titleInfo = LanguageController.Instance.GetText("screen.game.run.publishing.edition.pois.title");
					string descriptionInfo = LanguageController.Instance.GetText("screen.game.run.publishing.edition.pois.description");
					ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, titleInfo, descriptionInfo);
					if (UsersController.Instance.CurrentUser != null)
					{
						if (!UsersController.Instance.CurrentUser.IsEmptyUser())
						{
							if (UsersController.Instance.CurrentUser.Admin)
							{					
								GameLevelData.Instance.SetVersion((int)UsersController.Instance.CurrentUser.Id, UsersController.Instance.CurrentUser.PasswordPlain, GameLevelData.Instance.VersionNumber + 1, GameLevelData.Instance.UnlockSecretsIndex);					
							}
						}
					}
				}
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(SetVersionHTTP.EventSetVersionHTTPCompleted))
			{
				UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
				if ((bool)parameters[0])
				{
					string titleInfo = LanguageController.Instance.GetText("screen.game.run.publish.edition.pois.success.title");
					string descriptionInfo = LanguageController.Instance.GetText("screen.game.run.saving.publish.pois.success.description");
					ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, titleInfo, descriptionInfo);
				}
				else
				{
					
				}
			}			
			if (nameEvent.Equals(ScreenSelectedEditionPOIView.EventScreenSelectedEditionPOIViewSelected))
			{
				if (_stateEditPOIs == StatesEditionPOIs.Selected)
				{
					ChangeSubState(StatesEditionPOIs.RePosition);
				}
			}
			if (nameEvent.Equals(EventGameStateRunPublishEditionPOIs))
			{
				if (GameLevelData.Instance.HasBeenEditionModified)
				{
					string warning = LanguageController.Instance.GetText("message.warning");
					string description = LanguageController.Instance.GetText("message.do.you.discard.edition.and.publish");
					ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenConfirmation, null, warning, description, SubEventGameStateRunConfirmDiscardAndPublish);
				}
				else
				{
					string warningPublication = LanguageController.Instance.GetText("message.warning");
					string descriptionPublication = LanguageController.Instance.GetText("message.do.you.confirm.publish.new.version");
					ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenConfirmation, null, warningPublication, descriptionPublication, SubEventGameStateRunFinalConfirmationPublication);
				}
			}
			if (nameEvent.Equals(ScreenSelectedEditionPOIView.EventScreenSelectedEditionPOIViewCancelled))
			{
				ChangeSubState(StatesEditionPOIs.WaitToIdle);
			}
			if (nameEvent.Equals(EventGameStateRunSaveEditionPOIs))
			{
				string titleInfo = LanguageController.Instance.GetText("screen.game.run.saving.edition.pois.title");
				string descriptionInfo = LanguageController.Instance.GetText("screen.game.run.saving.edition.pois.description");
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, titleInfo, descriptionInfo);

				MainController.Instance.SaveEditionPOIs();
			}
			if (nameEvent.Equals(GameLevelData.EventGameLevelDataSaveAllData))
			{
				MainController.Instance.SaveEditionPOIs();
			}
			if (nameEvent.Equals(GameLevelData.EventGameLevelDataCompletedUpdate))
			{
				UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);

				string titleInfo = LanguageController.Instance.GetText("screen.game.run.saving.edition.pois.success.title");
				string descriptionInfo = LanguageController.Instance.GetText("screen.game.run.saving.edition.pois.success.description");
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, titleInfo, descriptionInfo);
				GameLevelData.Instance.HasBeenEditionModified = false;
			}
        }

		private void ChangeSubState(StatesEditionPOIs newSubState)
		{
			_timeAcum = 0;
			_stateEditPOIs = newSubState;
			switch (_stateEditPOIs)
			{
				case StatesEditionPOIs.Idle:
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, true);
					_currentEditedPOI = null;
					MainController.Instance.SelectedPOI.SetActive(false);
					MainController.Instance.HighlightedPOI.SetActive(false);
					break;

				case StatesEditionPOIs.Selected:
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, false);
					MainController.Instance.SelectedPOI.transform.position = _highlightedPOI.transform.position;
					MainController.Instance.SelectedPOI.SetActive(true);
					MainController.Instance.HighlightedPOI.SetActive(false);
					if (GameLevelData.Instance.EditPOIsMode)
					{
						_currentEditedPOI = _highlightedPOI.transform.parent;
						_originalBackupEditedPOI = _currentEditedPOI.transform.position;
						GameLevelData.Instance.IndexPOILevelEdited = MainController.Instance.LevelView.GetIndexSelectedPOI(_currentEditedPOI);
						if (GameLevelData.Instance.IndexPOILevelEdited != -1)
						{
							ScreenController.Instance.CreateScreen(ScreenSelectedEditionPOIView.ScreenName, false, true, true);
						}						
					}
					else
					{
						_currentEditedPOI = _highlightedPOI.transform;
						_originalBackupEditedPOI = _currentEditedPOI.transform.position;
						EasterEgg secretSelected = MainController.Instance.LevelView.GetSelectedSecret(_currentEditedPOI);
						if (secretSelected != null)
						{
							ScreenController.Instance.CreateScreen(ScreenSelectedEditionPOIView.ScreenName, false, true, false, secretSelected);
						}						
					}
					break;

				case StatesEditionPOIs.RePosition:
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, true);
					break;

				case StatesEditionPOIs.WaitToIdle:
					SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, false);
					break;
			}
		}

		public void Run()
		{
			RaycastHit ray = new RaycastHit();
			Vector3	positionCurrentController = Vector3.zero;
			Vector3	forwardCurrentController = Vector3.zero;
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
			if (VRInputController.Instance.VRController.CurrentController != null)
			{
				positionCurrentController = VRInputController.Instance.VRController.CurrentController.transform.position;
				forwardCurrentController = VRInputController.Instance.VRController.CurrentController.transform.forward;
			}
#else
			positionCurrentController = Camera.main.transform.position;
			forwardCurrentController = Camera.main.transform.forward;
#endif

			switch (_stateEditPOIs)
			{
				case StatesEditionPOIs.Idle:
					MainController.Instance.PlayerView.Run();
					if (GameLevelData.Instance.EditPOIsMode)
					{
						_highlightedPOI = RaycastingTools.GetRaycastObject(positionCurrentController, forwardCurrentController, 100, ref ray, GameLevelData.Instance.LayerReplay);
					}
					else
					{
						_highlightedPOI = RaycastingTools.GetRaycastObject(positionCurrentController, forwardCurrentController, 100, ref ray, GameLevelData.Instance.LayerEasterEgg);
					}
					if (_highlightedPOI != null)
					{
						MainController.Instance.HighlightedPOI.transform.position = _highlightedPOI.transform.position;
						MainController.Instance.HighlightedPOI.SetActive(true);

						if (MainController.Instance.GameInputController.ActionPrimaryUp())
						{
							ChangeSubState(StatesEditionPOIs.Selected);
						}
					}
					else
					{
						MainController.Instance.HighlightedPOI.SetActive(false);
					}
					break;

				case StatesEditionPOIs.Selected:
					break;

				case StatesEditionPOIs.RePosition:
					MainController.Instance.PlayerView.Run();
					if (_currentEditedPOI != null)
					{
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
						positionCurrentController += VRInputController.Instance.VRController.CurrentController.transform.forward;
#else
						positionCurrentController += Camera.main.transform.forward;
#endif
						if (GameLevelData.Instance.EditPOIsMode)
						{
							if (MainController.Instance.IsNormalAxis)
							{
								positionCurrentController.y = MainController.Instance.LevelView.Floor.transform.position.y + NavMeshController.SHIFT_FROM_FLOOR;
							}
							else
							{
								positionCurrentController.z = MainController.Instance.LevelView.Floor.transform.position.z + NavMeshController.SHIFT_FROM_FLOOR;
							}						
						}

						_currentEditedPOI.transform.position = positionCurrentController;
						MainController.Instance.SelectedPOI.transform.position = positionCurrentController;
						MainController.Instance.SelectedPOI.SetActive(true);
						if (MainController.Instance.GameInputController.ActionPrimaryUp())
						{
							if (GameLevelData.Instance.EditPOIsMode)
							{
								int totalPOIs = GameLevelData.Instance.GetLevelPOIsNumber(MainController.Instance.CurrentGameLevel);
								if (!MainController.Instance.LevelView.IsInsideNavigationArea(_currentEditedPOI) && (totalPOIs > 1))
								{
									_currentEditedPOI.transform.position = _originalBackupEditedPOI;
									string titleInfo = LanguageController.Instance.GetText("screen.game.run.edit.poi.invalid.position.title");
									string descriptionInfo = LanguageController.Instance.GetText("screen.game.run.edit.poi.invalid.position.description");
									ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, titleInfo, descriptionInfo);
								}
								GameLevelData.Instance.UpdatePOIsPosition(MainController.Instance.CurrentGameLevel, MainController.Instance.LevelView.PackPOIsContent());
							}
							else
							{
								GameLevelData.Instance.UpdateSecretsPosition(MainController.Instance.CurrentGameLevel, MainController.Instance.LevelView.PackSecretsContent());
							}
							ChangeSubState(StatesEditionPOIs.Idle);
							GameLevelData.Instance.HasBeenEditionModified = true;
						}
					}
					break;

				case StatesEditionPOIs.WaitToIdle:
					_timeAcum += Time.deltaTime;
					if (_timeAcum > 0.5f)
					{
						ChangeSubState(StatesEditionPOIs.Idle);
					}
					break;
			}
		}
	}
}
using yourvrexperience.Utils;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using yourvrexperience.Narration;
using yourvrexperience.Networking;
using yourvrexperience.UserManagement;

namespace yourvrexperience.template6dof
{
	public class PanelPOIManagementView : MonoBehaviour
	{
		public const string SubEventClearAllPOIsConfirmation = "SubEventClearAllPOIsConfirmation";

		[SerializeField] private Button buttonAddPOI;
		[SerializeField] private TextMeshProUGUI titleAdd;
		[SerializeField] private Button buttonRemovePOI;
		[SerializeField] private TextMeshProUGUI titleRemove;
		[SerializeField] private Button buttonClearAll;
		[SerializeField] private TextMeshProUGUI titleClearAll;

		void Start()
		{
			this.gameObject.SetActive(true);
			
			buttonAddPOI.onClick.AddListener(OnAddPOI);
			buttonRemovePOI.onClick.AddListener(OnRemovePOI);
			buttonClearAll.onClick.AddListener(OnClearPOIs);
			buttonRemovePOI.gameObject.SetActive(false);
			titleRemove.gameObject.SetActive(false);
			
			buttonClearAll.gameObject.SetActive(true);
			titleClearAll.gameObject.SetActive(true);

			UIEventController.Instance.Event += OnUIEvent;
			SystemEventController.Instance.Event += OnSystemEvent;

			RefreshEditionMode();
		}

        void OnDestroy()
		{
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		private void RefreshEditionMode()
		{
			if (GameLevelData.Instance.EditPOIsMode)
			{
				titleAdd.text = LanguageController.Instance.GetText("panel.poi.management.add.poi");
				titleRemove.text = LanguageController.Instance.GetText("panel.poi.management.remove.poi");
				titleClearAll.text = LanguageController.Instance.GetText("panel.poi.management.clear.pois");
			}
			else
			{
				titleAdd.text = LanguageController.Instance.GetText("panel.poi.management.add.secret");
				titleRemove.text = LanguageController.Instance.GetText("panel.poi.management.remove.secret");
				titleClearAll.text = LanguageController.Instance.GetText("panel.poi.management.clear.secrets");
			}
		}

		private void OnAddPOI()
		{
			Vector3 positionInFront = MainController.Instance.PlayerView.transform.position + Camera.main.transform.forward * 1;
			positionInFront.y = MainController.Instance.LevelView.Floor.transform.position.y + NavMeshController.SHIFT_FROM_FLOOR;
#if !(UNITY_EDITOR || ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || UNITY_WEBGL)
			Vector3 playerForward = Camera.main.transform.forward.normalized;
			if (!MainController.Instance.IsNormalAxis)
			{											
				positionInFront = MainController.Instance.PlayerView.transform.position + playerForward * 1;
				positionInFront.z = MainController.Instance.LevelView.Floor.transform.position.z + NavMeshController.SHIFT_FROM_FLOOR;
			}
			else
			{
#if ENABLE_NIANTIC
				positionInFront = MainController.Instance.GetARWorldCamera().transform.position + playerForward * 1;
				positionInFront = NavMeshController.Instance.ConvertARWorldToNavigation(positionInFront, false);
#else				
				positionInFront = MainController.Instance.PlayerView.transform.localPosition + playerForward * 1;
				if (GameLevelData.Instance.EditPOIsMode)
				{				
					positionInFront.y = MainController.Instance.LevelView.Floor.transform.position.y - NavMeshController.SHIFT_FROM_FLOOR;
				}
#endif				
			}
#endif		
			SystemEventController.Instance.DispatchSystemEvent(GameLevelData.EventGameLevelDataAddNewPOI, positionInFront);
		}

		private void OnRemovePOI()
		{
			SystemEventController.Instance.DispatchSystemEvent(GameLevelData.EventGameLevelDataRemovePOI, (int)UsersController.Instance.CurrentUser.Id, UsersController.Instance.CurrentUser.PasswordPlain);
			buttonRemovePOI.gameObject.SetActive(false);
			titleRemove.gameObject.SetActive(false);
		}

        private void OnClearPOIs()
        {
			string titleWarning = LanguageController.Instance.GetText("text.warning");
			string textAskToClear = LanguageController.Instance.GetText("panel.poi.management.clear.confirmation.all");
			ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenConfirmation, null, titleWarning, textAskToClear, SubEventClearAllPOIsConfirmation);
		}

        private void OnUIEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(ItemPOIMuseum.EventItemPOIMuseumSelected))
			{
				int indexSelected = (int)parameters[2];
				buttonRemovePOI.gameObject.SetActive(indexSelected != -1);
				titleRemove.gameObject.SetActive(indexSelected != -1);
			}
			if (nameEvent.Equals(SubEventClearAllPOIsConfirmation))
			{
				ScreenInformationResponses userResponse = (ScreenInformationResponses)parameters[1];
				if (userResponse == ScreenInformationResponses.Confirm)
				{
					SystemEventController.Instance.DispatchSystemEvent(GameLevelData.EventGameLevelDataClearAll, (int)UsersController.Instance.CurrentUser.Id, UsersController.Instance.CurrentUser.PasswordPlain);
					buttonClearAll.gameObject.SetActive(false);
					titleClearAll.gameObject.SetActive(false);
				}
			}
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(GameLevelData.EventGameLevelDataEditModeChanged))
			{
				RefreshEditionMode();
			}
        }
	}
}

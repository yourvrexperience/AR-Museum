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
	public class ScreenNetworkView : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenNetworkView";

		public const string EventScreenNetworkConnect = "EventScreenNetworkConnect";
		public const string EventScreenNetworkBack = "EventScreenNetworkBack";

		public const string NameRoomStored = "RoomName";

		[SerializeField] private TextMeshProUGUI titleScreen;
		[SerializeField] private TextMeshProUGUI descriptionScreen;
		[SerializeField] private Button buttonNetworkConnect;
		[SerializeField] private Button buttonBack;
		[SerializeField] private CustomInput inputField;

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			buttonBack.onClick.AddListener(OnButtonBack);
			buttonNetworkConnect.onClick.AddListener(OnButtonNetworkConnect);

			titleScreen.text = LanguageController.Instance.GetText("screen.main.menu.title");
			descriptionScreen.text = LanguageController.Instance.GetText("screen.network.description");

			SystemEventController.Instance.Event += OnSystemEvent;
			UIEventController.Instance.Event += OnUIEvent;

			inputField.text = PlayerPrefs.GetString(NameRoomStored, "Room1");
			if (inputField != null)
			{
				inputField.OnFocusEvent += OnFocusInputValue;
			}
		}

        public override void Destroy()
		{
			base.Destroy();

			if (inputField != null)
			{
				inputField.OnFocusEvent -= OnFocusInputValue;
			}
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}

        private void OnFocusInputValue()
        {
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			_content.gameObject.SetActive(false);
			ScreenController.Instance.CreateScreen(ScreenVRKeyboardView.ScreenName, false, true,  inputField.gameObject, inputField, 10);
#endif	            
        }

        private void OnButtonBack()
        {
			SoundsController.Instance.PlaySoundFX(GameSounds.FxSelection, false, 1);		
			UIEventController.Instance.DispatchUIEvent(EventScreenNetworkBack);			
        }

        private void OnButtonNetworkConnect()
		{
			string nameRoom = inputField.text;
			if (nameRoom.Length < 4)
			{
				string titleInfo = LanguageController.Instance.GetText("screen.network.room.name.incorrect.title");
				string descriptionInfo = LanguageController.Instance.GetText("screen.network.room.name.incorrect.description");
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, this.gameObject, titleInfo, descriptionInfo);
			}
			else
			{
				PlayerPrefs.SetString(NameRoomStored, nameRoom);
				SoundsController.Instance.PlaySoundFX(GameSounds.FxSelection, false, 1);
				UIEventController.Instance.DispatchUIEvent(EventScreenNetworkConnect, nameRoom.ToLower());
				descriptionScreen.text = LanguageController.Instance.GetText("screen.network.connecting.now");
				buttonBack.gameObject.SetActive(false);
				buttonNetworkConnect.gameObject.SetActive(false);
				inputField.gameObject.SetActive(false);
			}
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
        }

        private void OnUIEvent(string nameEvent, object[] parameters)
        {
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR	|| ENABLE_NREAL	
            if (nameEvent.Equals(ScreenVRKeyboardView.EventScreenVRKeyboardSetNewText))
			{
				if (inputField.gameObject == (GameObject) parameters[0])
				{
					_content.gameObject.SetActive(true);
					inputField.text = (string)parameters[1];
				}
			}
#endif			
        }
	}
}
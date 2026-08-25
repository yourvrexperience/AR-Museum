using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
using yourvrexperience.VR;
#endif

namespace yourvrexperience.Utils
{
	public class ScreenInputDescription : BaseScreenView, IScreenView
	{
		public const string ScreenNameSession = "ScreenInputDescription";

		public const string ScreenInputDescriptionEnteredValue = "ScreenInputDescriptionEnteredValue";

		[SerializeField] private TextMeshProUGUI Title;
		[SerializeField] private CustomInput inputNameObject;
		[SerializeField] private TextMeshProUGUI Description;
		[SerializeField] protected CustomInput inputDescriptionObject;
		[SerializeField] private Button buttonConfirm;
		[SerializeField] private Button buttonCancel;

		private GameObject _source;

		public override string NameScreen 
		{ 			
			get { return ScreenNameSession; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			_source = (GameObject)parameters[0];			

			if (Title != null) Title.text = (string)parameters[1];
			if (inputNameObject != null) inputNameObject.text = (string)parameters[2];

			if (Description != null) Description.text = (string)parameters[3];
			if (inputDescriptionObject != null) inputDescriptionObject.text = (string)parameters[4];

			if (inputNameObject != null) inputNameObject.OnFocusEvent += OnFocusEvent;

			buttonConfirm.onClick.AddListener(OnButtonConfirmChange);
			buttonConfirm.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("text.confirm");

			buttonCancel.onClick.AddListener(OnButtonCancel);
			buttonCancel.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("text.cancel");

			UIEventController.Instance.Event += OnUIEvent;
		}

		private void OnFocusEvent()
		{
			if (inputNameObject != null) 
			{
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
				ScreenController.Instance.CreateScreen(ScreenVRKeyboardView.ScreenName, false, true,  inputNameObject.gameObject, inputNameObject, 100);
#endif			
			}
		}

		private void OnButtonConfirmChange()
		{
			string finalName = ((inputNameObject != null)?inputNameObject.text:"");
			string finalDescription = ((inputDescriptionObject != null)?inputDescriptionObject.text:"");
			UIEventController.Instance.DispatchUIEvent(ScreenInputDescriptionEnteredValue, _source, true, finalName, finalDescription);
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
		}

		private void OnButtonCancel()
		{
			UIEventController.Instance.DispatchUIEvent(ScreenInputDescriptionEnteredValue, _source, false);
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
		}

		public override void Destroy()
		{
			base.Destroy();

			_source = null;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}

		public void SetTitle(string title)
		{
			if (inputNameObject != null)
			{
				inputNameObject.text = title;
			}			
		}

		public void SetDescription(string description)
		{
			if (inputDescriptionObject != null)
			{
				inputDescriptionObject.text = description;
			}
		}

		private void OnUIEvent(string nameEvent, object[] parameters)
		{
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR	|| ENABLE_NREAL		
			if (nameEvent.Equals(ScreenVRKeyboardView.EventScreenVRKeyboardSetNewText))
			{
				if (inputNameObject != null)
				{
					if (inputNameObject.gameObject == (GameObject)parameters[0])
					{
						inputNameObject.text = (string)parameters[1];
					}
				}
			}
#endif			
		}
	}
}
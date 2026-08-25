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
	public class ScreenInputText : BaseScreenView, IScreenView
	{
		public const string ScreenNameSession = "ScreenInputText";

		public const string ScreenScreenInputTextEnteredValue = "ScreenScreenInputTextEnteredValue";

		[SerializeField] private TextMeshProUGUI Title;
		[SerializeField] private CustomInput inputNameObject;
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

			Title.text = (string)parameters[1];
			if (parameters.Length > 2)
			{
				inputNameObject.text = (string)parameters[2];
			}
			else
			{
				inputNameObject.text = "";
			}

			inputNameObject.OnFocusEvent += OnFocusEvent;

			buttonConfirm.onClick.AddListener(OnButtonConfirmChange);
			buttonConfirm.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("text.confirm");

			buttonCancel.onClick.AddListener(OnButtonCancel);
			buttonCancel.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("text.cancel");

			UIEventController.Instance.Event += OnUIEvent;
		}

		private void OnFocusEvent()
		{
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			ScreenController.Instance.CreateScreen(ScreenVRKeyboardView.ScreenName, false, true,  inputNameObject.gameObject, inputNameObject, 100);
#endif			
		}

		private void OnButtonConfirmChange()
		{
			UIEventController.Instance.DispatchUIEvent(ScreenScreenInputTextEnteredValue, _source, inputNameObject.text);
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
		}

		private void OnButtonCancel()
		{
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
		}

		public override void Destroy()
		{
			base.Destroy();

			_source = null;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}

		private void OnUIEvent(string nameEvent, object[] parameters)
		{
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR	|| ENABLE_NREAL
			if (nameEvent.Equals(ScreenVRKeyboardView.EventScreenVRKeyboardSetNewText))
			{
				if (inputNameObject.gameObject == (GameObject)parameters[0])
				{
					inputNameObject.text = (string)parameters[1];
				}				
			}
#endif			
		}
	}
}
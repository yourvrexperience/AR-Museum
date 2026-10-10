using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.speech;
using yourvrexperience.Utils;
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
using yourvrexperience.VR;
#endif

namespace yourvrexperience.template6dof
{
	public class ScreenInputURLView : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenInputURLView";

		[SerializeField] private TextMeshProUGUI title;
		[SerializeField] private TMP_InputField inputField;
		[SerializeField] private Button buttonNext;
		[SerializeField] private Button buttonClose;

		private string _event = "";

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);
			_event = (string)parameters[1];

			title.text = LanguageController.Instance.GetText("screen.input.url.title");

			inputField.text = "";

			buttonNext.onClick.AddListener(OnButtonNext);
			buttonClose.onClick.AddListener(OnButtonNext);
			buttonNext.transform.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("text.confirm");			

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			RefocusScreen refocusComponent = this.gameObject.GetComponent<RefocusScreen>();
			if (refocusComponent == null)
			{
				refocusComponent = this.gameObject.AddComponent<RefocusScreen>();
			}
			refocusComponent.Activate(VRInputController.Instance.Camera, ScreenController.Instance.DistanceScreen, 1, 0.4f);
#endif			
		}

        public override void Destroy()
		{
			base.Destroy();
		}

		private void OnButtonNext()
		{
			string data = inputField.text;
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
			UIEventController.Instance.DispatchUIEvent(_event, data);			
		}
	}
}
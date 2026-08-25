using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace yourvrexperience.VR
{
	public class HandMenuOptions : HandMenuBase, IHandMenu
	{
		[SerializeField] private TextMeshProUGUI textTitle;
		[SerializeField] private Button optionAButton;
		[SerializeField] private Button optionBButton;
		[SerializeField] private TextMeshProUGUI textFeedback;

		protected override void Start()
		{
			base.Start();

			textTitle.text = LanguageController.Instance.GetText("screen.hand.menu.title.options");

			optionAButton.onClick.AddListener(OnOptionA);
			optionBButton.onClick.AddListener(OnOptionB);

			optionAButton.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.hand.menu.options.a");
			optionBButton.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.hand.menu.options.b");

			textFeedback.text = "";
		}

		public void OnOptionA()
		{
			textFeedback.text = "Selected Option A";
		}

		public void OnOptionB()
		{
			textFeedback.text = "Selected Option B";
		}
	}
}
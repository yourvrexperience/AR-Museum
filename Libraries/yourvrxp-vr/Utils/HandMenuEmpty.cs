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
	public class HandMenuEmpty : HandMenuBase, IHandMenu
	{
		[SerializeField] private TextMeshProUGUI textTitle;

		protected override void Start()
		{
			base.Start();

			textTitle.text = LanguageController.Instance.GetText("screen.hand.menu.title.options");
		}
	}
}
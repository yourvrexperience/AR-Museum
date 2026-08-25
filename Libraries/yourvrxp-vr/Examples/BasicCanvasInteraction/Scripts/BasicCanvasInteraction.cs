using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.VR
{
	public class BasicCanvasInteraction : MonoBehaviour
	{
		[SerializeField] TextMeshProUGUI TextInformation;
		[SerializeField] Button ButtonA;
		[SerializeField] Button ButtonB;
		[SerializeField] Button ButtonC;

		void Start()
		{
			ButtonA.onClick.AddListener(OnButtonAClicked);
			ButtonB.onClick.AddListener(OnButtonBClicked);
			ButtonC.onClick.AddListener(OnButtonCClicked);
		}

		private void OnButtonCClicked()
		{
			TextInformation.text = "Button C clicked";
		}

		private void OnButtonBClicked()
		{
			TextInformation.text = "Button B clicked";
		}

		private void OnButtonAClicked()
		{
			TextInformation.text = "Button A clicked";
		}
	}
}
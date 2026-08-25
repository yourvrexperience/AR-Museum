using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.Utils
{
	public class DebugUIDisplayController : MonoBehaviour
	{
        private static DebugUIDisplayController _instance;
        public static DebugUIDisplayController Instance
        {
            get
            {
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(DebugUIDisplayController)) as DebugUIDisplayController;
				}
				return _instance;
            }
        }

		public TextMeshProUGUI UIMessage;

		void Start()
		{
			if (UIMessage != null) UIMessage.text = "";
		}
		
		public void DisplayMessage(string message)
		{			
			if (UIMessage != null) UIMessage.text = message;
		}
	}
}
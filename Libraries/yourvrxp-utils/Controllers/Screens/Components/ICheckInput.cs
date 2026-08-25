using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.Utils
{
	public interface ICheckInput
	{
		int IsTextValid(string text);
		string GetMessageFeedback(int codeValid);
	}
}
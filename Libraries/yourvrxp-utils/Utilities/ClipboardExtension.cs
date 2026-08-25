using UnityEngine;

namespace yourvrexperience.Utils
{
	public static class ClipboardExtension
	{
		public static void CopyToClipboard(this string str)
		{
			GUIUtility.systemCopyBuffer = str;
		}
	}
}
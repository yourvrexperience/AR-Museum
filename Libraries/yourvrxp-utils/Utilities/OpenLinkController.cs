using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;

namespace yourvrexperience.Utils
{
	public static class OpenLinkController
	{
		public static void OpenLink(string url)
		{
			Application.OpenURL(url);
		}

		public static void OpenLinkJSPluginNewTab(string url)
		{
#if UNITY_IOS || UNITY_ANDROID
			Application.OpenURL(url);
#elif UNITY_WEBGL && !UNITY_EDITOR
			OpenInNewTab(url);
#else
			Application.OpenURL(url);
#endif			
		}
		public static void OpenLinkJSPluginSameTab(string url)
		{
#if UNITY_IOS || UNITY_ANDROID
			Application.OpenURL(url);
#elif UNITY_WEBGL && !UNITY_EDITOR
			OpenInSameTab(url);
#else
			Application.OpenURL(url);
#endif			
		}

		public static void SaveScreenshotPNG(string fileName, byte[] pngBytes)
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			DownloadFile(fileName, pngBytes, pngBytes.Length);
#endif			
		}

#if UNITY_WEBGL
		[DllImport("__Internal")]
		public static extern void OpenInNewTab(string url);
		[DllImport("__Internal")]
		public static extern void OpenInSameTab(string url);
		[DllImport("__Internal")]
		private static extern void DownloadFile(string filename, byte[] fileData, int dataLength);
#endif		
	}
}
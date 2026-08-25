using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;

namespace yourvrexperience.Utils
{
	public static class DeviceDectector
	{
		public static bool IsRunningInMobileDevice()
		{
#if UNITY_IOS || UNITY_ANDROID
			return true;			
#elif UNITY_WEBGL && !UNITY_EDITOR
			return IsMobileDevice() == 1;
#else
			return false;
#endif
		}

#if UNITY_WEBGL
		[DllImport("__Internal")]
		private static extern int IsMobileDevice();
#endif

	}
}
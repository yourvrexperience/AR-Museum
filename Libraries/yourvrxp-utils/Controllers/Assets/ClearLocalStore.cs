#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace yourvrexperience.Utils
{	
	public class ClearLocalStore 
	{
		[MenuItem("Asset Bundles/Clear PlayerPrefs")]
		private static void NewMenuOption()
		{
			PlayerPrefs.DeleteAll();
			Debug.Log("PlayerPrefs CLEARED!!!");
		}

        [MenuItem("Asset Bundles/Clear Cache")]
        private static void CleanCache()
        {
            if (Caching.ClearCache())
            {
                Debug.Log("Successfully cleaned the cache.");
            }
            else
            {
                Debug.Log("Cache is being used.");
            }
        }
	}
}
#endif
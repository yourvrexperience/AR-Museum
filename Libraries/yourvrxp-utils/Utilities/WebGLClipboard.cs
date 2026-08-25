using System;
using UnityEngine;
using System.Runtime.InteropServices;
using AOT;

namespace yourvrexperience.Utils
{
    public class WebGLClipboard
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    public static extern void ClipboardReader(string gObj, string vName);
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    public static extern void ClipboardPaster(string toCopy);
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    [MonoPInvokeCallback(typeof(Action))]
#endif
        public static void PasteFromWebGLClipboard(string gameObjectName, string handler)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ClipboardReader(gameObjectName, handler);   
#endif
        }


#if UNITY_WEBGL && !UNITY_EDITOR
    [MonoPInvokeCallback(typeof(Action<string>))]
#endif
        public static void CopyToWebGLClipboard(string toCopy)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ClipboardPaster(toCopy);   
#else
            GUIUtility.systemCopyBuffer = toCopy;
#endif
        }
    }
}
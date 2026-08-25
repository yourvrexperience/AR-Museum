#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public static class AddUrlScheme
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;
        string plistPath = Path.Combine(path, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        // If CFBundleURLTypes already exists (some plugins add it), append instead.
        var urlTypes = plist.root.values.ContainsKey("CFBundleURLTypes")
            ? plist.root["CFBundleURLTypes"].AsArray()
            : plist.root.CreateArray("CFBundleURLTypes");
        var dict = urlTypes.AddDict();
        dict.SetString("CFBundleURLName", "com.yourvrexperience.museumtechdemo.auth");
        dict.CreateArray("CFBundleURLSchemes").AddString("com.yourvrexperience.museumtechdemo");        
        plist.WriteToFile(plistPath);
    }
}
#endif
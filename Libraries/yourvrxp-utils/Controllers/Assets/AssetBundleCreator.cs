#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace yourvrexperience.Utils
{
    public class AssetBundleCreator : MonoBehaviour
    {
        [MenuItem("Asset Bundles/Build Asset Bundle Android")]
        static void BuildBundles()
        {
            BuildPipeline.BuildAssetBundles("AssetsBundles", BuildAssetBundleOptions.None, BuildTarget.Android);
        }

        [MenuItem("Asset Bundles/Build Asset Bundle IOS")]
        static void BuildBundlesIOS()
        {
            BuildPipeline.BuildAssetBundles("AssetsBundles", BuildAssetBundleOptions.None, BuildTarget.iOS);
        }

        [MenuItem("Asset Bundles/Build Asset Bundle WebGL")]
        static void BuildBundlesWebGL()
        {
            BuildPipeline.BuildAssetBundles("AssetsBundles", BuildAssetBundleOptions.None, BuildTarget.WebGL);
        }

        [MenuItem("Asset Bundles/Build Asset Bundle Windows")]
        static void BuildBundlesWindows()
        {
            BuildPipeline.BuildAssetBundles("AssetsBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
        }

        [MenuItem("Asset Bundles/Build Asset Bundle MacOS")]
        static void BuildBundlesMacOS()
        {
            BuildPipeline.BuildAssetBundles("AssetsBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneOSX);
        }
    }
}
#endif
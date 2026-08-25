using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.Utils
{
	public class ScreenLoadBundle : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenLoadBundle";

		[SerializeField] private TextMeshProUGUI titleScreen;
		[SerializeField] private TextMeshProUGUI progressDownload;		

		private bool _loadingFinished = false;

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			titleScreen.text = LanguageController.Instance.GetText("screen.loading.title");
			progressDownload.text = "";

			AssetBundleController.Instance.AssetBundleEvent += OnAssetBundleEvent;
			AssetBundleController.Instance.LoadAssetBundle();
		}

		public override void Destroy()
		{
			base.Destroy();
			AssetBundleController.Instance.AssetBundleEvent -= OnAssetBundleEvent;
		}

		private void OnAssetBundleEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(AssetBundleController.EventAssetBundleAssetsLoaded))
            {
                if (!_loadingFinished)
                {
                    _loadingFinished = true;
                    progressDownload.text = LanguageController.Instance.GetText("screen.loading.completed");
                    AssetBundleController.Instance.ClearAssetBundleEvents();
					SystemEventController.Instance.DelaySystemEvent(ScreenControllerApp.EventScreenControllerBasicLoadCompleted, 0.5f);
                }
            }
            if (nameEvent.Equals(AssetBundleController.EventAssetBundleAssetsProgress))
            {
                if (!_loadingFinished)
                {
                    float realProgress = ((90 * (float)parameters[0]) / 90);
                    if ((realProgress >= 0) && (realProgress <= 1))
                    {
                        progressDownload.text = LanguageController.Instance.GetText("screen.loading.progress") + " " + ((int)(100 * realProgress)) + "%";
                    }
                    else
                    {
                        progressDownload.text = "";
                    }                    
                }
            }
            if (nameEvent.Equals(AssetBundleController.EventAssetBundleAssetsUnknownProgress))
            {
                int dots = (int)parameters[0];
                string dotprogress = "";
                for (int i = 0; i < dots; i++) dotprogress += ".";
                progressDownload.text = LanguageController.Instance.GetText("message.downloading.assets.bundle") + " " + dotprogress;

                int newDots = (dots + 1) % 4;
                AssetBundleController.Instance.DelayAssetBundleEvent(AssetBundleController.EventAssetBundleAssetsUnknownProgress, 1, newDots);
            }
        }
	}
}
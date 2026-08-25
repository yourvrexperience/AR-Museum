using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;

namespace yourvrexperience.template6dof
{
    public class ScreenDownloadAssetsView : BaseScreenView, IScreenView
    {
        public const string ScreenName = "ScreenDownloadAssetsView";

        public const string EventScreenDownloadAssetsViewProgress = "EventScreenDownloadAssetsViewProgress";

        [SerializeField] private TextMeshProUGUI titleScreen;
        [SerializeField] private TextMeshProUGUI descriptionScreen;
        [SerializeField] private Image BackgroundProgressBar;
        [SerializeField] private Image ProgressBar;

        private bool _loadingFinished = false;

        public override string NameScreen
        {
            get { return ScreenName; }
        }

        public override void Initialize(params object[] parameters)
        {
            base.Initialize(parameters);

            UpdateProgressBar(0);

            AssetBundleController.Instance.AssetBundleEvent += OnAssetBundleEvent;
            SystemEventController.Instance.Event += OnSystemEvent;
        }

        public override void Destroy()
        {
            base.Destroy();
            if (AssetBundleController.Instance != null) AssetBundleController.Instance.AssetBundleEvent -= OnAssetBundleEvent;
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
        }

        private void UpdateProgressBar(float progress)
        {
            ProgressBar.fillAmount = progress;
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(GameStateDownload.EventGameStateDownloadReportNoConnection))
            {
                BackgroundProgressBar.gameObject.SetActive(false);
                ProgressBar.gameObject.SetActive(false);
                descriptionScreen.text = LanguageController.Instance.GetText("screen.download.no.internet.connection");
            }
            if (nameEvent.Equals(EventScreenDownloadAssetsViewProgress))
            {                
                UpdateProgressBar((float)parameters[0]);
            }
        }

        private void OnAssetBundleEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(AssetBundleController.EventAssetBundleAssetsLoaded))
            {
                if (!_loadingFinished)
                {
                    _loadingFinished = true;
                    UpdateProgressBar(1);
                    AssetBundleController.Instance.ClearAssetBundleEvents();
                    SystemEventController.Instance.DelaySystemEvent(GameStateDownload.EventGameStateDownloadLoadCompleted, 0.5f);
                }
            }
            if (nameEvent.Equals(AssetBundleController.EventAssetBundleAssetsProgress))
            {
                if (!_loadingFinished)
                {
                    UpdateProgressBar(0.1f + (float)parameters[0]);
                }
            }
        }
    }
}

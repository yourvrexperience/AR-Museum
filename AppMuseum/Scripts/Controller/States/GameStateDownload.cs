using UnityEngine;
using yourvrexperience.Utils;
using System;
using yourvrexperience.Networking;
using yourvrexperience.Narration;
using static yourvrexperience.Narration.GameLevelData;
using yourvrexperience.ai;
using System.Collections.Generic;
using yourvrexperience.UserManagement;

namespace yourvrexperience.template6dof
{
	public class GameStateDownload : IGameState
	{
		public const string EventGameStateDownloadLoadCompleted = "EventGameStateDownloadLoadCompleted";
		public const string EventGameStateDownloadTextCompleted = "EventGameStateDownloadTextCompleted";
		public const string EventGameStateDownloadNoConnection = "EventGameStateDownloadNoConnection";
		public const string EventGameStateDownloadReportNoConnection = "EventGameStateDownloadReportNoConnection";

		public const string CoockieAppVersion = "CoockieAppVersion";
		private int _counterNarration = 0;
		private int _newServerVersion = -1;
		private int _levelsMuseum = -1;

		public void Initialize()
		{
			SystemEventController.Instance.Event += OnSystemEvent;
			AssetBundleController.Instance.AssetBundleEvent += OnAssetBundleEvent;

			ScreenController.Instance.CreateScreen(ScreenDownloadAssetsView.ScreenName, true, false);

			_counterNarration = 0;
			GameLevelData.Instance.GetVersion();
			SystemEventController.Instance.DelaySystemEvent(EventGameStateDownloadNoConnection, 10);
		}

        public void Destroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;			
		}

		private void LoadAssetBundle(bool clearCache)
		{
			if (_newServerVersion != -1)
			{
				GameLevelData.Instance.VersionNumber = _newServerVersion;
			} 

			if (clearCache) AssetBundleController.Instance.ClearLocalCache();

			if (GameLevelData.Instance.GetDeveloperMode())
			{
#if UNITY_ANDROID
				AssetBundleController.Instance.LoadAssetBundle(GameLevelData.Instance.URLBase + "/Android/dev/template6dof");
#elif ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR
				AssetBundleController.Instance.LoadAssetBundle(GameLevelData.Instance.URLBase + "/Oculus/dev/template6dof");
#elif UNITY_IOS
				AssetBundleController.Instance.LoadAssetBundle(GameLevelData.Instance.URLBase + "/IOS/dev/template6dof");
#elif UNITY_WEBGL
				AssetBundleController.Instance.LoadAssetBundle(GameLevelData.Instance.URLBase + "/webgl/dev/template6dof");
#endif			
			}
			else
			{
#if UNITY_ANDROID
				AssetBundleController.Instance.LoadAssetBundle(GameLevelData.Instance.URLBase + "/Android/prod/template6dof");
#elif ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR
				AssetBundleController.Instance.LoadAssetBundle(GameLevelData.Instance.URLBase + "/Oculus/prod/template6dof");
#elif UNITY_IOS
				AssetBundleController.Instance.LoadAssetBundle(GameLevelData.Instance.URLBase + "/IOS/prod/template6dof");
#elif UNITY_WEBGL
				AssetBundleController.Instance.LoadAssetBundle(GameLevelData.Instance.URLBase + "/webgl/prod/template6dof");
#endif			
			}
		}

		private void OnAssetBundleEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(AssetBundleController.EventAssetBundleAssetsLoaded))
			{
				SystemEventController.Instance.DelaySystemEvent(EventGameStateDownloadLoadCompleted, 0.1f);
				if (AssetBundleController.Instance != null) AssetBundleController.Instance.AssetBundleEvent -= OnAssetBundleEvent;
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(EventGameStateDownloadNoConnection))
			{
				SystemEventController.Instance.DispatchSystemEvent(EventGameStateDownloadReportNoConnection);
			}
			if (nameEvent.Equals(ConsultVersionHTTP.EventConsultVersionHTTPCompleted))
			{
				SystemEventController.Instance.ClearSystemEvents(EventGameStateDownloadNoConnection);
				bool isConnection = (bool)parameters[0];
				if (!isConnection)
				{
					SystemEventController.Instance.DispatchSystemEvent(EventGameStateDownloadReportNoConnection);
				}
				else
				{
					int serverVersion = -1;
					_levelsMuseum = (int)parameters[3];					
					if (GameLevelData.Instance.GetDeveloperMode())
					{					
						serverVersion = (int)parameters[1];
						GameLevelData.Instance.UnlockSecretsIndex = (int)parameters[4];
						GameLevelData.Instance.VersionAssets = (string)parameters[6];
					}
					else
					{
						serverVersion = (int)parameters[2];
						GameLevelData.Instance.UnlockSecretsIndex = (int)parameters[5];
						GameLevelData.Instance.VersionAssets = (string)parameters[7];
					}					
					if (GameLevelData.Instance.VersionNumber != serverVersion)
					{
						_newServerVersion = serverVersion;						
					}
					_counterNarration = 0;			
					if (GameLevelData.Instance.IsMuseumEmpty() || (_newServerVersion != -1))
					{
						GameLevelData.Instance.SetTotalNarrations(_levelsMuseum);												
					}
					GameLevelData.Instance.ConsultPOIs(_counterNarration, -1, GameLevelData.Instance.GetDeveloperMode());
				}
			}
			if (nameEvent.Equals(ConsultPOIsHTTP.EventConsultPOIsHTTPCompleted))
			{
				if ((bool)parameters[0])
				{
					string poiData = (string)parameters[1];
					string secretsData = (string)parameters[2];
					string narrationData = (string)parameters[3];

					POIPosition[] poiLevelData = GameLevelData.Instance.UnPackPOIStringData(poiData);
					SecretPosition[] secretLevelData = GameLevelData.Instance.UnPackSecretStringData(secretsData);
					if (GameLevelData.Instance.IsMuseumEmpty(_counterNarration) || (_newServerVersion != -1))
					{
						GameLevelData.Instance.SetTotalSizeNarration(_counterNarration, poiLevelData.Length, secretLevelData.Length);
					}		
					GameLevelData.Instance.SetLevelNarration(_counterNarration, narrationData);
					GameLevelData.Instance.SetPOIsPositions(_counterNarration, poiLevelData);
					GameLevelData.Instance.SetSecretsPositions(_counterNarration, secretLevelData);					
					// IF WE CHANGE OF VERSION WE RESET THE LOCAL DATA AND INIT AGAIN					
					_counterNarration++;
					if (_counterNarration < GameLevelData.Instance.GetTotalSizeNarrations())
					{
						float progressDone = 0.1f * ((float)_counterNarration / (float)GameLevelData.Instance.GetTotalSizeNarrations());
						SystemEventController.Instance.DispatchSystemEvent(ScreenDownloadAssetsView.EventScreenDownloadAssetsViewProgress, progressDone);
						GameLevelData.Instance.ConsultPOIs(_counterNarration, -1, GameLevelData.Instance.GetDeveloperMode());
					}
					else
					{
						_counterNarration = 100;
						if (_newServerVersion != -1)
						{
							GameLevelData.Instance.SaveGameProgressLocally();
						}						
					}
				}
				if (_counterNarration >= GameLevelData.Instance.GetTotalSizeNarrations())
				{
					if (GameLevelData.Instance.GetDeveloperMode())
					{
						CommController.Instance.GetFileData(GameLevelData.Instance.URLBase + "Texts/GameTextsDevelopment.xml");
					}
					else
					{
						CommController.Instance.GetFileData(GameLevelData.Instance.URLBase + "Texts/GameTextsProduction.xml");
					}					
				}
			}
			if (nameEvent.Equals(GetFileDataHTTP.EventGetFileDataHTTP))
			{
				if ((bool)parameters[0])
				{
					if (((string)parameters[1]).Length > 200)
					{
						LanguageController.Instance.LoadGameTexts((string)parameters[1]);
					}					
				}
				LoadAssetBundle(_newServerVersion != -1);
			}		
			if (nameEvent.Equals(EventGameStateDownloadLoadCompleted))
			{
				if (!UsersController.Instance.LogIn())
				{
					 MainController.Instance.ChangeGameState(MainController.StatesGame.MainMenu);
				}
			}
			if (nameEvent.Equals(UsersController.EVENT_USER_LOGIN_FORMATTED))
			{
				if (UsersController.Instance.CurrentUser != null)
				{
					if (!UsersController.Instance.CurrentUser.IsEmptyUser())
					{					
						GameLevelData.Instance.UnpackAdminConsumption(UsersController.Instance.CurrentUser.Profile.Data2);
						GameLevelData.Instance.UnpackConsumerConsumption(UsersController.Instance.CurrentUser.Profile.Data3);
					}
				}				
				MainController.Instance.ChangeGameState(MainController.StatesGame.MainMenu);
			}
		}

		public void Run()
		{
		}
	}
}
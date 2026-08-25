using UnityEngine;
using yourvrexperience.Utils;
using System;
using yourvrexperience.Networking;
using yourvrexperience.Narration;
using yourvrexperience.ai;
#if ENABLE_GOOGLE_SPEECH	
using yourvrexperience.speech;
#endif

namespace yourvrexperience.template6dof
{
	public class GameStateLoad : IGameState
    {
		public const string EventGameStateLoadCompleted = "EventGameStateLoadCompleted";
		public const string EventGameStateLoadLocalDelayToCreate = "EventGameStateLoadLocalDelayToCreate";

		private bool _processCompleted = false;

		public void Initialize()
		{
			SystemEventController.Instance.Event += OnSystemEvent;

			ScreenController.Instance.CreateForwardScreen(ScreenLoadingView.ScreenName, new Vector3(0,0,1), true, false, LanguageController.Instance.GetText("text.loading"));

			MainController.Instance.FadeInCamera();

			if (MainController.Instance.IsMultiplayer)
			{
				SystemEventController.Instance.DelaySystemEvent(ScreenLoadingView.EventScreenLoadingViewUpdateText, 0.01f, LanguageController.Instance.GetText("screen.loading.progress"));
				NetworkController.Instance.NetworkEvent += OnNetworkEvent;
			}
			else
			{
				SystemEventController.Instance.DelaySystemEvent(ScreenLoadingView.EventScreenLoadingViewUpdateText, 0.01f, LanguageController.Instance.GetText("screen.loading"));
				SystemEventController.Instance.DelaySystemEvent(EventGameStateLoadLocalDelayToCreate, 1);
			}
		}

        public void Destroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (MainController.Instance.IsMultiplayer)
			{
				if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
			}
		}

		private void CreateGameElements()
		{
			if (!_processCompleted)
			{
				_processCompleted = true;
				SystemEventController.Instance.DispatchSystemEvent(MainController.EventMainControllerReleaseGameResources);
				MainController.Instance.CreateGameElementsView();
				MainController.Instance.FadeOutCamera();
			}
		}

		private void CheckToInitGame()
		{
			InitGame();
		}

		private void InitGame()
		{
#if ENABLE_GOOGLE_SPEECH
            SpeechRecognitionController.Instance.InitRecognitionLanguage(LanguageController.Instance.CodeLanguage);
			string speechVoice = LanguageController.Instance.GetSpeechVoice(LanguageController.Instance.CodeLanguage);
			if (speechVoice != null)
			{
				SpeechRecognitionController.Instance.SetVoiceByLanguage(LanguageController.Instance.CodeLanguage, speechVoice, speechVoice, speechVoice, speechVoice, speechVoice, speechVoice);
			}
#endif
			ScreenController.Instance.DestroyScreens();
			MainController.Instance.ChangeGameState(MainController.StatesGame.Run);
		}

		private void LoadGameTexts()
		{
			string finalURLGameTexts = "";
			if (GameLevelData.Instance.GetDeveloperMode())
			{
				finalURLGameTexts = GameLevelData.Instance.URLBase + "Texts/GameTextsDevelopment"+(int)GameLevelData.Instance.Age+".xml";
			}
			else
			{
				finalURLGameTexts = GameLevelData.Instance.URLBase + "Texts/GameTextsProduction"+(int)GameLevelData.Instance.Age+".xml";
			}					
			CommController.Instance.GetFileData(finalURLGameTexts);
		}

        private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
        {
			if (nameEvent.Equals(MainController.EventMainControllerGameReadyToStart))
			{
				LoadGameTexts();				
			}
            if (nameEvent.Equals(MainController.EventMainControllerAllPlayerViewReadyToStartGame))
			{
				CheckToInitGame();
			}
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(EventGameStateLoadLocalDelayToCreate))
			{
				LoadGameTexts();
			}
            if (nameEvent.Equals(MainController.EventMainControllerAllPlayerViewReadyToStartGame))
			{
				if (!MainController.Instance.IsMultiplayer)
				{
					CheckToInitGame();
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
				CreateGameElements();
			}		
        }
		
		public void Run()
		{
		}
	}
}
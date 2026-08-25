using yourvrexperience.Utils;
using UnityEngine;
using System.Collections.Generic;
using yourvrexperience.ai;
using System;
using System.Linq;
using yourvrexperience.Narration;
using static yourvrexperience.Utils.SoundsController;
using static yourvrexperience.Narration.NarrationController;
using static yourvrexperience.template6dof.LevelView;
using yourvrexperience.UserManagement;

namespace yourvrexperience.template6dof
{
	public class SpeechDatabaseController : MonoBehaviour
	{
        static readonly Dictionary<string, int> LanguageIds = new()
        {
            ["ca"] = 0,
            ["es"] = 1,
            ["en"] = 2,
            ["fr"] = 3,
            ["de"] = 4,
            ["it"] = 5
            // ....
        };    

		public enum GenderTypes { MALE = 0, FEMALE = 1, NONE = 2 }

		[Serializable]
		public class VoiceProviderEntryJSON
		{
			public string id;
			public string display_name;
			public string gender;
			public string description;
			public string audio;

			public AudioType GetAudioType()
            {
				if (audio.IndexOf(".mp3") != -1)
                {
					return AudioType.MPEG;
                }
				else if (audio.IndexOf(".ogg") != -1)
				{
					return AudioType.OGGVORBIS;
				}
				else if (audio.IndexOf(".wav") != -1)
				{
					return AudioType.WAV;
				}
				else
                {
					return AudioType.UNKNOWN;
				}
			}

			public GenderTypes GetGender()
            {
				if (gender == null)
                {
					return GenderTypes.NONE;
                }
				else
                {
					if (gender.ToLower().Equals("male"))
                    {
						return GenderTypes.MALE;
					}
					if (gender.ToLower().Equals("female"))
					{
						return GenderTypes.FEMALE;
					}
				}
				return GenderTypes.NONE;
			}

			public override string ToString()
			{
				return "ID[" + id + "]::NAME[" + display_name + "]::GENDER[" + gender + "]::DESCRIPTION[" + description + "]::AUDIO["+ audio + "]";
			}
		}


		public const int TOTAL_ALLOWED_SPEECHES = 50;

		public const string TagFilenameSpeech = "speech_";

		public const string EventSpeechDatabaseControllerDownloadSpeech = "EventSpeechDatabaseControllerDownloadSpeech";
		public const string EventSpeechDatabaseControllerDownloadedAudioClip = "EventSpeechDatabaseControllerDownloadedAudioClip";
		public const string EventSpeechDatabaseControllerAvailableSpeech = "EventSpeechDatabaseControllerAvailableSpeech";
		public const string EventSpeechDatabaseControllerUpdateDataCompleted = "EventSpeechDatabaseControllerUpdateDataCompleted";
		public const string EventSpeechDatabaseControllerSpeechGenerated = "EventSpeechDatabaseControllerSpeechGenerated";
		public const string EventSpeechDatabaseControllerSpeechStored = "EventSpeechDatabaseControllerSpeechStored";
		public const string EventSpeechDatabaseControllerSpeechAIGenerated = "EventSpeechDatabaseControllerSpeechAIGenerated";		
		public const string EventSpeechDatabaseControllerCreatedAudioClip = "EventSpeechDatabaseControllerCreatedAudioClip";

		private static SpeechDatabaseController _instance;

        public static SpeechDatabaseController Instance
        {
            get
            {
                if (!_instance)
                {					
                    _instance = GameObject.FindObjectOfType(typeof(SpeechDatabaseController)) as SpeechDatabaseController;
                }
                return _instance;
            }
        }

		private List<int> _speechIndex = new List<int>();
		private Dictionary<int, AudioClip> _speechData = new Dictionary<int, AudioClip>();
		private List<int> _speechesToProcess = new List<int>();		

		private Dictionary<string, ItemMultiObjectEntry> _speechesToGenerate = new Dictionary<string, ItemMultiObjectEntry>();
		private Dictionary<int, ItemMultiObjectEntry> _speechesToDownload = new Dictionary<int, ItemMultiObjectEntry>();
		private string _isProcessingSpeech = "";

		private int _generatedID;
		private string _voice;
		private string _text = "";
        private int _age;
        private int _floor;
        private int _poi;
        private int _segment;
		private int _secret = -1;
		private string _language;
		private string _generatedEvent;

		private bool _channelVoice = false;

		private TTSpeechProvider _speechProvider = TTSpeechProvider.None;

		private bool _shouldPlayAudio = false;

		private int _secretToPlay;
		private int _poiToPlay;
		private int _segmentToPlay;
		private ChannelsAudio _channelToPlay;
		private float _volumeToPlay;

		private bool _processingReceivedMicrophone = false;

		public bool ChannelVoice
        {
			get { return _channelVoice; }
			set { _channelVoice = value; }
        }

		public TTSpeechProvider SpeechProvider
        {
			get { return _speechProvider; }
			set { _speechProvider = value; }
		}

		public void Initialize()
		{	
			SystemEventController.Instance.Event += OnSystemEvent;
		}

		public void InitTTSProvider()
        {
			_speechProvider = TTSpeechProvider.Speechify;
		}

        void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

        public bool OnUploadSpeech(string text, int secret, int age, int floor, int poi, int segment, string language)
        {
			_text = text;
			_secret = secret;
			_language = language;
			_age = age;
			_floor = floor;
			_poi = poi;
			_segment = segment; 

#if UNITY_WEBGL && !UNITY_EDITOR
			ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, "", LanguageController.Instance.GetText("message.please.wait"));
			WebFileBrowser.Upload(OnUploadSpeech, "ogg");						
#endif

			return false;
        }
		
        private void OnUploadSpeech(string fileName, string mime, byte[] bytes)
        {
			if (bytes == null)
			{
				UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, LanguageController.Instance.GetText("text.error"), LanguageController.Instance.GetText("text.no.voice.recorded.micro"));
			}
			else
			{
				RemoveSpeechDataByID(GetSpeechID(_secret, _age, _floor, _poi, _segment, _language));
				string textFinal = _text;
				GameLevelData.Instance.StoreSpeech((int)UsersController.Instance.CurrentUser.Id, UsersController.Instance.CurrentUser.PasswordPlain, EventSpeechDatabaseControllerSpeechStored, _secret, textFinal, _age, _floor, _poi, _segment, _language, bytes);
			}
        }
		
		public int CountSpeechData()
		{
			return _speechData.Count;
		}

        public int GetSpeechID(int secret, int age, int floor, int poi, int segment, string language)
        {                        
            int lang = LanguageIds[language];      // 0–99

			if (secret >= 0)
			{
				return segment + 100 * poi + 1000 * floor + 100000 * age + 1000000 * lang + 10000000 * (secret + 1);
			}
			else
			{
				return segment + 100 * poi + 1000 * floor + 100000 * age + 1000000 * lang;
			}            	
        }

		public AudioClip GetSpeechDataByID(int secret, int age, int floor, int poi, int segment, string language)
		{
            return GetSpeechDataByID(GetSpeechID(secret, age, floor, poi, segment, language));
        }

		private AudioClip GetSpeechDataByID(int id)
		{
			AudioClip speech = null;
			if (_speechData.TryGetValue(id, out speech))
			{
				return speech;
			}
			else
			{
				return null;
			}
		}

		private bool RemoveSpeechDataByID(int id)
		{
			return _speechData.Remove(id);
		}

		public void AddSpeechData(int secret, int age, int floor, int poi, int segment, string language, AudioClip speech)
		{
            int id = GetSpeechID(secret, age, floor, poi, segment, language);
            AddSpeechData(id, speech);
		}

        public void AddSpeechData(int id, AudioClip speech)
        {
            if (_speechData.ContainsKey(id))
			{
				_speechData.Remove(id);
			}
			_speechData.Add(id, speech);
			if (!_speechIndex.Contains(id)) _speechIndex.Add(id);
			if (_speechIndex.Count > TOTAL_ALLOWED_SPEECHES)
            {
				int indexSpeech = _speechIndex[0];
				_speechData.Remove(indexSpeech);
				_speechIndex.RemoveAt(0);
			}
        }        

		public void RegisterNewSpeech(string text, ItemMultiObjectEntry item)
        {
			ItemMultiObjectEntry found;
			if (!_speechesToGenerate.TryGetValue(text, out found))
            {
				_speechesToGenerate.Add(text, item);
			}

			CheckProcessSpeech();
		}

		public void PlaySpeech(string text, string generatedEvent)
        {
			_generatedEvent = generatedEvent;
			_text = text;
			string voice = LanguageController.Instance.GetNarrationVoice(LanguageController.Instance.CodeLanguage);
			GameAIData.Instance.AskGenericTTSpeechDirectAI(voice, text, LanguageController.Instance.CodeLanguage, "", EventSpeechDatabaseControllerSpeechAIGenerated);		
		}

		private void CheckProcessSpeech()
        {
			if ((_isProcessingSpeech.Length == 0) && (_speechesToGenerate.Count > 0))
            {
				foreach (var pair in _speechesToGenerate)
				{
					_isProcessingSpeech = pair.Key;
					break;
				}
				ItemMultiObjectEntry processItem;
				if (_speechesToGenerate.TryGetValue(_isProcessingSpeech, out processItem))
				{
					_secret = (int)processItem.Objects[0];
                    _text = (string)processItem.Objects[1];
					_voice = (string)processItem.Objects[2];
					_language = (string)processItem.Objects[3];
                    _age = (int)processItem.Objects[4];
                    _floor = (int)processItem.Objects[5];
                    _poi = (int)processItem.Objects[6];
                    _segment = (int)processItem.Objects[7]; 
					 
                    GameAIData.Instance.AskGenericTTSpeechDirectAI(_voice, _text, _language, "", EventSpeechDatabaseControllerSpeechGenerated);
				}
			}
		}

		private void PlayAudio(int secret, int poi, int segment, ChannelsAudio channel, float volume)
		{
			AudioClip audioNarration = GetSpeechDataByID(secret, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, poi, segment, LanguageController.Instance.CodeLanguage);
			if (audioNarration == null)
			{
				_shouldPlayAudio = true;	
				AddSpeechToDownload(secret, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, poi, segment, LanguageController.Instance.CodeLanguage);
				ProcessDownloadSpeeches();
			}
			else
			{
				_shouldPlayAudio = false;
				if (MainController.Instance.EnableEditionPOIs)
				{
					SystemEventController.Instance.DelaySystemEvent(ScreenXMLEditSegmentView.EventScreenXMLEditSegmentViewStopPlaying, audioNarration.length);
				}				
				SoundsController.Instance.PlaySoundClipFx(channel, audioNarration, false, volume, false);
			}		
		}

		private void ProcessedTextConfirmation(string textProcessed)
        {
			if (_isProcessingSpeech.Equals(textProcessed))
			{
				if (_speechesToGenerate.Count > 0)
				{
					if (_speechesToGenerate.Remove(textProcessed))
					{
						_isProcessingSpeech = "";
						CheckProcessSpeech();
					}
				}
			}
		}

		private void AddSpeechToDownload(int secret, int age, int floor, int poi, int segment, string language)
		{
			int speechId = GetSpeechID(secret, age, floor, poi, segment, language);
			AudioClip audioPoiSegment = GetSpeechDataByID(speechId);
			if (audioPoiSegment == null)
			{
				ItemMultiObjectEntry found = null;
				if (!_speechesToDownload.TryGetValue(speechId, out found))
				{
					_speechesToDownload.Add(speechId, new ItemMultiObjectEntry(secret, age, floor, poi, segment, language));
				}
			}
		}		                    

		private void ProcessDownloadSpeeches(int speechDownloaded = -1)
		{
			if (_speechesToDownload.Remove(speechDownloaded))
			{
				Debug.Log("===================== REMOVED SUCCESSFULLY SPEECH DOWNLOADED["+speechDownloaded+"]");
			}

			if (_speechesToDownload.Count > 0)
			{
				int speechToDownload = -1;
				foreach (var pair in _speechesToDownload)
				{
					speechToDownload = pair.Key;
					break;
				}				
				ItemMultiObjectEntry downloadItem;
				if (_speechesToDownload.TryGetValue(speechToDownload, out downloadItem))
				{   
					int secret = (int)downloadItem.Objects[0];                 
                    int age = (int)downloadItem.Objects[1];
                    int floor = (int)downloadItem.Objects[2];
                    int poi = (int)downloadItem.Objects[3];
                    int segment = (int)downloadItem.Objects[4]; 
					string language = (string)downloadItem.Objects[5];

                    SystemEventController.Instance.DispatchSystemEvent(EventSpeechDatabaseControllerDownloadSpeech, secret, age, floor, poi, segment, language);
				}
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(NarrationController.EventNarrationControllerDownloadPOIAudios))
			{
				int poiToDownload = (int)parameters[0];
				List<NarrationToken> segmentsToDownload = (List<NarrationToken>)parameters[1];
				for (int i = 0; i < segmentsToDownload.Count; i++)
				{
					AddSpeechToDownload(-1, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, poiToDownload, i, LanguageController.Instance.CodeLanguage);
				}

				ProcessDownloadSpeeches();
			}
			if (nameEvent.Equals(NarrationData.EventNarrationPlayAudio))
			{
				_secretToPlay = (int)parameters[0];
				_poiToPlay = (int)parameters[1];
				_segmentToPlay = (int)parameters[2];
				_channelToPlay = (ChannelsAudio)parameters[3];
				_volumeToPlay = (float)parameters[4];
				PlayAudio(_secretToPlay, _poiToPlay, _segmentToPlay, _channelToPlay, _volumeToPlay);				
			}
			if (nameEvent.Equals(EventSpeechDatabaseControllerSpeechAIGenerated))
			{
				if ((bool)parameters[0])
				{
					SoundsController.Instance.LoadSoundDataBytes((byte[])parameters[2], EventSpeechDatabaseControllerCreatedAudioClip, -1, ".ogg", true);
				}
				else
				{
					SystemEventController.Instance.DispatchSystemEvent(_generatedEvent, false, _text);
				}
			}
			if (nameEvent.Equals(EventSpeechDatabaseControllerCreatedAudioClip))
			{
				if ((bool)parameters[0])
				{
					AudioClip audioSpeechDownloaded = (AudioClip)parameters[4];
					SystemEventController.Instance.DispatchSystemEvent(_generatedEvent, true, _text, audioSpeechDownloaded);
				}
				else
				{
					SystemEventController.Instance.DispatchSystemEvent(_generatedEvent, false, _text);
				}
			}
			if (nameEvent.Equals(EventSpeechDatabaseControllerSpeechGenerated))
            {								
				if ((bool)parameters[0])
				{
                    if (_text.Equals((string)parameters[1]))
                    {
						RemoveSpeechDataByID(GetSpeechID(_secret, _age, _floor, _poi, _segment, _language));
                        GameLevelData.Instance.StoreSpeech((int)UsersController.Instance.CurrentUser.Id, UsersController.Instance.CurrentUser.PasswordPlain, EventSpeechDatabaseControllerSpeechStored, _secret, _text, _age, _floor, _poi, _segment, _language, (byte[])parameters[2]);
                    }
				}
				else
				{
					UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
					ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, LanguageController.Instance.GetText("text.error"), LanguageController.Instance.GetText("text.no.voice.generated"));
					_speechesToGenerate.Clear();
					_isProcessingSpeech = "";
				}
			}
			if (nameEvent.Equals(EventSpeechDatabaseControllerSpeechStored))
			{
				if ((bool)parameters[0])
                {
                    if (_text.Equals((string)parameters[1]))
                    {
						SystemEventController.Instance.DispatchSystemEvent(EventSpeechDatabaseControllerDownloadSpeech, _secret, _age, _floor, _poi, _segment, _language);
                    }
					else
					{						
						UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
						ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, LanguageController.Instance.GetText("text.error"), LanguageController.Instance.GetText("text.no.voice.generated"));
						_speechesToGenerate.Clear();
						_isProcessingSpeech = "";
					}
                }
				else
				{
					UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
					ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, LanguageController.Instance.GetText("text.error"), LanguageController.Instance.GetText("text.no.voice.generated"));
					_speechesToGenerate.Clear();
					_isProcessingSpeech = "";
				}			
			}
			if (nameEvent.Equals(EventSpeechDatabaseControllerDownloadSpeech))
			{
				int secret = (int)parameters[0];
				int age = (int)parameters[1];
                int floor = (int)parameters[2];
                int poi = (int)parameters[3];
                int segment = (int)parameters[4];
                string language = (string)parameters[5];

                UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);

                int idSpeech = GetSpeechID(secret, age, floor, poi, segment, language);
				AudioClip data = GetSpeechDataByID(idSpeech);
				if (data == null)
				{
					if (!_speechesToProcess.Contains(idSpeech))
                    {
						_speechesToProcess.Add(idSpeech);
						_speechData.Remove(idSpeech);
						if (MainController.Instance.EnableEditionPOIs)
						{
							ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, "", LanguageController.Instance.GetText("message.please.wait"));
						}
						GameLevelData.Instance.DownloadSpeech(EventSpeechDatabaseControllerDownloadedAudioClip, idSpeech, secret, age, floor, poi, segment, language);
					}
				}
				else
				{
                    SystemEventController.Instance.DispatchSystemEvent(EventSpeechDatabaseControllerAvailableSpeech, idSpeech, true);
					ProcessDownloadSpeeches(idSpeech);
					if (_shouldPlayAudio)
					{
						PlayAudio(_secretToPlay, _poiToPlay, _segmentToPlay, _channelToPlay, _volumeToPlay);
					}
				}				
			}
			if (nameEvent.Equals(EventSpeechDatabaseControllerDownloadedAudioClip))
			{
				bool success = (bool)parameters[0];
				int idSpeech = (int)parameters[1];
				UIEventController.Instance.DispatchUIEvent(ScreenInformationView.EventScreenInformationRequestAllScreensDestroyed);
				if (_speechesToProcess.Remove(idSpeech))
                {
					Debug.Log("--["+success+"] REMOVED SPEECH[" + idSpeech + "] OF TOTAL=" + _speechesToProcess.Count);
				}
				if (success)
				{
					bool shouldReport = (bool)parameters[2];
					AudioClip audioSpeechDownloaded = (AudioClip)parameters[4];
                    AddSpeechData(idSpeech, audioSpeechDownloaded);
					if (_shouldPlayAudio)
					{
						PlayAudio(_secretToPlay, _poiToPlay, _segmentToPlay, _channelToPlay, _volumeToPlay);
					}
                    SystemEventController.Instance.DispatchSystemEvent(EventSpeechDatabaseControllerAvailableSpeech, idSpeech, true);					
				}
				else
				{
					_shouldPlayAudio = false;
					SystemEventController.Instance.DispatchSystemEvent(EventSpeechDatabaseControllerAvailableSpeech, idSpeech, false);					
				}				
				ProcessDownloadSpeeches(idSpeech);
				ProcessedTextConfirmation(_text);
			}
			if (nameEvent.Equals(MainController.EventMainControllerReleaseGameResources))
			{
				_speechData.Clear();
				_speechIndex.Clear();
				_speechesToProcess.Clear();
                _speechProvider = TTSpeechProvider.None;
			}
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerReleaseAllResources))
			{
				_instance = null;
				_speechData.Clear();
				_speechIndex.Clear();
				GameObject.Destroy(this.gameObject);
			}
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))
			{
				if (Instance)
				{
					DontDestroyOnLoad(Instance.gameObject);
				}
			}
		}
	}
}
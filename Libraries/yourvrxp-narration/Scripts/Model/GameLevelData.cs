using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Xml.Serialization;
using System.IO;
#if ENABLE_INPUT_FORM
using yourvrexperience.UserManagement;
#endif
using yourvrexperience.Utils;

namespace yourvrexperience.Narration
{
    [CreateAssetMenu(menuName = "Game/NarrationLevelData")]
	public class GameLevelData : ScriptableObject
    {
        public const int MAXIMUM_NUMBER_SECRETS = 20;
        public const int MAXIMUM_NUMBER_POIS = 30;
        public const int TOTAL_SECONDS_DAY = 86400;

        public const int TOTAL_ADMIN_OPERATIONS = 300;
        public const int TOTAL_CUSTOMER_OPERATIONS = 50;

        public const string EventGameLevelDataCompletedUpdate  = "EventGameLevelDataCompletedUpdate";
        public const string EventGameLevelDataSelectedIndexPOI  = "EventGameLevelDataSelectedIndexPOI";
        public const string EventGameLevelDataAddNewPOI  = "EventGameLevelDataAddNewPOI";
        public const string EventGameLevelDataRemovePOI  = "EventGameLevelDataRemovePOI";
        public const string EventGameLevelDataClearAll  = "EventGameLevelDataClearAll";        
        public const string EventGameLevelDataRefreshPOILevel  = "EventGameLevelDataRefreshPOILevel";
        public const string EventGameLevelDataDestroyNarrationObjects  = "EventGameLevelDataDestroyNarrationObjects";
		public const string EventGameLevelDataEditModeChanged = "EventGameLevelDataEditModeChanged";
        public const string EventGameLevelDataSaveAllData = "EventGameLevelDataSaveAllData";
        public const string EventGameLevelDataRefreshLocalData = "EventGameLevelDataRefreshLocalData";
        public const string EventGameLevelDataReorderedPOIs = "EventGameLevelDataReorderedPOIs";
        public const string EventGameLevelDataSpeechesDeleted = "EventGameLevelDataSpeechesDeleted";
        public const string EventGameLevelDataDeleteAndReorderSpeeches = "EventGameLevelDataDeleteAndReorderSpeeches";
        public const string EventGameLevelDataReorderAndSavePOIs = "EventGameLevelDataReorderAndSavePOIs";        

        public const string EventCommInsertPOIsHTTP  = "yourvrexperience.Narration.InsertPOIsHTTP";
        public const string EventCommConsultPOIsHTTP  = "yourvrexperience.Narration.ConsultPOIsHTTP";

        public const string EventCommGetVersionHTTP  = "yourvrexperience.Narration.ConsultVersionHTTP";
        public const string EventCommSetVersionHTTP  = "yourvrexperience.Narration.SetVersionHTTP";
        public const string EventCommStoreSpeechHTTP  = "yourvrexperience.Narration.StoreSpeechHTTP";        
        public const string EventCommDownloadSpeechHTTP  = "yourvrexperience.Narration.DownloadSpeechHTTP";        
        public const string EventCommDeleteSpeechHTTP  = "yourvrexperience.Narration.DeleteSpeechHTTP";        
        public const string EventCommReorderPOISpeechesHTTP  = "yourvrexperience.Narration.ReorderPOISpeechesHTTP";        
        public const string EventCommReorderSecretsSpeechesHTTP  = "yourvrexperience.Narration.ReorderSecretsSpeechesHTTP";        
        

        public const string DeveloperMode  = "developer6dofmode";
        public const string VersionText  = "versionm6doftexts";
        public const string VersionAssetsText  = "versionassets6doftexts";        
        public const string NarrationTexts  = "narrationtexts";
        public const string POIsDataTexts  = "poisdatatexts";
        public const string SecretsDataTexts  = "secretsdatatexts";
        public const string GameProgressTexts  = "gameprogresstexts";
        public const string AdminAIConsumption  = "adminaiconsumption";
        public const string CustomerAIConsumption  = "customeraiconsumption";

        public const string LanguagesText  = "languagestexts";
        public const string ExtraNarration0Text  = "extranarration0texts";
        public const string ExtraNarration1Text  = "extranarration1texts";
        public const string ExtraNarration2Text  = "extranarration2texts";
        public const string ExtraNarration3Text  = "extranarration3texts";

        [System.Serializable]
        public class AIConsumption
        {
            public long Timestamp = 0;
            public int TotalAIRequests = 0;

            public AIConsumption(long timestamp, int totalAIRequests)
            {
                Timestamp = timestamp;
                TotalAIRequests = totalAIRequests;
            }
        }

        [XmlRoot(ElementName="assets")]
        public class Assets
        {
            [XmlElement(ElementName="asset")]
            public List<Asset> AssetList { get; set; }
        }

        public class Asset
        {
            [XmlAttribute(AttributeName="type")]
            public string Type { get; set; }
            
            [XmlAttribute(AttributeName="name")]
            public string Name { get; set; }
            
            [XmlAttribute(AttributeName="value")]
            public string Value { get; set; }
            [XmlAttribute(AttributeName="animations")]
            public string Animations { get; set; }
        }

 		[System.Serializable]
        public class POIPosition
        {
            public int ID = 0;
            public Vector3 Position = Vector3.zero;

            public POIPosition(int id, Vector3 position)
            {
                ID = id;
                Position = position;
            }
            public POIPosition(POIPosition data)
            {
                ID = data.ID;
                Position = data.Position;
            }
        }

        [System.Serializable]
        public class SerializedPOIPosition
        {
            public POIPosition[] Positions;
        }

 		[System.Serializable]
        public class SecretPosition
        {
            public int ID = 0;
            public Vector3 Position = Vector3.zero;
            public string CustomEvent = "";
            public string Narration = "";

            private string _realNarration;

            public string RealNarration
            {
                get { return _realNarration; }
            }

            public void SetHexNarration(string narration)
            {
                if (narration.Length > 0)
                {
                    Narration =  HexadecimalEncoding.ToHexString(narration);
                }               
               _realNarration = narration;
            }
            public void GetRealNarration(string narration)
            {
                Narration = narration;
                if (Narration.Length > 0)
                {
                    try {
                        _realNarration = HexadecimalEncoding.FromHexString(Narration);
                    } catch (Exception err) {};                    
                }                
            }

            public SecretPosition(int id, Vector3 position, string customEvent, string narration)
            {
                ID = id;
                Position = position;
                CustomEvent = customEvent;
                Narration = narration;
                GetRealNarration(Narration);
            }
            public SecretPosition(SecretPosition data)
            {
                ID = data.ID;
                Position = data.Position;
                Narration = data.Narration;
                CustomEvent = data.CustomEvent;
                GetRealNarration(Narration);
            }
        }

        [System.Serializable]
        public class SerializedSecretPosition
        {
            public SecretPosition[] Secrets;
        }

        [System.Serializable]
        public class QuestionForm
        {
            public int ID = 0;
            public string Response = "";

            public QuestionForm(int id, string response)
            {
                ID = id;
                Response = response;
            }
        }

        [System.Serializable]
        public class SerializedQuestionsForm
        {
            public QuestionForm[] Questions;
        }

        [System.Serializable]
        public class MapDataContent
        {
            public int ID = 0;
            public int[] POIs;
            public bool[] Secrets;
            public POIPosition[] Positions;
            public SecretPosition[] SecretsData;
            public string Narration;

            public MapDataContent(int id, int[] pois, bool[] secrets)
            {
                ID = id;
                POIs = pois;
                Secrets = secrets;
            }

            public void Reset()
            {
                for(int i = 0; i < POIs.Length; i++) POIs[i] = 0;
                for(int i = 0; i < Secrets.Length; i++) Secrets[i] = false;
            }
        }

        [System.Serializable]
        public class SerializedMapDataContent
        {
            public MapDataContent[] Maps;
        }

        public const string EventGameLevelDataScoreUpdated = "EventGameLevelDataScoreUpdated";
        public const string EventGameLevelDataTimeUpdated = "EventGameLevelDataTimeUpdated";

        public const string EventCommInsertFormHTTP = "yourvrexperience.ar.museum.InsertFormHTTP";
        
        public enum GameLevelStates { Initialization = 0, Synchronization, InGame, Pause, EasterEgg, AIInteraction, NarrationReplay, GameOver, ExitApp, EditPOIs }
        public enum GameLevelSubStates { Null = 0, InitialWelcome, WaitForPlayerClose, PlayAudio, PlayAnimation, Completed, GoToNextPOI, Idle }
        public enum GameAge { Kids = 0, Adults, Experts }

        public const string HighscoresDataKey = "HighscoresDataKey";

	    private static GameLevelData _instance;
        public static GameLevelData Instance
        {
            get { return _instance; }
        }

        [Tooltip("URL Base to download narrations")]
        [SerializeField] private string urlBase;
        [Tooltip("Total areas")]
		[SerializeField] private MapDataContent[] areasMuseum;
        [Tooltip("Name of the layer where the player is standing")]
		[SerializeField] private string layerFloorName = "Floor";
        [Tooltip("Name of the layer that will limit the game area")]
        [SerializeField] private string layerGameArea = "GameArea";
        [Tooltip("Name of the layer of the UI")]
        [SerializeField] private string layerUI = "UI";
        [Tooltip("Name of the layer of the replay POIs")]
        [SerializeField] private string layerReplay = "Replay";
        [Tooltip("Name of the layer of the video")]
        [SerializeField] private string layerVideo = "Video";        
        [Tooltip("Name of the layer of the secret")]
        [SerializeField] private string layerEasterEgg = "EasterEgg";
        [Tooltip("Speed of movement of the desktop client")]
		[SerializeField] private float playerDesktopSpeed = 50;
        [Tooltip("Speed of movement of the VR client")]
        [SerializeField] private float playerVRSpeed = 20;
        [Tooltip("Sensitivity of the rotation of the camera in desktop mode")]
        [SerializeField] private float sensitivityCamera = 7;
        [Tooltip("Distance to detect player is close to guide for next waypoint")]
        [SerializeField] private float distanceToTriggerGuide = 2;
        [Tooltip("Total Questions to ask")]
        [SerializeField] private int totalQuestions = 4;
        [SerializeField] private int totalAreas = 3;
        [SerializeField] private int totalAges = 3;
        [SerializeField] private GameAge age;
        [SerializeField] private string urlBaseManagement = "http://localhost:8080/template6dof/";
        [SerializeField] private int versionNumber = -1;
        [SerializeField] private int unlockSecretsIndex = -1;
        [SerializeField] private string versionAssets = "";
        [SerializeField] private string languagesTextAsset;
        [SerializeField] private TextAsset InitialNarration;
        

        public int VersionNumber
        {
            get { return versionNumber; }
            set { 
                versionNumber = value; 
                PlayerPrefs.SetString(VersionText, versionNumber.ToString());
            }
        }
        public int UnlockSecretsIndex
        {
            get { return unlockSecretsIndex; }
            set {  unlockSecretsIndex = value; }
        }        
        public string VersionAssets
        {
            get { return versionAssets; }
            set { 
                versionAssets = value; 
                _assetsBundle = DeserializeXml(versionAssets);
            }
        }

        private int _currentLevel = -1;
        private int _layerFloor;
        private int _layerUI;
        private int _layerGun;
        private int _layerReplay;
        private int _layerVideo;
        private int _layerEasterEgg;
		private GameLevelStates _gameLevelState = GameLevelStates.Initialization;
        private float _timerLevel = 0;
        private int _currentScore = 0;
        private int _currentTime = 0;

        private int _currentQuestion = 0;

		private int _nextAreaGame = -1;
		
		private float _totalTimeDone = 0;
		private bool _subtitlesActivated = true;
        private bool _enablePauseAccess = true;
        private int _developerMode = -1;
        private int _indexPOILevelEdited = -1;

        private bool _hasBeenEditionModified = false;

        private int _indexPOIListSelection = -1;

		private bool _editPOIsMode = true;
        private bool _changedLevelData = false;

        private Assets _assetsBundle;

        private AIConsumption _aiAdministrationConsumption;
        private AIConsumption  _aiCustomerConsumption;

        public Assets AssetsBundle
        {
            get { return _assetsBundle; }
        }
        public int CurrentLevel
        {
            get { return _currentLevel; }
            set { _currentLevel = value; }
        }
        public string URLBase
        {
            get { return urlBase; }
        }
		public float TotalTimeDone
		{
			get { return _totalTimeDone; }
			set { _totalTimeDone = value;}
		}
        public int NextAreaGame
        {
			get { return _nextAreaGame; }
			set { _nextAreaGame = value; }
		}
        public int LayerFloor
        {
            get { return _layerFloor; }
        }
        public int LayerUI
        {
            get { return _layerUI; }
        }
        public int LayerReplay
        {
            get { return _layerReplay; }
        }
        public int LayerVideo
        {
            get { return _layerVideo; }
        }
        public int LayerGun
        {
            get { return _layerGun; }
        }
        public string LayerGameArea
        {
            get { return layerGameArea; }
        }
		public int LayerEasterEgg
        {
			get { return _layerEasterEgg; }
        }
        public float PlayersDesktopSpeed
        {
            get { return playerDesktopSpeed; }
        }
        public float PlayerVRSpeed
        {
            get { return playerVRSpeed; }
        }
        public float SensitivityCamera
        {
            get { return sensitivityCamera; }
        }
        public GameLevelStates GameLevelState
        {
            get { return _gameLevelState; }
        }
        public float DistanceToTriggerGuide
        {
            get { return distanceToTriggerGuide; }
        }
        public float TimerLevel
        {
            get { return _timerLevel; }
        }
        public int CurrentScore
        {
            get { return _currentScore; }
            set { _currentScore = value; 
                SystemEventController.Instance.DispatchSystemEvent(EventGameLevelDataScoreUpdated, _currentScore);
            }
        }
        public int CurrentTime
        {
            get { return _currentTime; }
            set { _currentTime = value;  }
        }
        public int CurrentQuestion
        {
            get { return _currentQuestion; }
            set { _currentQuestion = value; }
        }
        public int TotalQuestions
        {
            get { return totalQuestions; }
        }
		public bool EnablePauseAccess
		{
			get { return _enablePauseAccess; }
			set { 
                _enablePauseAccess = value; 
				UIEventController.Instance.DispatchUIEvent(ScreenNarrationNextButtonView.EventScreenNarrationNextButtonViewPauseVisibility, _enablePauseAccess);
			}
		}
		public bool SubtitlesActivated
        {
			get { return _subtitlesActivated; }
			set { 
                _subtitlesActivated = value;			
				UIEventController.Instance.DispatchUIEvent(ScreenNarrationNextButtonView.EventScreenNarrationNextButtonSubtitlesChangedActivation);
			}
		}
        public GameAge Age
        {
            get { return age; }
            set { age = value; }
        }
        public int TotalAreas
        {
            get { return totalAreas; }
        }
        public string URLBaseManagement
        {
            get { return urlBaseManagement; }
        }
        public bool HasBeenEditionModified
        {
            get { return _hasBeenEditionModified; }
            set { _hasBeenEditionModified = value; }
        }
        public int IndexPOILevelEdited
        {
            get { return _indexPOILevelEdited; }
            set { _indexPOILevelEdited = value; }
        }        
		public bool EditPOIsMode
		{
			get { return _editPOIsMode; }
			set { 
				_editPOIsMode = value; 
                if (_changedLevelData)
                {
                    _changedLevelData = false;
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, "", LanguageController.Instance.GetText("message.please.wait"));
                    SystemEventController.Instance.DelaySystemEvent(EventGameLevelDataSaveAllData, 0.1f);
                }
				SystemEventController.Instance.DispatchSystemEvent(EventGameLevelDataEditModeChanged);
			}
		}
        public void SetTotalNarrations(int size)
        {
            PlayerPrefs.SetString(GameProgressTexts, "");
            areasMuseum = new MapDataContent[size];
        }
        public void SetTotalSizeNarration(int narration, int pois, int secrets)
        {
            areasMuseum[narration] = new MapDataContent(narration, new int[pois], new bool[secrets]);
        }
        public bool IsMuseumEmpty()
        {
            return ((areasMuseum == null) || (areasMuseum.Length == 0));
        }
        public bool IsMuseumEmpty(int area)
        {
            if (!IsMuseumEmpty())
            {
                return (areasMuseum[area] == null);
            }
            else
            {
                return true;
            }            
        }
        public int GetTotalSizeNarrations()
        {
            return areasMuseum.Length;
        }
        public int GetLevel(int area)
        {
            return ((int)age * totalAreas) + area;
        }
        public int GetLevel(GameAge customAge, int area)
        {
            return ((int)customAge * totalAreas) + area;
        }
        public TextAsset GetLevelNarration(int level)
        {
            return new TextAsset(areasMuseum[level].Narration);
        }
        public void SetLevelNarration(int level, string narrationData)
        {
            if (narrationData.Length > 0)
            {
                areasMuseum[level].Narration = narrationData;
            }            
        }
        public int GetLevelPOIsNumber(int level)
        {
            if ((areasMuseum[level].Positions != null) && (areasMuseum[level].Positions.Length > 0))
            {
                return areasMuseum[level].Positions.Length;
            }
            else
            {
                return 0;
            }
        }
        
        public POIPosition[] GetLevelPOIsPositions(int level)
        {
            return areasMuseum[level].Positions;
        }
        public SecretPosition[] GetLevelSecretsPositions(int level)
        {
            return areasMuseum[level].SecretsData;
        }
        public void SetPOIsPositions(int level, POIPosition[] positionsData)
        {
            if ((positionsData != null) && (positionsData.Length > 0))
            {
                areasMuseum[level].Positions = new POIPosition[positionsData.Length];
                for (int i = 0; i < positionsData.Length; i++)
                {
                    areasMuseum[level].Positions[i] = new POIPosition(positionsData[i].ID, positionsData[i].Position);
                }
            }
        }
        public void SetSecretsPositions(int level, SecretPosition[] secretsData)
        {
            if ((secretsData != null) && (secretsData.Length > 0))
            {
                areasMuseum[level].SecretsData = new SecretPosition[secretsData.Length];
                for (int i = 0; i < secretsData.Length; i++)
                {
                    areasMuseum[level].SecretsData[i] = new SecretPosition(secretsData[i].ID, secretsData[i].Position, secretsData[i].CustomEvent, secretsData[i].Narration);
                }
            }
        }
        public int LengthUnlockedEasterEggs(int area)
        {
            if (GetLevel(area) < areasMuseum.Length)
            {
                return areasMuseum[GetLevel(area)].Secrets.Length;
            }
            else
            {
                return -1;
            }            
        }

  		private string PackPOIsContent(POIPosition[] data)
        {
            SerializedPOIPosition serializedPOIs = new SerializedPOIPosition();
            serializedPOIs.Positions = new POIPosition[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                serializedPOIs.Positions[i] = new POIPosition(i, data[i].Position);
                Debug.LogError("PackPOIsContent: " + i + " - " + serializedPOIs.Positions[i].Position);
            }

            string jsonData = JsonUtility.ToJson(serializedPOIs, true);
            return jsonData;
        }

  		private string PackSecretsContent(SecretPosition[] data)
        {
            SerializedSecretPosition serializedPOIs = new SerializedSecretPosition();
            serializedPOIs.Secrets = new SecretPosition[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                serializedPOIs.Secrets[i] = new SecretPosition(i, data[i].Position, data[i].CustomEvent, data[i].Narration);
            }

            string jsonData = JsonUtility.ToJson(serializedPOIs, true);
            return jsonData;
        }

		public POIPosition[] UnPackPOIStringData(string jsonData)
		{	
            if ((jsonData == null) || (jsonData.Length == 0))
            {
                return null;
            }
            else
            {
                SerializedPOIPosition serializedPOIs = JsonUtility.FromJson<SerializedPOIPosition>(jsonData);
                POIPosition[] poisPositions = new POIPosition[serializedPOIs.Positions.Length];
                for (int i = 0; i < serializedPOIs.Positions.Length; i++)
                {
                    poisPositions[i] = new POIPosition(i, serializedPOIs.Positions[i].Position);
                }

                return poisPositions;
            }
		}

		public SecretPosition[] UnPackSecretStringData(string jsonData)
		{	
            if ((jsonData == null) || (jsonData.Length == 0))
            {
                return null;
            }
            else
            {
                SerializedSecretPosition serializedSecrets = JsonUtility.FromJson<SerializedSecretPosition>(jsonData);
                SecretPosition[] secretsPositions = new SecretPosition[serializedSecrets.Secrets.Length];
                for (int i = 0; i < serializedSecrets.Secrets.Length; i++)
                {
                    secretsPositions[i] = new SecretPosition(i, serializedSecrets.Secrets[i].Position, serializedSecrets.Secrets[i].CustomEvent, serializedSecrets.Secrets[i].Narration);
                }

                return secretsPositions;
            }
		}

        public void Initialize()
        {
            _instance = this;
            _layerFloor = LayerMask.GetMask(layerFloorName);
            _layerUI = LayerMask.GetMask(layerUI);
            _layerReplay = LayerMask.GetMask(layerReplay);
            _layerVideo = LayerMask.GetMask(layerVideo);
			_layerEasterEgg = LayerMask.GetMask(layerEasterEgg);
            _indexPOIListSelection = -1;

            _aiAdministrationConsumption = new AIConsumption(yourvrexperience.Utils.Utilities.GetTimestampSeconds(), 0);
            _aiCustomerConsumption = new AIConsumption(yourvrexperience.Utils.Utilities.GetTimestampSeconds(), 0);

            string dataGameProgress = PlayerPrefs.GetString(GameProgressTexts, "");
            if ((dataGameProgress != null) && (dataGameProgress.Length > 0)) UnPackMapDataContent(dataGameProgress);

            string stringVersion = PlayerPrefs.GetString(VersionText, "");
            if ((stringVersion != null) && (stringVersion.Length > 0)) versionNumber = int.Parse(stringVersion);

            string stringAssets =  PlayerPrefs.GetString(VersionAssetsText, "");
            if ((stringAssets != null) && (stringAssets.Length > 0))
            {
                versionAssets = stringAssets;
                _assetsBundle = DeserializeXml(versionAssets);
            } 

            SystemEventController.Instance.Event += OnSystemEvent;
        }

        public string GetInitialNarration()
        {
            return InitialNarration.text;
        }
        
        private Assets DeserializeXml(string xml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Assets));
            
            using (StringReader reader = new StringReader(xml))
            {
                return (Assets)serializer.Deserialize(reader);
            }
        }

        public List<Asset> GetAssetsByType(TypeObjectNarration typeAsset)
        {
            List<Asset> output = new List<Asset>();
            foreach (Asset asset in _assetsBundle.AssetList)
            {
                if ((typeAsset == TypeObjectNarration.Image) && (asset.Type.Equals("image")))
                {
                    output.Add(asset);
                }
                if ((typeAsset == TypeObjectNarration.Video) && (asset.Type.Equals("video")))
                {
                    output.Add(asset);
                }
                if ((typeAsset == TypeObjectNarration.Model3D) && (asset.Type.Equals("model3d")))
                {
                    output.Add(asset);
                }                
                if ((typeAsset == TypeObjectNarration.Sound) && (asset.Type.Equals("sound")))
                {
                    output.Add(asset);
                }                                
                if ((typeAsset == TypeObjectNarration.Interaction) && (asset.Type.Equals("interactable")))
                {
                    output.Add(asset);
                }          
                if ((typeAsset == TypeObjectNarration.Waypoints) && (asset.Type.Equals("waypoints")))
                {
                    output.Add(asset);
                }                                                
            } 
            return output;
        }

        public string GetNameByAsset(string assetName)
        {
            foreach (Asset asset in _assetsBundle.AssetList)
            {
                if (asset.Value.Equals(assetName))
                {
                    return asset.Name;
                }
            } 
            return "";
        }

        public string[] GetAnimationsByAsset(string assetName)
        {            
            foreach (Asset asset in _assetsBundle.AssetList)
            {
                if (asset.Value.Equals(assetName))
                {
                    if ((asset.Animations == null) || (asset.Animations.Length == 0))
                    {
                        return new string[0];
                    }
                    return asset.Animations.ToString().Split(',');
                }
            } 
            return new string[0];
        }

        public bool GetDeveloperMode()
        {
            if (_developerMode == -1)
            {
                _developerMode = PlayerPrefs.GetInt(DeveloperMode, 0);
            }
            return (_developerMode==1?true:false);
        }

        public void SetDeveloperMode(bool dev)
        {
            _developerMode = (dev?1:0);
            PlayerPrefs.SetInt(DeveloperMode, _developerMode);
            VersionNumber = -1;
            PlayerPrefs.SetString(VersionText, versionNumber.ToString());
        }

        public void ResetGameLevelData()
        {
		    _gameLevelState = GameLevelStates.Initialization;
            _timerLevel = 0;
        }

        public void SaveGameLevelState(GameLevelStates gameLevelState, float timerLevel)
        {
		    _gameLevelState = gameLevelState;
            _timerLevel = timerLevel;
        }

#if ENABLE_INPUT_FORM
        public void InsertFormHTTP(List<QuestionForm> questions)
        {            
            SerializedQuestionsForm serializedQuestions = new SerializedQuestionsForm();
            serializedQuestions.Questions = questions.ToArray();
            string jsonData = JsonUtility.ToJson(serializedQuestions, true);

            UIEventController.Instance.DelayUIEvent(UsersController.EVENT_USER_INPUT_FORM_REQUEST, 0.2f, jsonData);
        }
#endif        

        public void SetUnlockEasterEgg(int area, int easterEgg)
        {
            areasMuseum[GetLevel(area)].Secrets[easterEgg] = true;
#if ENABLE_ANALYTICS
            TourAnalyticsController.Instance.LogEasterEggUnlockedEvent(age, area, easterEgg);
#endif            
            SaveGameProgressLocally();
        }

        public bool GetUnlockedEasterEgg(int area, int easterEgg)
        {
            if ((areasMuseum[GetLevel(area)].Secrets == null) ||  (easterEgg < 0) || (areasMuseum[GetLevel(area)].Secrets.Length <= easterEgg))
            {
                return false;
            }
            return areasMuseum[GetLevel(area)].Secrets[easterEgg];
        }

        public void SetUnlockPOI(int area, int poiIndex, int timePOI)
        {
            int finalIndex = poiIndex - 1;
            if (finalIndex < areasMuseum[GetLevel(area)].POIs.Length)
            {
                areasMuseum[GetLevel(area)].POIs[finalIndex] = timePOI;                
            }
            SaveGameProgressLocally();
        }

        public int GetTotalProgress(int area)
        {
            int finalLevel = GetLevel(area);
            int[] progressMap = areasMuseum[finalLevel].POIs;
            bool[] unlockedEasterEggs = areasMuseum[finalLevel].Secrets;
            if ((progressMap != null) && (unlockedEasterEggs != null))
            {
                int totalTokens = progressMap.Length + unlockedEasterEggs.Length;
                int tokens = 0;
                for (int i = 0; i < progressMap.Length; i++)   
                {
                    if (progressMap[i] > 0)
                    {
                        tokens++;
                    }
                }
                for (int i = 0; i < unlockedEasterEggs.Length; i++)   
                {
                    if (unlockedEasterEggs[i])
                    {
                        tokens++;
                    }
                }    
                if ((tokens == 0) || (totalTokens == 0))
                {
                    return 0;
                }
                else
                {
                    return (int)(((float)tokens/(float)totalTokens) * 100);
                }                
            }
            else
            {
                return 0;
            }
        }

        public void ResetLocalData()
        {
            foreach (MapDataContent item in areasMuseum)
            {
                item.Reset();
            }            
        }

        public void SaveGameProgressLocally()
        {
            string progressDone = PackMapDataContent();
            PlayerPrefs.SetString(GameProgressTexts, progressDone);
            string adminConsumptionPacket = PackAIConsumption(_aiAdministrationConsumption);
            string consumerConsumptionPacket = PackAIConsumption(_aiCustomerConsumption);
#if ENABLE_INPUT_FORM
            if (UsersController.Instance.CurrentUser != null)
            {
                if (!UsersController.Instance.CurrentUser.IsEmptyUser())
                {
                    UIEventController.Instance.DelayUIEvent(UsersController.EVENT_USER_UPDATE_PROFILE_REQUEST, 0.2f, UsersController.Instance.CurrentUser.Id.ToString(), UsersController.Instance.CurrentUser.Nickname, UsersController.Instance.CurrentUser.Email, "", progressDone, adminConsumptionPacket, consumerConsumptionPacket, "", "");
                }                
            }            
#endif            
        }

        public bool AllowAIAdminOperation(int requests)
        {
            long currentTimestamp = yourvrexperience.Utils.Utilities.GetTimestampSeconds();
            long timeDifference = currentTimestamp - _aiAdministrationConsumption.Timestamp;
            if (timeDifference > TOTAL_SECONDS_DAY)
            {
                _aiAdministrationConsumption.TotalAIRequests = 0;
                _aiAdministrationConsumption.Timestamp = currentTimestamp;
            }
            if (_aiAdministrationConsumption.TotalAIRequests < TOTAL_ADMIN_OPERATIONS)
            {
                _aiAdministrationConsumption.TotalAIRequests += requests;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool AllowAIConsumerOperation(int requests)
        {
            long currentTimestamp = yourvrexperience.Utils.Utilities.GetTimestampSeconds();
            long timeDifference = currentTimestamp - _aiCustomerConsumption.Timestamp;
            if (timeDifference > TOTAL_SECONDS_DAY)
            {
                _aiCustomerConsumption.TotalAIRequests = 0;
                _aiCustomerConsumption.Timestamp = currentTimestamp;
            }
            if (_aiCustomerConsumption.TotalAIRequests < TOTAL_CUSTOMER_OPERATIONS)
            {
                _aiCustomerConsumption.TotalAIRequests += requests;
                return true;
            }
            else
            {
                return false;
            }
        }

        public string PackAIConsumption(AIConsumption aiConsumption)
        {
            string jsonData = JsonUtility.ToJson(aiConsumption, true);
            return jsonData;
        }

        public void UnpackAdminConsumption(string jsonData)
        {
            if ((jsonData != null) && (jsonData.Length > 0))
            {
                _aiAdministrationConsumption = UnpackAIConsumption(jsonData);
            }
        }

        public void UnpackConsumerConsumption(string jsonData)
        {
            if ((jsonData != null) && (jsonData.Length > 0))
            {
                _aiCustomerConsumption = UnpackAIConsumption(jsonData);
            }
        }

        public AIConsumption UnpackAIConsumption(string jsonData)
        {
            AIConsumption aiConsumption = JsonUtility.FromJson<AIConsumption>(jsonData);
            if (aiConsumption != null)
            {
                if (aiConsumption.Timestamp > 0)
                {
                    long currentTimestamp = yourvrexperience.Utils.Utilities.GetTimestampSeconds();
                    long timeDifference = currentTimestamp - aiConsumption.Timestamp;
                    if (timeDifference > TOTAL_SECONDS_DAY)
                    {                        
                        aiConsumption.TotalAIRequests = 0;
                        aiConsumption.Timestamp = currentTimestamp;
                    }
                }
            }
            return aiConsumption;
        }


        public string PackMapDataContent()
        {
            SerializedMapDataContent serializedMaps = new SerializedMapDataContent();
            serializedMaps.Maps = new MapDataContent[areasMuseum.Length];
            for (int i = 0; i < areasMuseum.Length; i++)
            {
                serializedMaps.Maps[i] = new MapDataContent(i, areasMuseum[i].POIs, areasMuseum[i].Secrets);
            }

            string jsonData = JsonUtility.ToJson(serializedMaps, true);
            return jsonData;
        }

        public void UnPackMapDataContent(string jsonData)
        {
            SerializedMapDataContent serializedMaps = JsonUtility.FromJson<SerializedMapDataContent>(jsonData);
            areasMuseum = new MapDataContent[serializedMaps.Maps.Length];
			for (int i = 0; i < serializedMaps.Maps.Length; i++)
			{
                areasMuseum[i] = new MapDataContent(i, serializedMaps.Maps[i].POIs, serializedMaps.Maps[i].Secrets);
			}
        }

        public string GetLanguagesTextAsset()
        {
		    return languagesTextAsset;
        }

        public void SaveLanguagesTextAsset(string textLanguages)
        {
		    languagesTextAsset = textLanguages;
        }

        public string GetNarrationTextAsset(int index)
        {
		    return null;
        }

        public void SaveNarrationTextAsset(int index, string textNarration)
        {
        }

        public void UpdatePOIsPosition(int level, string positions)
        {           
            for (int j = 0; j < totalAges; j++)
            {
                int idFinalLevel = GetLevel((GameAge)j, level);
                POIPosition[] poiLevelData = UnPackPOIStringData(positions);
                SetPOIsPositions(idFinalLevel, poiLevelData);
            }
        }

        public void UpdateSecretsPosition(int level, string secrets)
        {           
            for (int j = 0; j < totalAges; j++)
            {
                int idFinalLevel = GetLevel((GameAge)j, level);
                SecretPosition[] easterEggLevelData = UnPackSecretStringData(secrets);
                SetSecretsPositions(idFinalLevel, easterEggLevelData);
            }
        }

        public void InsertPOIs(int iduser, string passworduser, int id, int age, int level, string positions, string secrets, string narration, bool shouldUpdateDatabase)
        {
            int idFinalLevel = id;
            POIPosition[] poiLevelData = UnPackPOIStringData(positions);
            SecretPosition[] easterEggLevelData = UnPackSecretStringData(secrets);
            SetPOIsPositions(idFinalLevel, poiLevelData);
            SetSecretsPositions(idFinalLevel, easterEggLevelData);
            if (shouldUpdateDatabase)
            {
                CommController.Instance.Request(EventCommInsertPOIsHTTP, false, iduser, passworduser, idFinalLevel, age, level, true, positions, secrets, narration);
            }                
        }

        public void InsertInitialPOIs(int iduser, string passworduser, int id, int age, int level, string positions, string secrets, string narration)
        {
            CommController.Instance.Request(EventCommInsertPOIsHTTP, false, iduser, passworduser, id, age, level, false, positions, secrets, narration);
        }

        public void ConsultPOIs(int id, int age, bool dev)
        {
            CommController.Instance.Request(EventCommConsultPOIsHTTP, true, id, age, dev);
        }

        public void GetVersion()
        {            
            CommController.Instance.Request(EventCommGetVersionHTTP, false);
        }

        public void SetVersion(int iduser, string passworduser, int version, int secrets)
        {            
            CommController.Instance.Request(EventCommSetVersionHTTP, false, iduser, passworduser, version, secrets);
        }

        public void StoreSpeech(int iduser, string passworduser, string customEvent, int secret, string text, int age, int floor, int poi, int segment, string language, byte[] data)
        {            
            CommController.Instance.Request(EventCommStoreSpeechHTTP, false, iduser, passworduser, customEvent, secret, text, age, floor, poi, segment, language, data);
        }

        public void DownloadSpeech(string nameEvent, int id, int secret, int age, int floor, int poi, int segment, string language)
        {   
            int dev = GetDeveloperMode()?1:0;
            long timestamp = yourvrexperience.Utils.Utilities.GetTimestamp();
            SoundsController.Instance.DownloadAudioFile(nameEvent, id, ".ogg", URLBaseManagement + "MuseumDownloadSpeech.php?secret=" + secret + "&age=" + age + "&floor=" + floor + "&poi=" + poi + "&segment=" + segment + "&language=" + language + "&direct=" + 0 + "&dev=" + dev + "&time=" + timestamp, true);
        }

        public void DeleteSpeech(int iduser, string passworduser, string customEvent, bool all, int secret, int age, int floor, int poi, int segment)
        {
            CommController.Instance.Request(EventCommDeleteSpeechHTTP, false, iduser, passworduser, customEvent, all, secret, age, floor, poi, segment);
        }

        public void ReorderPOISSpeeches(string customEvent, int age, int floor, int poi, bool addOperation)
        {
            CommController.Instance.Request(EventCommReorderPOISpeechesHTTP, false, customEvent, age, floor, poi, addOperation);
        }

        public void ReorderSecretsSpeeches(string customEvent, int secret, int age, int floor, bool addOperation)
        {
            CommController.Instance.Request(EventCommReorderSecretsSpeechesHTTP, false, customEvent, secret, age, floor, addOperation);
        }

        private int GetModIndex(int index, int length)
        {
            if (index < 0)
            {
                return length - 1;
            }
            else
            {
                return index % length;
            }
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(InsertPOIsHTTP.EventInsertPOIsHTTPCompleted))
            {
                SystemEventController.Instance.DispatchSystemEvent(EventGameLevelDataCompletedUpdate);
            }
            if (nameEvent.Equals(EventGameLevelDataSelectedIndexPOI))
            {
                _indexPOIListSelection = (int)parameters[0];
                if (_indexPOIListSelection != -1)
                {
                    if (_editPOIsMode)
                    {
                        int poiDataSelectionIndex = (int)parameters[1];
                        if (poiDataSelectionIndex != _indexPOIListSelection)
                        {
                            Debug.LogError("The index["+_indexPOIListSelection+"] doesn't match with the data["+poiDataSelectionIndex+"]");
                        }
                    }
                    else
                    {
                        int secretDataSelectionIndex = (int)parameters[1];
                        if (secretDataSelectionIndex != _indexPOIListSelection)
                        {
                            Debug.LogError("The index["+_indexPOIListSelection+"] doesn't match with the data["+secretDataSelectionIndex+"]");
                        }
                    }
                }
            }
            if (nameEvent.Equals(EventGameLevelDataAddNewPOI))
            {
                _changedLevelData = true;
                if (_editPOIsMode)
                {
                    // ADD/INSERT POI POSITION
                    int currentTotal = 0;
                    bool isLastElement = false;
                    List<POIPosition> dataBackup = new List<POIPosition>();
                    if ((areasMuseum[_currentLevel].Positions != null) && (areasMuseum[_currentLevel].Positions.Length > 0))
                    {
                        currentTotal = areasMuseum[_currentLevel].Positions.Length;
                        if (currentTotal > MAXIMUM_NUMBER_POIS)
                        {
                            ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, LanguageController.Instance.GetText("text.error"), LanguageController.Instance.GetText("message.max.pois.reached"));
                            return;
                        }
                        for (int i = 0; i < currentTotal; i++)
                        {
                            dataBackup.Add(new POIPosition(areasMuseum[_currentLevel].Positions[i]));
                        }                        
                        if (_indexPOIListSelection != -1)
                        {
                            int nextIndex = (_indexPOIListSelection + 1) % dataBackup.Count;
                            Vector3 newPositionPOI = (Vector3)parameters[0];
                            dataBackup.Insert(_indexPOIListSelection + 1, new POIPosition(dataBackup.Count, newPositionPOI));
                        }
                        else
                        {
                            Vector3 newPositionPOI = (Vector3)parameters[0];
                            dataBackup.Add(new POIPosition(dataBackup.Count, newPositionPOI));
                            _indexPOIListSelection = currentTotal;
                            isLastElement = true;
                        }
                    }
                    else
                    {
                        Vector3 newPositionPOI = (Vector3)parameters[0];
                        dataBackup.Add(new POIPosition(0, newPositionPOI));
                        _indexPOIListSelection = -1;
                        isLastElement = true;
                    }
                    areasMuseum[_currentLevel].Positions = dataBackup.ToArray();
                
                    // ADD NARRATION
                    NarrationCreator narrationCreator = new NarrationCreator();
                    narrationCreator.AddNewPOINarration(_indexPOIListSelection + 1, GetLevelNarration(_currentLevel).text);
                    SetLevelNarration(_currentLevel, narrationCreator.ToXML());

                    // UPDATE SYSTEM
                    int poi = _indexPOIListSelection;
                    _indexPOIListSelection = -1;
                    SystemEventController.Instance.DispatchSystemEvent(EventGameLevelDataRefreshPOILevel);
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, "", LanguageController.Instance.GetText("message.please.wait"));
                    if (!isLastElement)
                    {
                        SystemEventController.Instance.DelaySystemEvent(EventGameLevelDataReorderAndSavePOIs, 0.2f, -1, (int)Age, _currentLevel, poi, true);
                    }
                    else
                    {
                        SystemEventController.Instance.DelaySystemEvent(EventGameLevelDataSaveAllData, 0.2f);
                    }
                }
                else
                {
                    // ADD/INSERT SECRET POSITION                    
                    bool isLastElement = false;
                    List<SecretPosition> dataSecretBackup = new List<SecretPosition>();
                    if ((areasMuseum[_currentLevel].SecretsData != null) && (areasMuseum[_currentLevel].SecretsData.Length > 0))
                    {
                        int currentTotalSecrets = areasMuseum[_currentLevel].SecretsData.Length;
                        if (currentTotalSecrets > MAXIMUM_NUMBER_SECRETS)
                        {
                            ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, LanguageController.Instance.GetText("text.error"), LanguageController.Instance.GetText("message.max.secrets.reached"));
                            return;
                        }                        
                        for (int i = 0; i < currentTotalSecrets; i++)
                        {
                            dataSecretBackup.Add(new SecretPosition(areasMuseum[_currentLevel].SecretsData[i]));
                        }                        
                        if (_indexPOIListSelection != -1)
                        {
                            Vector3 newPositionPOI = (Vector3)parameters[0];
                            dataSecretBackup.Insert(_indexPOIListSelection + 1, new SecretPosition(dataSecretBackup.Count, newPositionPOI, "", HexadecimalEncoding.ToHexString(InitialNarration.text)));
                        }
                        else
                        {
                            Vector3 newPositionPOI = (Vector3)parameters[0];
                            dataSecretBackup.Add(new SecretPosition(dataSecretBackup.Count, newPositionPOI, "", HexadecimalEncoding.ToHexString(InitialNarration.text)));
                            _indexPOIListSelection = currentTotalSecrets;
                            isLastElement = true;
                        }
                    }
                    else
                    {
                        Vector3 newPositionPOI = (Vector3)parameters[0];
                        dataSecretBackup.Add(new SecretPosition(0, newPositionPOI, "", HexadecimalEncoding.ToHexString(InitialNarration.text)));
                        _indexPOIListSelection = -1;
                        isLastElement = true;                        
                    }
                    areasMuseum[_currentLevel].SecretsData = dataSecretBackup.ToArray();
                
                    // REORDER THE SPEECHES IF THERE HAS BEEN AN INSERTION
                    int secret = _indexPOIListSelection;
                    _indexPOIListSelection = -1;
                    SystemEventController.Instance.DispatchSystemEvent(EventGameLevelDataRefreshPOILevel);
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, "", LanguageController.Instance.GetText("message.please.wait"));
                    if (!isLastElement)
                    {
                        SystemEventController.Instance.DelaySystemEvent(EventGameLevelDataReorderAndSavePOIs, 0.2f, secret, (int)Age, _currentLevel, -1, true);
                    }
                    else
                    {
                        SystemEventController.Instance.DelaySystemEvent(EventGameLevelDataSaveAllData, 0.2f);
                    }                    
                }
            }
            if (nameEvent.Equals(EventGameLevelDataRemovePOI))
            {
                if (_indexPOIListSelection != -1)
                {
                    _changedLevelData = true;
                    int idUser = (int)parameters[0];
                    string passwordUser = (string)parameters[1];
                    if (_editPOIsMode)
                    {
                        // REMOVE POI POSITION
                        int currentTotal = areasMuseum[_currentLevel].Positions.Length;
                        List<POIPosition> dataBackup = new List<POIPosition>();
                        for (int i = 0; i < currentTotal; i++)
                        {
                            dataBackup.Add(new POIPosition(areasMuseum[_currentLevel].Positions[i]));
                        }
                        dataBackup.RemoveAt(_indexPOIListSelection);
                        areasMuseum[_currentLevel].Positions = dataBackup.ToArray();

                        // REMOVE NARRATION
                        NarrationCreator narrationCreator = new NarrationCreator();
                        narrationCreator.RemovePOINarration(_indexPOIListSelection, GetLevelNarration(_currentLevel).text);
                        SetLevelNarration(_currentLevel, narrationCreator.ToXML());
                        
                        // DELETE SPEECHES LINKED TO DELETED POI 
                        int poi = _indexPOIListSelection;
                        _indexPOIListSelection = -1;     
                        SystemEventController.Instance.DispatchSystemEvent(EventGameLevelDataRefreshPOILevel);
                        ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, "", LanguageController.Instance.GetText("message.please.wait"));
                        SystemEventController.Instance.DelaySystemEvent(EventGameLevelDataDeleteAndReorderSpeeches, 0.2f, idUser, passwordUser, false, -1, (int)Age, _currentLevel, poi, -1);
                    }
                    else
                    {
                        // REMOVE SECRET POSITION                        
                        int currentTotalSecrets = areasMuseum[_currentLevel].SecretsData.Length;
                        List<SecretPosition> dataSecretBackup = new List<SecretPosition>();
                        for (int i = 0; i < currentTotalSecrets; i++)
                        {
                            dataSecretBackup.Add(new SecretPosition(areasMuseum[_currentLevel].SecretsData[i]));
                        }
                        dataSecretBackup.RemoveAt(_indexPOIListSelection);
                        areasMuseum[_currentLevel].SecretsData = dataSecretBackup.ToArray();
                        
                        // DELETE SPEECHES LINKED TO DELETED SECRET 
                        int secret = _indexPOIListSelection;
                        _indexPOIListSelection = -1;
                        SystemEventController.Instance.DispatchSystemEvent(EventGameLevelDataRefreshPOILevel);
                        ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, "", LanguageController.Instance.GetText("message.please.wait"));
                        SystemEventController.Instance.DelaySystemEvent(EventGameLevelDataDeleteAndReorderSpeeches, 0.2f, idUser, passwordUser, false, secret, (int)Age, _currentLevel, -1, -1);
                    }
                }
            }
            if (nameEvent.Equals(GameLevelData.EventGameLevelDataClearAll))
            {
                _changedLevelData = true;
                int idUser = (int)parameters[0];
                string passwordUser = (string)parameters[1];                
                if (_editPOIsMode)
                {
                    List<POIPosition> dataBackup = new List<POIPosition>();
                    areasMuseum[_currentLevel].Positions = dataBackup.ToArray();

                    // REMOVE ALL NARRATIONS
                    NarrationCreator narrationCreator = new NarrationCreator();
                    narrationCreator.RemoveAllPOINarration();
                    SetLevelNarration(_currentLevel, narrationCreator.ToXML());
                    
                    // DELETE ALL SPEECHES
                    SystemEventController.Instance.DispatchSystemEvent(EventGameLevelDataRefreshPOILevel);
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, "", LanguageController.Instance.GetText("message.please.wait"));
                    SystemEventController.Instance.DelaySystemEvent(EventGameLevelDataDeleteAndReorderSpeeches, 0.2f, idUser, passwordUser, true, -1, (int)Age, _currentLevel, -1, -1);
                }
                else
                {
                    // REMOVE ALL SECRETS
                    List<SecretPosition> dataSecretBackup = new List<SecretPosition>();
                    areasMuseum[_currentLevel].SecretsData = dataSecretBackup.ToArray();
                    
                    // DELETE ALL SPEECHES
                    SystemEventController.Instance.DispatchSystemEvent(EventGameLevelDataRefreshPOILevel);
                    ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, null, "", LanguageController.Instance.GetText("message.please.wait"));
                    SystemEventController.Instance.DelaySystemEvent(EventGameLevelDataDeleteAndReorderSpeeches, 0.2f, idUser, passwordUser, true, 1, (int)Age, _currentLevel, -1, -1);
                }
            }
            if (nameEvent.Equals(EventGameLevelDataDeleteAndReorderSpeeches))
            {
                int idUser = (int)parameters[0];
                string passwordUser = (string)parameters[1];
                bool all = (bool)parameters[2];
                int secret = (int)parameters[3];
                int age = (int)parameters[4];
                int floor = (int)parameters[5];
                int poi = (int)parameters[6];
                int segment = (int)parameters[7];

                GameLevelData.Instance.DeleteSpeech(idUser, passwordUser, EventGameLevelDataSpeechesDeleted, all, secret, age, floor, poi, segment);
            }
            if (nameEvent.Equals(EventGameLevelDataSpeechesDeleted))
            {
                if ((bool)parameters[0])
                {
                    bool all = (bool)parameters[1];
                    int secret = (int)parameters[2];
                    int age = (int)parameters[3];
                    int floor = (int)parameters[4];
                    int poi = (int)parameters[5]; 
                    int segment = (int)parameters[6];

                    if (all)
                    {
                        SystemEventController.Instance.DispatchSystemEvent(EventGameLevelDataSaveAllData);
                    }
                    else
                    {
                        if (secret == -1)
                        {
                            GameLevelData.Instance.ReorderPOISSpeeches(EventGameLevelDataReorderedPOIs, age, floor, poi, false);
                        }
                        else
                        {
                            GameLevelData.Instance.ReorderSecretsSpeeches(EventGameLevelDataReorderedPOIs, secret, age, floor, false);
                        }
                    }
                }                
            }
            if (nameEvent.Equals(EventGameLevelDataReorderAndSavePOIs))
            {
                int secret = (int)parameters[0];
                int age = (int)parameters[1];
                int floor = (int)parameters[2];
                int poi = (int)parameters[3];
                bool addOperation = (bool)parameters[4];

                if (secret == -1)
                {
                    GameLevelData.Instance.ReorderPOISSpeeches(EventGameLevelDataReorderedPOIs, age, floor, poi, addOperation);
                }
                else
                {
                    GameLevelData.Instance.ReorderSecretsSpeeches(EventGameLevelDataReorderedPOIs, secret, age, floor, addOperation);
                }                
            }
            if (nameEvent.Equals(EventGameLevelDataReorderedPOIs))
            {
                SystemEventController.Instance.DispatchSystemEvent(EventGameLevelDataSaveAllData);
            }
        }
    }
}
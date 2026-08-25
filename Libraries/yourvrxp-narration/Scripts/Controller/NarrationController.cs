using yourvrexperience.Utils;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Xml;
using static yourvrexperience.Utils.SoundsController;
#if ENABLE_SPEECH
using yourvrexperience.speech;
using Unity.VisualScripting;
using UnityEngine.Rendering;
#endif
#if ENABLE_NETWORKING
using yourvrexperience.Networking;
#endif
using System.Globalization;

namespace yourvrexperience.Narration
{
    public class NarrationController : MonoBehaviour
    {
		public const bool ENABLE_AUTOMATIC_TRANSITION = false;

        [System.Serializable]
        public class SerializedNarrationObjects
        {
            public NarrationObject[] NarrationObjects;
        }

		[Serializable]
		public class NarrationObject
		{
			public string AssetName;
			public Vector3 Position;
			public Quaternion Rotation;
			public Vector3 Scale;
			public string Animation;
			public TypeObjectNarration Type;

			public NarrationObject(XmlNode data)
			{
				AssetName = ((data.Attributes["name"] == null)?"":data.Attributes["name"].Value);
				Animation = ((data.Attributes["animation"] == null)?"":data.Attributes["animation"].Value);
				StringToPosition(((data.Attributes["position"]==null)?"":data.Attributes["position"].Value));
				StringToRotation(((data.Attributes["rotation"]==null)?"":data.Attributes["rotation"].Value));
				StringToScale(((data.Attributes["scale"]==null)?"":data.Attributes["scale"].Value));
				string stype = ((data.Attributes["type"] == null)?"Image":data.Attributes["type"].Value);
				Enum.TryParse<TypeObjectNarration>(stype, out Type);
			}

			public NarrationObject(string assetName, Vector3 position, Quaternion rotation, Vector3 scale, TypeObjectNarration type, string animation)
			{
				AssetName = assetName;
				Position = position;
				Rotation = rotation;
				Scale = scale;
				Animation = animation;
				Type = type;
			}

			private string PositionToString()
			{
				return Position.x + "," + Position.y + "," + Position.z;
			}

			private string ScaleToString()
			{
				return Scale.x + "," + Scale.y + "," + Scale.z;
			}

			private void StringToPosition(string data)
			{
				string[] digits = data.Split(',');
				if (digits.Length == 3) Position = new Vector3(float.Parse(digits[0]), float.Parse(digits[1]), float.Parse(digits[2]));
			}

			private void StringToScale(string data)
			{
				string[] digits = data.Split(',');
				if (digits.Length == 3) Scale = new Vector3(float.Parse(digits[0]), float.Parse(digits[1]), float.Parse(digits[2]));
				if (Scale.Equals(Vector3.zero))
				{
					Scale = Vector3.one;
				}
			}

			private string RotationToString()
			{
				return Rotation.x + "," + Rotation.y + "," + Rotation.z + "," + Rotation.w;
			}

			private void StringToRotation(string data)
			{
				string[] digits = data.Split(',');
				if (digits.Length == 4) Rotation = new Quaternion(float.Parse(digits[0]), float.Parse(digits[1]), float.Parse(digits[2]), float.Parse(digits[3]));
			}

			public string ToXML()
			{
				string output = "\n";
				output += "<asset name = \""+AssetName+"\" position = \""+PositionToString()+"\" rotation = \""+RotationToString()+"\" scale = \""+ScaleToString()+"\" type = \""+Type+"\" animation = \""+Animation+"\"/>";
				return output;
			}			

			public string[] GetAssetNames()
			{
				return AssetName.Split(',');
			}
		}

		[Serializable]
		public class NarrationToken
		{
			public const float AVERAGE_TIME_FOR_CHARACTER = 0.07f;
			public const string POI_EMPTY = "POI_EMPTY";

			public const string EventNarrationTokenStart = "EventNarrationTokenStart";
			public const string EventNarrationTokenEnd = "EventNarrationTokenEnd";
			public const string EventNarrationTokenCreateNarrationObject = "EventNarrationTokenCreateNarrationObject";
			public const string EventNarrationTokenDestroyNarrationObject = "EventNarrationTokenDestroyNarrationObject";
			public const string EventNarrationTokenSetVisibilityTourGuide = "EventNarrationTokenSetVisibilityTourGuide";
			public const string NarrationTokenViewUpdateText = "NarrationTokenViewUpdateText";			

			public int Id;
			public float Time;
			public string StartEvent;
			public string EndEvent;
			public string NameClip;
			private TextEntry Message;
			private TextEntry Audios;
			public List<string> DisplayUnits;			
			public int CurrentUnit = -1;
			public AudioClip Audio;
			public float StartTime = -1;
			public float WordTime = -1;
			public bool ShouldHideGuide = false;
			public bool ShouldDestroy = false;
			public bool ShouldPause = false;
			public bool ShouldPauseOriginal = false;
			
			public List<NarrationObject> Assets = new List<NarrationObject>();

			private Dictionary<string,List<string>> _displayUnits = new Dictionary<string, List<string>>();
			private Dictionary<string,string> _senteces = new Dictionary<string, string>();
			private Dictionary<string,string> _audios = new Dictionary<string, string>();

			private XmlNode _data;
			private NarrationController _narrationController;
			private string _titleNarration;

            public XmlNode GetXMLData()
            {
                return _data;
            }
            public string GetTitleNarration()
            {
                return _titleNarration;
            }
 			public string GetSentence()
			{
				return _senteces[LanguageController.Instance.CodeLanguage];
			}
 			public string GetAudio()
			{
				return _audios[LanguageController.Instance.CodeLanguage];
			}

			public NarrationToken(NarrationController narrationController, XmlNode data, string titleNarration)
			{
				_narrationController = narrationController;
				_data = data;
				_titleNarration = titleNarration;
				InitData();
			}

            public NarrationToken(NarrationController narrationController, NarrationToken data, string titleNarration)
            {
                _narrationController = narrationController;
                _data = data.GetXMLData();
                _titleNarration = data.GetTitleNarration();
                InitData();
            }

			private void InitData()
			{
				Id       = ParseIntAttr(_data, "id", 0);
				NameClip = GetAttr(_data, "audioclip", "");

				XmlNode nodeLanguages = _data["languages"];   // element indexer, no XPath
				if (nodeLanguages != null && nodeLanguages.ChildNodes.Count > 0)
					Message = new TextEntry("", nodeLanguages.ChildNodes);

				XmlNode nodeAudios = _data["audios"];          // no XPath
				if (nodeAudios != null && nodeAudios.ChildNodes.Count > 0)
					Audios = new TextEntry("", nodeAudios.ChildNodes);

				SetUpAudioTargetLanguage();
			#if ENABLE_SPEECH
				WordTime = XmlCreator.AVERAGE_TIME_FOR_WORD;
			#else
				string wt = GetAttr(_data, "wordtime", null);
				WordTime = wt != null
					? float.Parse(wt, CultureInfo.InvariantCulture)
					: _narrationController.GetTimeDisplayUnit();
			#endif
				StartEvent    = GetAttr(_data, "startEvent", "");
				EndEvent      = GetAttr(_data, "endEvent", "");
				ShouldHideGuide = bool.TryParse(GetAttr(_data, "hide", "false"), out bool d) && d;
				ShouldDestroy = bool.TryParse(GetAttr(_data, "destroy", "false"), out bool h) && h;
				ShouldPause   = bool.TryParse(GetAttr(_data, "pause", "false"), out bool p) && p;
				ShouldPauseOriginal = ShouldPause;

				Assets = new List<NarrationObject>();
				foreach (XmlNode child in _data.ChildNodes)    // replaces SelectNodes("./asset")
				{
					if (child.NodeType == XmlNodeType.Element && child.Name == "asset")
						Assets.Add(new NarrationObject(child));
				}
			}

			private string GetAttr(XmlNode node, string name, string fallback)
			{
				if (node?.Attributes == null) return fallback;
				XmlAttribute a = node.Attributes[name];
				return a != null ? a.Value : fallback;
			}

			private int ParseIntAttr(XmlNode node, string name, int fallback)
			{
				string s = GetAttr(node, name, null);
				return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;
			}

			public void DestroyNarrationObjects()
			{
				if (ShouldDestroy)
				{
					bool shouldThrowLocalEvent = true;
#if ENABLE_NETWORKING
					if (NetworkController.Instance.IsConnected)
					{
						shouldThrowLocalEvent = false;
					}
					if (NetworkController.Instance.IsServer && NetworkController.Instance.IsConnected)
					{
						// NetworkController.Instance.DelayNetworkEvent(EventNarrationTokenDestroyNarrationObject, 0.01f, -1, -1);
					}
#endif				
					if (shouldThrowLocalEvent)
					{
						SystemEventController.Instance.DispatchSystemEvent(EventNarrationTokenDestroyNarrationObject);
					}
				}			
			}

			public void CreateNarrationObjects()
			{
				foreach (NarrationObject narrationObj in Assets)
				{
					bool shouldThrowLocalEvent = true;
#if ENABLE_NETWORKING
					if (NetworkController.Instance.IsConnected)
					{
						shouldThrowLocalEvent = false;
					}
					if (NetworkController.Instance.IsConnected)
					{
						if (NetworkController.Instance.IsServer)
						{
							if (_narrationController.IsMainNarration)
							{
								NetworkController.Instance.DelayNetworkEvent(EventNarrationTokenCreateNarrationObject, 0.01f, -1, -1, _narrationController.IsMainNarration, (int)narrationObj.Type, narrationObj.AssetName, narrationObj.Position, narrationObj.Rotation, narrationObj.Scale, narrationObj.Animation);
							}	
							else
							{
								shouldThrowLocalEvent = true;
							}
						}
						else
						{
							shouldThrowLocalEvent = true;
						}
					}
#endif				
					if (shouldThrowLocalEvent)
					{
						SystemEventController.Instance.DispatchSystemEvent(EventNarrationTokenCreateNarrationObject, _narrationController.IsMainNarration, narrationObj.Type, narrationObj.AssetName, narrationObj.Position, narrationObj.Rotation, narrationObj.Scale, narrationObj.Animation);
					}
				}
			}

			public string GetNarrationWaypoints()
			{
				List<NarrationObject> narrationObjects = new List<NarrationObject>();
				foreach (NarrationObject narrationObj in Assets)
				{
					if (narrationObj.Type == TypeObjectNarration.Waypoints)
					{
						narrationObjects.Add(narrationObj);
					}
				}
				if (narrationObjects.Count > 0)
				{
					SerializedNarrationObjects serializedNarrationObjects = new SerializedNarrationObjects();
					serializedNarrationObjects.NarrationObjects = narrationObjects.ToArray();
					return JsonUtility.ToJson(serializedNarrationObjects, false);
				}
				else
				{
					return "";
				}
			}

			public void PrepareSegments()
			{
				for (int i = 0; i < LanguageController.Instance.SupportedLanguages.Length; i++)
				{
					string codeLanguage = LanguageController.Instance.SupportedLanguages[i];

					string textMessage = Message?.GetText(codeLanguage);
					if (textMessage == null) textMessage = "";
					_senteces.Add(codeLanguage, textMessage);

					string textAudio = Audios?.GetText(codeLanguage);
					if (textAudio == null) textAudio = "";
					_audios.Add(codeLanguage, textAudio);
				}
			}

						
			public void PrepareSegments(float startTime, int words)
			{
				StartTime = startTime;
				for (int i = 0; i < LanguageController.Instance.SupportedLanguages.Length; i++)
				{
					string codeLanguage = LanguageController.Instance.SupportedLanguages[i];
					List<string> displayUnits = new List<string>();
					string sentence = PrepareSegmentByLanguage(displayUnits, words, codeLanguage);
					_displayUnits.Add(codeLanguage, displayUnits);
					_senteces.Add(codeLanguage, sentence);

					string textAudio = Audios?.GetText(codeLanguage);
					if (textAudio == null) textAudio = "";
					_audios.Add(codeLanguage, textAudio);
				}

				UpdateTargetLanguage();
			}

			public void UpdateTargetLanguage(bool dispatchEvent = false, bool updateAudio = false)
            {
				DisplayUnits = _displayUnits[LanguageController.Instance.CodeLanguage];

				if (updateAudio) SetUpAudioTargetLanguage();

				if (dispatchEvent) UIEventController.Instance.DispatchUIEvent(NarrationToken.NarrationTokenViewUpdateText, _narrationController.IsMainNarration,  DisplayUnits[CurrentUnit], _titleNarration);
			}

			private void SetUpAudioTargetLanguage()
            {
				string lang = LanguageController.Instance.CodeLanguage;

				float audioTime = -1f;
				string audioString = Audios?.GetText(lang);
				if (!string.IsNullOrEmpty(audioString))
				{
					if (!float.TryParse(audioString, NumberStyles.Float, CultureInfo.InvariantCulture, out audioTime))
						audioTime = -1f;
				}
				Time = -1;
				string targetTime = GetAttr(_data, "time", null);
				if (targetTime != null)
				{
					if (!float.TryParse(targetTime, out Time))
					{
						Time = -1;
					}
				}					
				if (Time != -1)
				{
					if (_narrationController.AINarration)
					{
						if (audioTime != -1)
						{
							Time = audioTime;
						}
					}
				}
				else
				{
					if (audioTime != -1)
					{
						Time = audioTime;
					}
					else
					{
						string text = Message?.GetText(lang);               // null-safe both ways
						Time = (text?.Length ?? 0) * AVERAGE_TIME_FOR_CHARACTER;						
					}
				}

				if (_narrationController.IsMainNarration)
				{
					Time += 1;
				}
				else
				{
					Time += 2f;
				}				
			}

			private string PrepareSegmentByLanguage(List<string> displayUnits, int words, string languageCode)
            {
				string data = Message.GetText(languageCode);
				if (data == null)
				{
					displayUnits.Add("");
					return "";
				}
				else
				{
					string[] wordData = data.Split(' ');
					int counterWords = 0;
					string currentText = "";
					string sentence = "";
					for (int i = 0; i < wordData.Length; i++)
					{
						counterWords++;
						if (counterWords < words)
						{
							currentText += " " + wordData[i];
							sentence += " " + wordData[i];
						}
						else
						{
							displayUnits.Add(currentText);
							counterWords = 0;
							currentText = " " + wordData[i];
							sentence += " " + wordData[i];
						}
					}
					displayUnits.Add(currentText);
					return sentence;
				}
			}

			public void Reset()
			{
				CurrentUnit = -1;
				ShouldPause = ShouldPauseOriginal;
			}
			
			public void FullReset()
			{
				Reset();

				foreach (KeyValuePair<string,List<string>> item in _displayUnits)
				{
					item.Value.Clear();
				}
				
				_displayUnits.Clear();
				_senteces.Clear();
				_audios.Clear();
			}
			public override string ToString()
			{
				string data = "\t TOKEN::Id["+Id+"]["+Time+"]["+NameClip+"]["+StartEvent+"][]"+EndEvent+"]::TEXT=" + Message.GetText(LanguageController.Instance.CodeLanguage);
				return data;
			}
			public void ShowTime()
			{
				Debug.LogError("\tNarrationToken::StartTime["+StartTime+"]::Time["+Time+"]::UNITS["+DisplayUnits.Count+"]::TEXT=" + DisplayUnits[0]);
			}
			public void Update(float time, string title)
			{
				float progressTime = time - StartTime;
				int nextUnit = (int)(progressTime / WordTime);
				if (CurrentUnit < nextUnit)
				{
					CurrentUnit = nextUnit;
					if (CurrentUnit < DisplayUnits.Count)
					{
						UIEventController.Instance.DispatchUIEvent(NarrationToken.NarrationTokenViewUpdateText, _narrationController.IsMainNarration, DisplayUnits[CurrentUnit], title);
					}
				}
			}
		}

		[Serializable]
		public class NarrationData
		{
			public const string EventNarrationDataStart = "EventNarrationDataStart";
			public const string EventNarrationDataEnd = "EventNarrationDataEnd";
			public const string EventNarrationPlayAudio = "EventNarrationPlayAudio";

			public int Id;
			public string StartEvent;
			public string EndEvent;
			public NarrationToken Title;	
			public List<NarrationToken> Segments;	
			public NarrationToken CurrentToken;
			public float StartTime = -1;
			public float TotalTime = -1;
			public float CurrentTime = -1;
			public string TitleNarration 
			{
				get 
				{
					if (Title != null)
					{
						return Title.GetSentence();
					}
					else
					{
						return _titleNarration;
					}
				}
			}

			private string _titleNarration = "";

			private NarrationController _narrationController;

			public NarrationData(NarrationController narrationController, int id, string startEvent, string endEvent, string titleNarration)
			{
				_narrationController = narrationController;
				Id = id;
				StartEvent = startEvent;
				EndEvent = endEvent;
				_titleNarration = titleNarration;
				Segments = new List<NarrationToken>();				
			}

            public NarrationData(NarrationController narrationController, NarrationData data)
            {
                _narrationController = narrationController;
                Id = data.Id;
                StartEvent = data.StartEvent;
                EndEvent = data.EndEvent;
                _titleNarration = data.TitleNarration;
                Segments = new List<NarrationToken>();
                foreach(NarrationToken segment in data.Segments)
                {
                    Segments.Add(new NarrationToken(_narrationController, segment, TitleNarration));
                }
            }			

			public override string ToString()
			{
				string data = "\n NARRATION::Id["+Id+"]["+StartEvent+"][]"+EndEvent+"]\n";
				foreach (NarrationToken item in Segments)
				{
					data += item.ToString() + "\n";
				}
				return data;
			}

			public void ShowTime()
			{
				Debug.LogError("NarrationData::StartTime["+StartTime+"]::TotalTime["+TotalTime+"]::Segments["+Segments.Count+"]");
				foreach (NarrationToken item in Segments)
				{
					item.ShowTime();
				}
			}

			public void ReportSentences()
			{
				foreach (NarrationToken item in Segments)
				{
					SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerReportTokens, item.NameClip, item);
				}
			}

			public void PrepareSegments(float startTime, int words)
			{
				StartTime = startTime;
				TotalTime = 0;
				float previousStart = startTime;
				float previousTotal = 0;
				for (int i = 0; i < Segments.Count; i++)
				{
					NarrationToken item = Segments[i];
					if (i > 0)
					{
						previousStart = Segments[i-1].StartTime;
						previousTotal = Segments[i-1].Time;
					}
					item.PrepareSegments(previousStart + previousTotal, words);
					TotalTime += item.Time;
				}
			}
			
			public void Reset()
			{
				CurrentTime = -1;
				foreach (NarrationToken item in Segments)
				{
					item.Reset();
				}
			}

			public void FullReset()
			{
				CurrentTime = -1;
				foreach (NarrationToken item in Segments)
				{
					item.FullReset();
				}
			}

			public void UpdateTargetLanguage()
            {
				foreach (NarrationToken item in Segments)
				{
					item.UpdateTargetLanguage(false, true);
				}
            }

			public void UpdateCurrentTokenTargetLanguage()
			{
				if (CurrentToken != null)
				{
					CurrentToken.UpdateTargetLanguage(true, true);
				}
			}

			public void Update(float time)
			{
				bool shouldPlayNewAudio = false;
				if (CurrentTime == -1)
				{
					shouldPlayNewAudio = true;
					CurrentToken = Segments[0];
					CurrentToken.CreateNarrationObjects();
					string serializeWaypoints = CurrentToken.GetNarrationWaypoints();
					SystemEventController.Instance.DispatchSystemEvent(NarrationToken.EventNarrationTokenStart, _narrationController.IsMainNarration, Id, TotalTime, CurrentToken.StartEvent, serializeWaypoints);
					SystemEventController.Instance.DispatchSystemEvent(NarrationToken.EventNarrationTokenSetVisibilityTourGuide, !CurrentToken.ShouldHideGuide);
				}
				else
				{
					for (int i = 0; i < Segments.Count; i++)
					{
						NarrationToken item = Segments[i];
						if ((item.StartTime >= CurrentTime) && (item.StartTime < time))
						{
							if (CurrentToken != null)
							{
								if (CurrentToken.ShouldPause)
								{
									CurrentToken.ShouldPause = false;
									if (_narrationController.IsMainNarration)
									{
										_narrationController.IsPlayingNarration = false;
									}
									SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerRequestButtonNextAction, _narrationController.IsMainNarration);
									return;
								}
								else
								{
									SystemEventController.Instance.DispatchSystemEvent(NarrationToken.EventNarrationTokenEnd, _narrationController.IsMainNarration, Id, CurrentToken.EndEvent);
									CurrentToken.Reset();
								}								
							} 
							shouldPlayNewAudio = true;							
							CurrentToken = item;
							CurrentToken.DestroyNarrationObjects();
							CurrentToken.CreateNarrationObjects();
							string serializeWaypoints = CurrentToken.GetNarrationWaypoints();
							SystemEventController.Instance.DispatchSystemEvent(NarrationToken.EventNarrationTokenStart, _narrationController.IsMainNarration, Id, TotalTime, CurrentToken.StartEvent, serializeWaypoints);
							SystemEventController.Instance.DispatchSystemEvent(NarrationToken.EventNarrationTokenSetVisibilityTourGuide, !CurrentToken.ShouldHideGuide);
							break;
						}
					}
				}
				CurrentTime = time;

				if (shouldPlayNewAudio)
				{
					float volumeChannel = SoundsController.Instance.GetChannelAudioSource(_narrationController.GetAudioChannelNarration()).volume;
					bool shouldPlayAudio = true;
#if ENABLE_SPEECH
					bool isANumber = float.TryParse(CurrentToken.GetAudio(), NumberStyles.Float, CultureInfo.InvariantCulture, out float audioTime);
					if ((CurrentToken.GetAudio() != null) && (CurrentToken.GetAudio().Length > 0) && isANumber)
					{
						_narrationController.WaitForAudio = false;
					}
					else
					{
						if (!_narrationController.AINarration)
						{
							shouldPlayAudio = false;
							_narrationController.CurrentNarrationTokenSynthesize = CurrentToken;
							_narrationController.WaitForAudio = true;
							SpeechRecognitionController.Instance.Synthetize(CurrentToken.GetSentence(), GenderVoice.FEMALE, AgeVoice.ADULT, SpeedVoice.NORMAL, NarrationController.EventNarrationControllerSpeechSynthetized);
						}
					}
#endif
					if (shouldPlayAudio)
					{	
						if (!_narrationController.AINarration)
						{					
							if (_narrationController.IsMainNarration)
							{
								SystemEventController.Instance.DispatchSystemEvent(EventNarrationPlayAudio, -1, Id, CurrentToken.Id, _narrationController.GetAudioChannelNarration(), volumeChannel);
							}
							else
							{
								SystemEventController.Instance.DispatchSystemEvent(EventNarrationPlayAudio, _narrationController.Secret, Id, CurrentToken.Id, _narrationController.GetAudioChannelNarration(), volumeChannel);
							}
						}
						else
						{							
							SoundsController.Instance.PlaySoundClipFx(_narrationController.GetAudioChannelNarration(), CurrentToken.Audio, false, volumeChannel, false);
						}
					}
					if (!_narrationController.IsPlayingNarration) SoundsController.Instance.PauseSoundFX(_narrationController.GetAudioChannelNarration());
				}
				
				if (_narrationController.IsPlayingNarration)
				{
					CurrentToken.Update(time, (Title!=null?Title.GetSentence():""));	
				}				
			}
		}

		public const string EventNarrationControllerPlayInfo = "EventNarrationControllerPlayInfo";
		public const string EventNarrationControllerDoPause = "EventNarrationControllerDoPause";
		public const string EventNarrationControllerDoStop = "EventNarrationControllerDoStop";
		public const string EventNarrationControllerDoResume = "EventNarrationControllerDoResume";		
		public const string EventNarrationControllerDoRestart = "EventNarrationControllerDoRestart";		
		public const string EventNarrationControllerConfirmedRestart = "EventNarrationControllerConfirmedRestart";				
		public const string EventNarrationControllerPaused = "EventNarrationControllerPaused";
		public const string EventNarrationControllerFinished = "EventNarrationControllerFinished";
		public const string EventNarrationControllerResumeIfPaused = "EventNarrationControllerResumeIfPaused";
		public const string EventNarrationControllerPlayPOIByIndex = "EventNarrationControllerPlayPOIByIndex";
		public const string EventNarrationControllerSetAction = "EventNarrationControllerSetAction";
		public const string EventNarrationControllerRequestAction = "EventNarrationControllerRequestAction";
		public const string EventNarrationControllerResponseAction = "EventNarrationControllerResponseAction";
		public const string EventNarrationControllerSetVolume = "EventNarrationControllerSetVolume";
		public const string EventNarrationControllerRequestTitlePOI = "EventNarrationControllerRequestTitlePOI";
        public const string EventNarrationControllerRequestTitleReplay = "EventNarrationControllerRequestTitleReplay";
        public const string EventNarrationControllerResponseTitleReplay = "EventNarrationControllerResponseTitleReplay";
		public const string EventNarrationControllerRequestReplayForAll = "EventNarrationControllerRequestReplayForAll";		
		public const string EventNarrationControllerRunNarrationPOI = "EventNarrationControllerRunNarrationPOI";				
		public const string EventNarrationControllerReportTokens = "EventNarrationControllerReportTokens";
		public const string EventNarrationControllerReleaseAllResources = "EventNarrationControllerReleaseAllResources";
		public const string EventNarrationControllerUpdateTexts = "EventNarrationControllerUpdateTexts";
		public const string EventNarrationControllerUpdateTitleLabel = "EventNarrationControllerUpdateTitleLabel";
		public const string EventNarrationControllerReplayPOIUpdateTitleLabel = "EventNarrationControllerReplayPOIUpdateTitleLabel";
		public const string NarrationControllerEndCurrentNarration = "NarrationControllerEndCurrentNarration";
		public const string NarrationControllerReportEndedCurrentNarration = "NarrationControllerReportEndedCurrentNarration";
		public const string EventNarrationControllerStopAllClients = "EventNarrationControllerStopAllClients";
		public const string EventNarrationControllerRequestButtonNextAction = "EventNarrationControllerRequestButtonNextAction";
		public const string EventNarrationControllerDestroyNoMainNarrations = "EventNarrationControllerDestroyNoMainNarrations";
		public const string EventNarrationControllerSpeechSynthetized = "EventNarrationControllerSpeechSynthetized";
		public const string EventNarrationControllerRequestPOIAudios = "EventNarrationControllerRequestPOIAudios";
		public const string EventNarrationControllerDownloadPOIAudios = "EventNarrationControllerDownloadPOIAudios";
		public const string EventNarrationControllerStopNarrations = "EventNarrationControllerStopNarrations";		

		public enum TypeActionNext { Play = 0, Pause, Walk }

		[SerializeField] private int WordsForUnit = 15;
		[SerializeField] private float TimeDisplayUnit = 5;

        private List<NarrationData> _narration = null;

		private bool _isPlaying = false;
		private bool _waitForAudio = false;
		private NarrationData _currentNarration = null;
		private float _currentTime = 0;
		private TypeActionNext _action;
		private bool _mainNarration;
		private bool _aiNarration = false;
		private ChannelsAudio _channelAudio;
		private int _currentPoi = -1;
		private float _startLogTime = 0;
		private int _pausedPOI = 0;
		private int _restartedPOI = 0;
		private NarrationToken _currentNarrationTokenSynthesize = null;
		private bool _autoDestroy = false;
		private int _secret = -1;

		public int CurrentPoi
		{
			get { return _currentPoi; }
		}
		public int Secret
		{
			get { return _secret; }
			set { _secret = value; }
		}
		public bool IsPlayingNarration
		{
			get { return _isPlaying; }
			set { _isPlaying = value; }
		}

		public bool IsMainNarration
		{
			get { return _mainNarration; }
		}
		public bool AINarration
		{
			get { return _aiNarration; }
		}
		public float CurrentTime
		{
			get { return _currentTime; }
		}
		public float GetTimeDisplayUnit()
		{
			return TimeDisplayUnit;
		}

		public ChannelsAudio GetAudioChannelNarration()
		{
			return _channelAudio;
		}

		private TypeActionNext Action
		{
			get { return _action; }
			set {
				_action = value;
				SystemEventController.Instance.DispatchSystemEvent(EventNarrationControllerResponseAction, _action, _currentTime);
			}
		}
		public NarrationToken CurrentNarrationTokenSynthesize
		{
			get { return _currentNarrationTokenSynthesize; }
			set { _currentNarrationTokenSynthesize = value; }
		}
		public bool WaitForAudio
		{
			set { _waitForAudio = value; }
		}

		void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;		
#if ENABLE_NETWORKING			
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;	
#endif			
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}

		public void Destroy()
		{			
			Stop();
			GameObject.Destroy(this.gameObject);
		}

		public int GetTotalSizeNarrations()
		{
			return (_narration != null)?_narration.Count:0;
		}

        public void LoadNarrationTexts(TextAsset data, bool mainNarration)
        {
            if (_narration != null) return;
			_narration = new List<NarrationData>();

			_aiNarration = false;
			_mainNarration = mainNarration;
			if (_mainNarration)
			{
				_channelAudio = ChannelsAudio.FX1;
			}
			else
			{
				_channelAudio = ChannelsAudio.FX2;
			}

			LoadNarrationString(data.text);
        }

		public void LoadNarrationGeneric(string data, bool aiNarration, bool autoDestroy)
		{
			if (_narration != null) return;
			_narration = new List<NarrationData>();
			
			_mainNarration = false;
			_aiNarration = aiNarration;
			_channelAudio = ChannelsAudio.FX2;
			_autoDestroy = autoDestroy;

			LoadNarrationString(data);	
		}

		private void LoadNarrationString(string data)
		{
			if ((data == null) || (data.Length == 0))
			{
				return;
			} 
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(data);

			XmlNodeList narrationData = xmlDoc.GetElementsByTagName("narration");
            foreach (XmlNode narrationEntry in narrationData)
            {
				int idToken = int.Parse(narrationEntry.Attributes["id"].Value);
				string startEvent = ((narrationEntry.Attributes["startEvent"] != null)?narrationEntry.Attributes["startEvent"].Value:"");
				string endEvent = ((narrationEntry.Attributes["endEvent"] != null)?narrationEntry.Attributes["endEvent"].Value:"");
				string titleNarration = ((narrationEntry.Attributes["title"] != null)?narrationEntry.Attributes["title"].Value:"");
 				
				NarrationData newNarration = new NarrationData(this, idToken, startEvent, endEvent, titleNarration);

				// TITLE
				XmlNodeList tokenTitles = narrationEntry.SelectNodes("./title");
				if (tokenTitles != null)
				{
					foreach (XmlNode titleToken in tokenTitles)
					{
						newNarration.Title = new NarrationToken(this, titleToken, titleNarration);
						newNarration.Title.PrepareSegments();
					}
				}

				// SEGMENTS		
				XmlNodeList tokensNarration = narrationEntry.SelectNodes("./token");
				List<NarrationToken> tokens = new List<NarrationToken>();
 				foreach (XmlNode narrationToken in tokensNarration)
            	{
                	newNarration.Segments.Add(new NarrationToken(this, narrationToken, titleNarration));
				}

				// ADDED NEW ELEMENT
				_narration.Add(newNarration);
            }

			float startTime = 0;
			for (int i = 0; i < _narration.Count; i++)
			{
				if (i > 0)
				{
					startTime = _narration[i-1].StartTime + _narration[i-1].TotalTime;
				}
				_narration[i].PrepareSegments(startTime, WordsForUnit);
			}

			SystemEventController.Instance.Event += OnSystemEvent;
#if ENABLE_NETWORKING			
			NetworkController.Instance.NetworkEvent += OnNetworkEvent;
#endif			
			UIEventController.Instance.Event += OnUIEvent;
		}

        public void LoadNarrationData(NarrationData data)
        {
            if (_narration != null) return;
            _narration = new List<NarrationData>();

            _mainNarration = false;
			_aiNarration = false;
            _channelAudio = ChannelsAudio.FX2;
            
            _narration.Add(new NarrationData(this, data));

            float startTime = 0;
            for (int i = 0; i < _narration.Count; i++)
            {
                if (i > 0)
                {
                    startTime = _narration[i-1].StartTime + _narration[i-1].TotalTime;
                }
                _narration[i].PrepareSegments(startTime, WordsForUnit);
            }

            SystemEventController.Instance.Event += OnSystemEvent;
			UIEventController.Instance.Event += OnUIEvent;
#if ENABLE_NETWORKING
			NetworkController.Instance.NetworkEvent += OnNetworkEvent;
#endif			
        }

        public float GetTotalTime()
		{
			if (_currentNarration != null)
			{
				return _currentNarration.TotalTime;
			}
			else
			{
				return 0;
			}
		}

		public void UpdateNarrationTime()
		{
			float startTime = 0;
			for (int i = 0; i < _narration.Count; i++)
			{
				_narration[i].FullReset();
				if (i > 0)
				{
					startTime = _narration[i-1].StartTime + _narration[i-1].TotalTime;
				}
				_narration[i].PrepareSegments(startTime, WordsForUnit);
			}			
		}

		public void Play(float jumpTime = -1)
		{
			if (jumpTime != -1)
			{
				_currentTime = jumpTime;
				Reset();
			}
			_currentTime += 0.1f;
			_currentNarration = GetNarrationDataByTime(_currentTime);
			if (_currentNarration == null)
			{
				_currentNarration = _narration[0];
			}			
			_isPlaying = true;
			_currentNarration.Update(_currentTime);
			SystemEventController.Instance.DispatchSystemEvent(EventNarrationControllerPlayInfo, _mainNarration, _currentNarration.TotalTime);
		}

		public void Pause(bool isUserAction = false)
		{
			if (_mainNarration) SystemEventController.Instance.DispatchSystemEvent(EventNarrationControllerPaused, isUserAction);
			_isPlaying = false;
			SoundsController.Instance.PauseSoundFX(GetAudioChannelNarration());
		}
		public void Stop()
		{
			if (_mainNarration) SystemEventController.Instance.DispatchSystemEvent(EventNarrationControllerFinished);
			_isPlaying = false;
			_currentTime = 0;
			SoundsController.Instance.StopSoundFx(GetAudioChannelNarration());
		}
		
		public void Resume()
		{
			_isPlaying = true;
			SystemEventController.Instance.DispatchSystemEvent(EventNarrationControllerPlayInfo, _mainNarration, _currentNarration.TotalTime);
			SoundsController.Instance.ResumeSoundFX(GetAudioChannelNarration());			
		}

		private NarrationData GetNarrationDataByTime(float time)
		{
			for (int i = 0; i < _narration.Count; i++)
			{
				NarrationData narration = _narration[i];
				if ((narration.StartTime <= time) && (narration.StartTime + narration.TotalTime > time))
				{
					return narration;
				}
			}
			return null;
		}

		private NarrationData GetNarrationDataByPOIIndex(int poi)
		{
			if ((poi >= 0) && (poi < _narration.Count))
			{
				return _narration[poi];
			}
			else
			{
				return null;
			}			
		}
		
		private void PlayByPOIIndex(int poi)
		{			
			_currentPoi = poi;
			NarrationData narration = GetNarrationDataByPOIIndex(poi);
			if (narration != null)
			{
				Play(narration.StartTime);
			}
		}
		public void ReportSentences()
		{
			if (_narration != null)
            {
				foreach(NarrationData narration in _narration)
				{
					narration.ReportSentences();
				}
			}
		}
		private void Reset()
		{
			if (_narration != null)
            {
				foreach(NarrationData narration in _narration)
				{
					narration.Reset();
				}
			}
		}
		private void OnSystemEvent(string nameEvent, object[] parameters)
        {
#if ENABLE_SPEECH			
			if (!AINarration)
			{
				if (nameEvent.Equals(EventNarrationControllerSpeechSynthetized))
				{
					if ((bool)parameters[0])
					{
						string textOrigin = (string)parameters[1];
						AudioClip audioSynth = (AudioClip)parameters[2];
						if (_currentNarrationTokenSynthesize == null)
						{
							SoundsController.Instance.PlaySoundClipFx(GetAudioChannelNarration(), audioSynth, false, 1, false);
						}
						else
						if (_currentNarrationTokenSynthesize.GetSentence().Equals(textOrigin))
						{
							_currentNarrationTokenSynthesize.Audio = audioSynth;
							if (_currentNarrationTokenSynthesize.Audio != null)
							{
								SoundsController.Instance.PlaySoundClipFx(GetAudioChannelNarration(), _currentNarrationTokenSynthesize.Audio, false, 1, false);
							}
							_waitForAudio = false;
						}
					}
				}
			}
#endif			
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerReleaseAllResources))
            {
                Destroy();
            }		
			if (nameEvent.Equals(EventNarrationControllerReleaseAllResources))
            {
				Reset();
				if (_narration != null) _narration.Clear();
				_narration = null;
				_isPlaying = false;
				_currentNarration = null;
				_currentTime = 0;
				Destroy();
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerUpdateTexts))
			{
				if (_narration != null)
                {
					for (int i = 0; i < _narration.Count; i++)
					{
						_narration[i].UpdateTargetLanguage();
					}
					if (_currentNarration != null)
					{
						_currentNarration.UpdateCurrentTokenTargetLanguage();
					}
				}
			}
            if (nameEvent.Equals(EventNarrationControllerDoPause))
            {
                bool targetMainNarration = (bool)parameters[0];
                if (_mainNarration == targetMainNarration)
                {
                    if (parameters.Length > 1)
                    {
                        Pause((bool)parameters[1]);
                    }
                    else
                    {
                        Pause();
                    }
                }
            }
            if (nameEvent.Equals(EventNarrationControllerDoResume))
            {
                bool targetMainNarration = (bool)parameters[0];
                if (_mainNarration == targetMainNarration)
                {
                    Resume();
                }
            }
            if (nameEvent.Equals(EventNarrationControllerSetVolume))
            {
                bool targetMainNarration = (bool)parameters[0];
                if (_mainNarration == targetMainNarration)
                {
                    float newVolume = (float)parameters[1];
                    SoundsController.Instance.SetVolume(GetAudioChannelNarration(), newVolume);
                }
            }
			if (nameEvent.Equals(EventNarrationControllerStopNarrations))
            {				
				SoundsController.Instance.StopSoundFx(GetAudioChannelNarration());
            }			
			if (nameEvent.Equals(EventNarrationControllerDestroyNoMainNarrations))
			{
				if (!IsMainNarration)
				{
					Destroy();
				}
			}			

			if (!_mainNarration) return;

			if (nameEvent.Equals(EventNarrationControllerFinished))
			{
#if ENABLE_NETWORKING
				if (NetworkController.Instance.IsServer && NetworkController.Instance.IsConnected)
				{
					NetworkController.Instance.DelayNetworkEvent(EventNarrationControllerStopAllClients, 0.01f, -1, -1);
				}
#endif				
			}
			if (nameEvent.Equals(EventNarrationControllerRequestAction))
			{
				SystemEventController.Instance.DispatchSystemEvent(EventNarrationControllerResponseAction, Action, _currentTime);
			}
			if (nameEvent.Equals(EventNarrationControllerPlayPOIByIndex))
			{
				if (_currentTime == 0)
				{
					_startLogTime = GameLevelData.Instance.TotalTimeDone;
					_pausedPOI = 0;
					_restartedPOI = 0;
					PlayByPOIIndex((int)parameters[0]);
				}
				else
				{
					Resume();
				}									
			}
			if (nameEvent.Equals(EventNarrationControllerResumeIfPaused))
			{
				if (Action == TypeActionNext.Pause)
                {
					Resume();
				}
			}
			if (nameEvent.Equals(EventNarrationControllerSetAction))
			{
				Action = (TypeActionNext)parameters[0];
			}
			if ((nameEvent.Equals(NarrationData.EventNarrationDataEnd)) || (nameEvent.Equals(EventNarrationControllerDoStop)))
			{
				Stop();
			}
			if (nameEvent.Equals(EventNarrationControllerRequestTitlePOI))
			{
				int poiSelected = (int)parameters[0];
				NarrationData narrationSelected = GetNarrationDataByPOIIndex(poiSelected);
				if (narrationSelected != null)
				{
					UIEventController.Instance.DispatchUIEvent(NarrationController.EventNarrationControllerUpdateTitleLabel, narrationSelected.TitleNarration);
				}
			}
			if (nameEvent.Equals(EventNarrationControllerDoRestart))
			{
				if (_mainNarration)
				{
					bool wasPlaying = _isPlaying;
					_restartedPOI++;	
					SystemEventController.Instance.DispatchSystemEvent(GameLevelData.EventGameLevelDataDestroyNarrationObjects);			
					PlayByPOIIndex(_currentPoi);
					Stop();
					SystemEventController.Instance.DispatchSystemEvent(EventNarrationControllerConfirmedRestart, _isPlaying);
				}				
			}
			if (nameEvent.Equals(EventNarrationControllerRequestTitleReplay))
            {
                int poiReplay = (int)parameters[0];
                NarrationData narrationSelected = GetNarrationDataByPOIIndex(poiReplay);
                if (narrationSelected != null)
                {
					UIEventController.Instance.DispatchUIEvent(EventNarrationControllerReplayPOIUpdateTitleLabel, narrationSelected);
                }
            }
			if (nameEvent.Equals(EventNarrationControllerRequestReplayForAll))
			{
				int poiReplay = (int)parameters[0];
				NarrationData narrationSelected = GetNarrationDataByPOIIndex(poiReplay);
				if (narrationSelected != null)
				{
					SystemEventController.Instance.DispatchSystemEvent(NarrationController.EventNarrationControllerRunNarrationPOI, poiReplay, narrationSelected);	
				}
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerRequestPOIAudios))
			{
				if (_mainNarration)
				{
					int poiCurrent = (int)parameters[0];
					NarrationData narration = GetNarrationDataByPOIIndex(poiCurrent);
					SystemEventController.Instance.DispatchSystemEvent(EventNarrationControllerDownloadPOIAudios, poiCurrent, narration.Segments);
				}				
			}
		}

#if ENABLE_NETWORKING
        private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
        {
			if (nameEvent.Equals(NarrationController.EventNarrationControllerResponseAction))
			{
				_currentTime = (float)parameters[1];
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerStopAllClients))
			{
				if (!NetworkController.Instance.IsServer)
				{
					Stop();
				}
			}
			if (nameEvent.Equals(NarrationToken.EventNarrationTokenDestroyNarrationObject))
			{
				SystemEventController.Instance.DispatchSystemEvent(NarrationToken.EventNarrationTokenDestroyNarrationObject);
			}
			if (nameEvent.Equals(NarrationToken.EventNarrationTokenCreateNarrationObject))
			{
				bool mainNarration = (bool)parameters[0];
				TypeObjectNarration typeObject = (TypeObjectNarration)((int)parameters[1]);
				string assetNameNarrationObj = (string)parameters[2];
				Vector3 posNarrationObj = (Vector3)parameters[3];
				Quaternion rotNarrationObj = (Quaternion)parameters[4];
				Vector3 scaleNarrationObj = (Vector3)parameters[5];
				string animationNarrationObj = (string)parameters[6];
				SystemEventController.Instance.DispatchSystemEvent(NarrationToken.EventNarrationTokenCreateNarrationObject, mainNarration, typeObject, assetNameNarrationObj, posNarrationObj, rotNarrationObj, scaleNarrationObj, animationNarrationObj);
			}
        }
#endif

		private void OnUIEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(NarrationControllerEndCurrentNarration))
			{
				GameLevelData.Instance.SetUnlockPOI(GameLevelData.Instance.NextAreaGame, _currentNarration.Id, (int)GameLevelData.Instance.TotalTimeDone);
#if ENABLE_ANALYTICS
				TourAnalyticsController.Instance.LogPOIVisitedEvent(GameLevelData.Instance.Age, _currentNarration.Id, _startLogTime, GameLevelData.Instance.TotalTimeDone, true, GameLevelData.Instance.TotalTimeDone, _pausedPOI, _restartedPOI);
#endif				
			}
        }

		private void Update()
		{
			if (_isPlaying && !_waitForAudio)
			{				
				_currentTime += Time.deltaTime;
				NarrationData currNarration = GetNarrationDataByTime(_currentTime);
				if ((_currentNarration != currNarration) || ENABLE_AUTOMATIC_TRANSITION)
				{
					NarrationData previousNarration = _currentNarration;
					if (previousNarration != null) previousNarration.Reset();
					_currentNarration = currNarration;
					if ((_currentNarration != null) && _mainNarration) SystemEventController.Instance.DispatchSystemEvent(NarrationData.EventNarrationDataStart, _currentNarration.StartEvent);
					if (_mainNarration)
					{
						SystemEventController.Instance.DispatchSystemEvent(NarrationData.EventNarrationDataEnd, previousNarration);
						if (_currentNarration != null)
						{
							GameLevelData.Instance.SetUnlockPOI(GameLevelData.Instance.NextAreaGame, _currentNarration.Id, (int)GameLevelData.Instance.TotalTimeDone);
#if ENABLE_ANALYTICS
							TourAnalyticsController.Instance.LogPOIVisitedEvent(GameLevelData.Instance.Age, _currentNarration.Id, _startLogTime, GameLevelData.Instance.TotalTimeDone, false, -1f, _pausedPOI, _restartedPOI);
#endif						
						} 
					} 
					if (currNarration == null)
					{
						if (_mainNarration)
						{
							GameLevelData.Instance.SetUnlockPOI(GameLevelData.Instance.NextAreaGame, previousNarration.Id + 1, (int)GameLevelData.Instance.TotalTimeDone);
#if ENABLE_ANALYTICS
							TourAnalyticsController.Instance.LogPOIVisitedEvent(GameLevelData.Instance.Age, previousNarration.Id + 1, _startLogTime, GameLevelData.Instance.TotalTimeDone, false, -1f, _pausedPOI, _restartedPOI);
#endif								
						}
						SystemEventController.Instance.DispatchSystemEvent(NarrationControllerReportEndedCurrentNarration);
						if (_autoDestroy)
						{
							Destroy();
						}
					}
					return;
				}
				if (_currentNarration != null)
                {
					_currentNarration.Update(_currentTime);
				}				
			}
		}
    }
}
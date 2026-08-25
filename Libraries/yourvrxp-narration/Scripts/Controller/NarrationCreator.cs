using yourvrexperience.Utils;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Xml;
using static yourvrexperience.Utils.SoundsController;
using static yourvrexperience.Narration.NarrationController;
#if ENABLE_SPEECH
using yourvrexperience.speech;
#endif
#if ENABLE_NETWORKING
using yourvrexperience.Networking;
#endif

namespace yourvrexperience.Narration
{
	public enum TypeObjectNarration { Image = 0, Video, Model3D, Sound, Interaction, Waypoints }

    public class NarrationCreator
    {
		[Serializable]
		public class NarrationCreatorToken
		{
			public int Id;
			public float Time;
			public string StartEvent;
			public string EndEvent;
			public string NameClip;
			public bool ShouldHideGuide = true;
			public bool ShouldDestroy = true;
			public bool ShouldPause = false;
			
			public List<NarrationObject> Assets = new List<NarrationObject>();

			private TextEntry Message;			
			private TextEntry Audio;			

			private XmlNode _data;
			private bool _isTitle;

			public bool IsTitle()
			{
				return _isTitle;
			}

            public XmlNode GetXMLData()
            {
                return _data;
            }

			public NarrationCreatorToken Clone()
			{
				NarrationCreatorToken newCopy = new NarrationCreatorToken(_data, _isTitle);
				return newCopy;
			}

			public NarrationCreatorToken(XmlNode data, bool isTitle)
			{
				_data = data;
				_isTitle = isTitle;
				InitData();
			}

			public NarrationCreatorToken(NarrationCreatorToken data, bool isTitle)
            {
                _data = data.GetXMLData();
				_isTitle = isTitle;
				InitData();
            }

			private void InitData()
			{
				Id = ((_data.Attributes["id"]==null)?-1:int.Parse(_data.Attributes["id"].Value));
				NameClip = ((_data.Attributes["audioclip"] == null)?"":_data.Attributes["audioclip"].Value);
				XmlNode nodeLanguages = _data.SelectSingleNode("./languages");
				if (nodeLanguages.ChildNodes.Count > 0)
				{
					Message = new TextEntry("", nodeLanguages.ChildNodes);
				}
				XmlNode nodeAudios = _data.SelectSingleNode("./audios");
				if (nodeAudios == null)
				{
					Audio = new TextEntry("", nodeLanguages.ChildNodes);
				}
				else
				{
					if (nodeAudios.ChildNodes.Count > 0)
					{
						Audio = new TextEntry("", nodeAudios.ChildNodes);
					}				
				}
				StartEvent = ((_data.Attributes["startEvent"] != null)? _data.Attributes["startEvent"].Value:"");
				EndEvent = ((_data.Attributes["endEvent"] != null)? _data.Attributes["endEvent"].Value:"");	
				ShouldHideGuide = ((_data.Attributes["hide"] != null)?bool.Parse(_data.Attributes["hide"].Value):false);
				ShouldDestroy = ((_data.Attributes["destroy"] != null)?bool.Parse(_data.Attributes["destroy"].Value):false);
				ShouldPause = ((_data.Attributes["pause"] != null)?bool.Parse(_data.Attributes["pause"].Value):false);
				XmlNodeList nodesAssets = _data.SelectNodes("./asset");
				Assets = new List<NarrationObject>();
				foreach (XmlNode nodeAsset in nodesAssets)
				{
					Assets.Add(new NarrationObject(nodeAsset));
				}
			}

			public string GetCurrentLanguageMessage()
			{
				return Message.GetText(LanguageController.Instance.CodeLanguage);
			}
			public string GetCurrentLanguageAudio()
			{
				return Audio.GetText(LanguageController.Instance.CodeLanguage);
			}
			public override string ToString()
			{
				string data = "\t TOKEN::Id["+Id+"]["+Time+"]["+NameClip+"]["+StartEvent+"][]"+EndEvent+"]["+ShouldDestroy+"][]"+ShouldPause+"]::TEXT=" + Message.GetText(LanguageController.Instance.CodeLanguage);
				return data;
			}

			public TextEntry GetMessage()
			{
				return Message;
			}
			public TextEntry GetAudio()
			{
				return Audio;
			}

			public string ToXML(int id)
			{
				string output = "\n";
				if (_isTitle)
				{
					output += "<title id = \""+id+"\" audioclip = \""+NameClip+"\" startEvent = \""+StartEvent+"\" endEvent = \""+EndEvent+"\">";
				} 
				else
				{
					if ((NameClip.Equals(NarrationToken.POI_EMPTY)))
					{
						output += "<token id = \""+id+"\" audioclip = \""+NarrationToken.POI_EMPTY+"\" startEvent = \""+StartEvent+"\" endEvent = \""+EndEvent+"\" hide = \""+ShouldHideGuide+"\" destroy = \""+ShouldDestroy+"\" pause = \""+ShouldPause+"\">";
					}
					else
					{
						output += "<token id = \""+id+"\" audioclip = \""+NameClip+"\" startEvent = \""+StartEvent+"\" endEvent = \""+EndEvent+"\" hide = \""+ShouldHideGuide+"\" destroy = \""+ShouldDestroy+"\" pause = \""+ShouldPause+"\">";
					}					
				}
				output += "<languages>" + Message.GetXML() + "</languages>";
				output += "<audios>" + Audio.GetXML() + "</audios>";
				if (_isTitle)
				{
					output += "</title>";
				} 
				else
				{
					output += "\n";
					foreach (NarrationObject asset in Assets)
					{
						output += "\n";
						output += asset.ToXML();
					}
					output += "</token>";
				}
				return output;
			}
		}

		[Serializable]
		public class NarrationCreatorData
		{
			public int Id;
			public string StartEvent;
			public string EndEvent;
			public NarrationCreatorToken Title;
			public List<NarrationCreatorToken> Segments;	
			public float StartTime = -1;
			public float TotalTime = -1;
			public float CurrentTime = -1;

			public NarrationCreatorData(int id, string startEvent, string endEvent)
			{
				Id = id;
				StartEvent = startEvent;
				EndEvent = endEvent;
				Segments = new List<NarrationCreatorToken>();				
			}

			public override string ToString()
			{
				string output = "SEGMENTS";
				for (int i = 0; i < Segments.Count; i++)
				{
					output += "\n"  + Segments[i].ToString();
				}
				return output;
			}

			public string ToXML(int index)
			{
				string output = "\n<narration id = \""+index+"\" startEvent = \""+StartEvent+"\" endEvent = \""+EndEvent+"\" title=\"nothing\">";
				output += "\n";
				output += Title.ToXML(0);
				for (int i = 0; i < Segments.Count; i++)
				{
					output += "\n"  + Segments[i].ToXML(i);
				}
				output += "\n</narration>";
				return output;
			}

			public void SetNewData(List<NarrationCreatorToken> newData)
			{
				Title = new NarrationCreatorToken(newData[0], true);
				Segments.Clear();
				for (int i = 1; i < newData.Count; i++)
				{
					Segments.Add(new NarrationCreatorToken(newData[i], false));
				}			
			}

			public bool DeleteSegment(int index)
			{
				if (Segments.Count > 1)
				{
					if (index < Segments.Count)
					{						
						Segments.RemoveAt(index);
						return true;
					}
				}
				return false;
			}

			public void AddSegment(TextEntry data, int index)
			{
				string xmlContent = "<token id = \""+index+"\" audioclip = \"\" wordtime=\"\" startEvent = \"\" endEvent = \"\"  destroy = \"true\" pause = \"false\">";
				xmlContent += "<languages>" + data.GetXML() + "</languages>";
				xmlContent += "<audios>" + data.GetXML() + "</audios>";
				xmlContent += "</token>";

				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.LoadXml(xmlContent);		
				XmlNode xmlDataNode = xmlDoc.FirstChild;

				Segments.Insert(index, new NarrationCreatorToken(xmlDataNode, false));
			}
		}

		[SerializeField] private int WordsForUnit = 15;
		[SerializeField] private float TimeDisplayUnit = 5;

        private List<NarrationCreatorData> _narration = null;

		public List<NarrationCreatorData> Narration
		{
			get { return _narration; }
		}

        public void LoadNarrationTexts(TextAsset data)
        {
            if (_narration != null) return;
			_narration = new List<NarrationCreatorData>();

			LoadNarrationString(data.text);
        }

		private XmlNode GetPlaceholderTitle()
		{
			string xmlString = "<title><languages><en>Title</en><es>Titulo</es><ca>Titol</ca><fr>Titre</fr></languages><audios><en></en><es></es><ca></ca><fr></fr></audios></title>";
			XmlDocument xmlDoc = new XmlDocument();
        	xmlDoc.LoadXml(xmlString);		
			return xmlDoc.FirstChild;
		}

		private XmlNode GetPlaceholderSegment()
		{
			string xmlString = "<token><languages><en>This is the first segment of the narration</en><es>Este es el primer segmento de la narración</es><ca>Aquest es el primer segment de la narració</ca><fr>C'est le premier segment de la narration</fr></languages><audios><en></en><es></es><ca></ca><fr></fr></audios></token>";
			XmlDocument xmlDoc = new XmlDocument();
        	xmlDoc.LoadXml(xmlString);		
			return xmlDoc.FirstChild;
		}

		public static string GetEmptyNarration()
		{
			return "";
		}

		public void AddNewPOINarration(int index, string data)
		{
            if (_narration != null) return;
			_narration = new List<NarrationCreatorData>();

			LoadNarrationString(data);

			NarrationCreatorData newNarration = new NarrationCreatorData(index, "", "");
			newNarration.Title = new NarrationCreatorToken(GetPlaceholderTitle(), true);
			newNarration.Segments.Add(new NarrationCreatorToken(GetPlaceholderSegment(), false));
			if (index < _narration.Count)
			{
				_narration.Insert(index, newNarration);
			}
			else
			{
				_narration.Add(newNarration);
			}			
		}

		public void RemovePOINarration(int index, string data)
		{
			if (_narration != null) return;
			_narration = new List<NarrationCreatorData>();

			LoadNarrationString(data);
			_narration.RemoveAt(index);
		}

		public void RemoveAllPOINarration()
		{
			if (_narration != null) return;
			_narration = new List<NarrationCreatorData>();
		}

		public void LoadNarrationString(string data)
		{
			string finalNarration = data;			
			if ((data == null) || (data.Length == 0))
			{
				finalNarration = "<narrations></narrations>";
			} 
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(finalNarration);

			XmlNodeList narrationData = xmlDoc.GetElementsByTagName("narration");
			if ((narrationData != null) && (narrationData.Count > 0))
			{
				foreach (XmlNode narrationEntry in narrationData)
				{
					int idToken = int.Parse(narrationEntry.Attributes["id"].Value);
					string startEvent = ((narrationEntry.Attributes["startEvent"] != null)?narrationEntry.Attributes["startEvent"].Value:"");
					string endEvent = ((narrationEntry.Attributes["endEvent"] != null)?narrationEntry.Attributes["endEvent"].Value:"");

					NarrationCreatorData newNarration = new NarrationCreatorData(idToken, startEvent, endEvent);

					XmlNodeList tokenTitles = narrationEntry.SelectNodes("./title");
					foreach (XmlNode titleToken in tokenTitles)
					{
						newNarration.Title = new NarrationCreatorToken(titleToken, true);
					}

					XmlNodeList tokensNarration = narrationEntry.SelectNodes("./token");
					foreach (XmlNode narrationToken in tokensNarration)
					{
						newNarration.Segments.Add(new NarrationCreatorToken(narrationToken, false));
					}
					_narration.Add(newNarration);
				}
			}
		}
		public override string ToString()
		{
			string output = "";
			for(int i = 0; i < _narration.Count; i++)
			{
				output += "\n" + _narration[i].ToString();
			}
			return output;
		}

		public string ToXML()
		{
			string output = "<narrations>";
			for(int i = 0; i < _narration.Count; i++)
			{
				output += "\n" + _narration[i].ToXML(i);
			}
			output += "</narrations>";
			return output;
		}
    }
}
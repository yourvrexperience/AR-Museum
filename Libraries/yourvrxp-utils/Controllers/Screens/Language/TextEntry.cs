using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace yourvrexperience.Utils
{
	public class TextEntry
	{
		private string _id;
		private Hashtable _texts;
		private Hashtable _modified;

		public string Id
		{
			get { return _id; }
		}

		public TextEntry(string id, XmlNodeList textEntryParameters)
		{
			_id = id;
			_texts = new Hashtable();
			_modified = new Hashtable();
			foreach (XmlNode itemParameter in textEntryParameters)
			{
				if (_texts[itemParameter.Name] != null)
				{
					Debug.LogError("REPEATED TAG[" + _id + "]");
				}
				else
				{
					_texts.Add(itemParameter.Name, itemParameter.InnerText);
					_modified.Add(itemParameter.Name, false);
				}
			}
		}

		public TextEntry(params string[] parameters)
		{
			_texts = new Hashtable();
			for (int i = 0; i < parameters.Length; i+=2)
			{
				_texts.Add(parameters[i], parameters[i+1]);
			}
		}

		public string GetText(string language)
		{
			if (_texts[language] != null)
			{
				string buffer = (string)_texts[language];
				string output = "";
				int indexNewLine = -1;
				while ((indexNewLine = buffer.IndexOf("#br#")) != -1)
				{
					output += buffer.Substring(0, indexNewLine) + '\n';
					buffer = buffer.Substring(indexNewLine + 4, buffer.Length - (indexNewLine + 4));
				}
				output += buffer;
				return output;
			}
			else
			{
				return null;
			}
		}

		public void SetText(string language, string value)
		{
			if (_texts[language] != null)
			{
				_texts[language] = value;
				_modified[language] = true;
			}
		}

		public bool GetModified(string language)
		{
			if (_modified[language] != null)
			{
				return (bool)_modified[language];
			}
			else
			{
				return false;
			}
		}

		public void ResetModified(string language)
		{
			if (_modified[language] != null)
			{
				_modified[language] = false;
			}
		}

		public string GetXML()
		{
			string output = "";
			foreach (DictionaryEntry entry in _texts)
			{
				output += "\n\t<" + entry.Key + ">";
				output += _texts[entry.Key];
				output += "</" + entry.Key + ">";
			}
			return output;
		}
	}
}
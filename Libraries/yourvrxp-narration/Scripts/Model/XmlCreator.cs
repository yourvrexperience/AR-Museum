using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml.Linq;
using yourvrexperience.Utils;

namespace yourvrexperience.Narration
{
	public class XmlCreator
	{
		public const float AVERAGE_TIME_FOR_WORD = 5.5f;
		public const float AVERAGE_TIME_FOR_CHARACTER = 0.5f;
		public const string AI_VOICE_CODE_IDENTIFIER = "AI_VOICE_";

		public static string GetNarrationData(string narrationText, string codeLanguage)
		{
			string[] englishParts = narrationText.Split(new[] { ". " }, StringSplitOptions.None);

			var narrations = new XElement("narrations");

			var narration = new XElement("narration",
				new XAttribute("id", "1"),
				new XAttribute("startEvent", ""),
				new XAttribute("endEvent", ""),
				new XAttribute("title", "")
			);

			var languagesTitle = new XElement("languages");
			languagesTitle.Add(new XElement(LanguageController.CodeLanguageCatalan, ""));
			languagesTitle.Add(new XElement(LanguageController.CodeLanguageEnglish, ""));
			languagesTitle.Add(new XElement(LanguageController.CodeLanguageSpanish, ""));
			languagesTitle.Add(new XElement(LanguageController.CodeLanguageFrench, ""));

			var languagesAudio = new XElement("audios");
			languagesAudio.Add(new XElement(LanguageController.CodeLanguageCatalan, ""));
			languagesAudio.Add(new XElement(LanguageController.CodeLanguageEnglish, ""));
			languagesAudio.Add(new XElement(LanguageController.CodeLanguageSpanish, ""));
			languagesAudio.Add(new XElement(LanguageController.CodeLanguageFrench, ""));

			var tokenTitle = new XElement("title",
					new XAttribute("id", "0"),
					new XAttribute("audioclip", ""),
					new XAttribute("startEvent", ""),
					new XAttribute("endEvent", ""),
					languagesTitle,
					languagesAudio
				);			
			narration.Add(tokenTitle);

			for (int i = 0; i < englishParts.Length; i++)
			{
				string sentence = englishParts[i];
				string[] words = sentence.Split(new[] { " " }, StringSplitOptions.None);

				var languagesToken = new XElement("languages");
				languagesToken.Add(new XElement(LanguageController.CodeLanguageCatalan, englishParts[i]));
				languagesToken.Add(new XElement(LanguageController.CodeLanguageEnglish, englishParts[i]));
				languagesToken.Add(new XElement(LanguageController.CodeLanguageSpanish, englishParts[i]));
				languagesToken.Add(new XElement(LanguageController.CodeLanguageFrench, englishParts[i]));

				var languagesAudios = new XElement("audios");
				languagesAudios.Add(new XElement(LanguageController.CodeLanguageCatalan, ""));
				languagesAudios.Add(new XElement(LanguageController.CodeLanguageEnglish, ""));
				languagesAudios.Add(new XElement(LanguageController.CodeLanguageSpanish, ""));
				languagesAudios.Add(new XElement(LanguageController.CodeLanguageFrench, ""));

				var token = new XElement("token",
					new XAttribute("id", i.ToString()),
					new XAttribute("audioclip", AI_VOICE_CODE_IDENTIFIER + i),
					new XAttribute("wordtime", AVERAGE_TIME_FOR_WORD.ToString()),
					new XAttribute("time", (words.Length * AVERAGE_TIME_FOR_CHARACTER).ToString()),
					new XAttribute("startEvent", ""),
					new XAttribute("endEvent", ""),
					languagesToken,
					languagesAudios
				);

				narration.Add(token);
			}

			narrations.Add(narration);

			var xmlDoc = new XDocument(narrations);
			return xmlDoc.ToString();
		}
	}
}
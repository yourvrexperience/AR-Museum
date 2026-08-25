using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SymSpell;
using static yourvrexperience.Utils.SymSpellController;

namespace yourvrexperience.Utils
{
    public class TestSpelling : MonoBehaviour
    {
        [SerializeField] private TMP_InputField textInput;
        [SerializeField] private TextMeshProUGUI textLog;


        void Start()
        {
            textLog.text = "";
            textInput.onTextSelection.AddListener(OnSelectText);

            SymSpellController.Instance.Initialize(LanguageController.CodeLanguageEnglish);

            SystemEventController.Instance.Event += OnSystemEvent;
        }

        private void OnDestroy()
        {
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
        }

        private void OnSelectText(string value, int start, int end)
        {
            if (end < start)
            {
                int tmp = start;
                start = end;
                end = tmp;
            }
            string selectedText = value.Substring(start, end - start);
            selectedText = yourvrexperience.Utils.Utilities.RemoveXmlTags(selectedText);
            WordSuggestion[] suggestionForWrongWord = SymSpellController.Instance.GetSuggestionForWord(selectedText);
            if (suggestionForWrongWord != null)
            {
                string output = "++SUGGESTED WORDS=";
                for (int i = 0; i < suggestionForWrongWord.Length; i++)
                {
                    output += suggestionForWrongWord[i].Word + ",";
                }
                Debug.LogError(output);
            }
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent == SymSpellController.EventSymSpellControllerClearUnderlines)
            {
                textInput.text = Regex.Replace(textInput.text, @"<\/?u>", "");
            }
            if (nameEvent == SymSpellController.EventSymSpellControllerUnderlineError)
            {
                WordChecked wordError = (WordChecked)parameters[0];
                string currentText = textInput.text;

                int finalStart = currentText.IndexOf(wordError.WordOriginal, wordError.Start);
                string part1 = currentText.Substring(0, finalStart);

                int finalEnd = finalStart + wordError.Word.Length;
                string part2 = textInput.text.Substring(finalEnd, textInput.text.Length - finalEnd);

                string totalText = part1 + "<u>" + wordError.WordOriginal + "</u>" + part2;
                textInput.text = totalText;
            }
        }

        private void Update()
        {
            if (textInput.isFocused && Input.GetKeyDown(KeyCode.Return))
            {
                SymSpellController.Instance.AnalyseText(textInput.text);
            }
        }
    }
}
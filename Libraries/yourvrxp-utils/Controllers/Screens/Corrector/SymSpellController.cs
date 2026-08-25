using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SymSpell;

namespace yourvrexperience.Utils
{

    public class SymSpellController : MonoBehaviour
    {
        public const string EventSymSpellControllerClearUnderlines = "EventSymSpellControllerClearUnderlines";
        public const string EventSymSpellControllerUnderlineError = "EventSymSpellControllerUnderlineError";
        public const string EventSymSpellControllerPackWordExceptions = "EventSymSpellControllerPackWordExceptions";

        public const string Separator = ";";

        private static SymSpellController _instance;

        public static SymSpellController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(SymSpellController)) as SymSpellController;
                }
                return _instance;
            }
        }

        public class WordSuggestion : IEqualityComparer<WordSuggestion>
        {
            public string Word;
            public float Distance;

            public WordSuggestion(string word, float distance)
            {
                Word = word;
                Distance = distance;
            }

            public bool Equals(WordSuggestion x, WordSuggestion y)
            {
                return x.Word == y.Word;
            }

            public int GetHashCode(WordSuggestion value)
            {
                return value.GetHashCode();
            }
        }

        public class WordChecked
        {
            public string Word;
            public string WordOriginal;
            public int Start;
            public string SentenceWrong;
            public int Valid = -1;
            public WordSuggestion[] Suggestions;

            public WordChecked(string word, string wordOriginal)
            {
                Word = word;
                WordOriginal = wordOriginal;
            }
        }

        private const int MaxEditDistanceLookup = 2; //max edit distance per lookup (maxEditDistanceLookup<=maxEditDistanceDictionary)
        private const Verbosity SuggestionVerbosity = SymSpell.Verbosity.Closest; //Top, Closest, All

        [SerializeField] private TextAsset dictionaryEnglish;
        [SerializeField] private TextAsset dictionarySpanish;
        [SerializeField] private TextAsset dictionaryGerman;
        [SerializeField] private TextAsset dictionaryFrench;
        [SerializeField] private TextAsset dictionaryItalian;
        [SerializeField] private TextAsset dictionaryRussian;

        private SymSpell _symSpell;
        private List<WordChecked> _currentWordsParagraph = new List<WordChecked>();
        private string _currentParagraph = "";
        private List<string> _wordsExceptions = new List<string>();
        private int _currentNumberErrors = 0;

        public void Initialize(string languageCode)
        {
            int initialCapacity = 82765;
            _symSpell = new SymSpell(initialCapacity, MaxEditDistanceLookup);
            int termIndex = 0; //column of the term in the dictionary text file
            int countIndex = 1; //column of the term frequency in the dictionary text file

            switch (languageCode)
            {
                case LanguageController.CodeLanguageEnglish:     
                    _symSpell.LoadDictionary(dictionaryEnglish.bytes, termIndex, countIndex);
                    break;

                case LanguageController.CodeLanguageSpanish:
                    _symSpell.LoadDictionary(dictionarySpanish.bytes, termIndex, countIndex);
                    break;

                case LanguageController.CodeLanguageGerman:
                    _symSpell.LoadDictionary(dictionaryGerman.bytes, termIndex, countIndex);
                    break;

                case LanguageController.CodeLanguageFrench:
                    _symSpell.LoadDictionary(dictionaryFrench.bytes, termIndex, countIndex);
                    break;

                case LanguageController.CodeLanguageItalian:
                    _symSpell.LoadDictionary(dictionaryItalian.bytes, termIndex, countIndex);
                    break;

                case LanguageController.CodeLanguageRussian:
                    _symSpell.LoadDictionary(dictionaryRussian.bytes, termIndex, countIndex);
                    break;
            }
        }

        private void ClearMemory()
        {
            _currentWordsParagraph = new List<WordChecked>();
            _currentParagraph = "";
            _currentNumberErrors = 0;
        }

        public void UnpackWordExceptions(string data)
        {
            if ((data != null) && (data.Length > 0))
            {
                string[] exceptionsTemp = data.Split(Separator);
                _wordsExceptions = new List<string>();
                foreach (string exception in exceptionsTemp)
                {
                    if ((exception != null) && (exception.Length > 0))
                    {
                        _wordsExceptions.Add(exception);
                    }
                }
            }
        }

        public string PackWordExceptions()
        {
            string output = "";
            foreach(string exception in _wordsExceptions)
            {
                output += exception + Separator;
            }
            return output;
        }

        public void AddException(string wordValue)
        {
            string word = wordValue;
            if ((word != null) && (word.Length > 1) && (word.Length < 20))
            {
                if (!_wordsExceptions.Contains(word))
                {
                    _wordsExceptions.Add(word);
                }
            }
            SystemEventController.Instance.DispatchSystemEvent(EventSymSpellControllerPackWordExceptions, PackWordExceptions());
        }

        public WordSuggestion[] GetSuggestionForWord(string word)
        {
            string wordLower = word.ToLower();
            foreach (WordChecked wordChecked in _currentWordsParagraph)
            {
                if (wordChecked.Word.Equals(wordLower))
                {
                    return wordChecked.Suggestions;
                }
            }
            return null;
        }

        public void ReportWordErrors()
        {
            foreach (WordChecked wordChecked in _currentWordsParagraph)
            {
                if (wordChecked.Valid == 0)
                {
                    SystemEventController.Instance.DispatchSystemEvent(EventSymSpellControllerUnderlineError, wordChecked);
                }
            }
        }

        public void AnalyseText(string textOrigin)
        {
            int newNumberErrors = 0;
            List<WordChecked> wordsError = new List<WordChecked>();            
            string formattedText = textOrigin.Replace("?", ".");
            formattedText = formattedText.Replace("�", string.Empty);
            formattedText = formattedText.Replace("�", string.Empty);
            formattedText = formattedText.Replace("�", string.Empty);
            formattedText = formattedText.Replace("!", ".");            
            string paragraph = yourvrexperience.Utils.Utilities.RemoveXmlTags(formattedText);

            ClearMemory();

            if ((paragraph.IndexOf(' ') != -1) || (paragraph.IndexOf(',') != -1) || (paragraph.IndexOf('.') != -1)
                || (paragraph.IndexOf('\"') != -1) || (paragraph.IndexOf('\n') != -1) || (paragraph.IndexOf(':') != -1)
                || (paragraph.IndexOf('�') != -1) || (paragraph.IndexOf('-') != -1) || (paragraph.IndexOf(';') != -1)
                || (paragraph.IndexOf('%') != -1) || (paragraph.IndexOf(')') != -1) || (paragraph.IndexOf('(') != -1)
                || (paragraph.IndexOf('_') != -1))
            {
                string[] splittedFull = paragraph.Split(' ', ',', '.', ':', ';', '\n', '\"', '-', '�', '%', '(', ')', '_');
                List<string> splittedListFull = splittedFull.ToList<string>();
                splittedListFull.RemoveAt(splittedListFull.Count - 1);
                string[] splitted = splittedListFull.ToArray();

                for (int i = 0; i < splitted.Length; i++)
                {
                    string sword = splitted[i];
                    sword = sword.Replace("�", "'");
                    _currentWordsParagraph.Add(new WordChecked(sword.ToLower(), splitted[i]));
                }

                int characterCounter = 0;
                foreach (WordChecked word in _currentWordsParagraph)
                {
                    if ((word.Word == null) || (word.Word.Length == 0))
                    {
                        word.Valid = 1;
                    }
                    else
                    {
                        characterCounter += word.Word.Length + 1;

                        if (word.Valid == -1)
                        {
                            float isNumber = 0;
                            if (float.TryParse(word.Word, out isNumber))
                            {
                                word.Valid = 1;
                            }
                            else
                            {
                                List<SuggestItem> suggestions = _symSpell.Lookup(word.Word, SuggestionVerbosity, MaxEditDistanceLookup);
                                List<SuggestItem> sortedSuggestions = suggestions.OrderByDescending(item => item.distance)
                                                                .ThenByDescending(item => item.count)
                                                                .ToList();

                                bool isValid = false;
                                foreach (var suggestion in sortedSuggestions)
                                {
                                    if (suggestion.distance == 0)
                                    {
                                        isValid = true;
                                    }
                                }

                                if (_wordsExceptions.Contains(word.WordOriginal))
                                {
                                    word.Valid = 1;
                                }
                                else
                                {
                                    if (isValid)
                                    {
                                        word.Valid = 1;
                                    }
                                    else
                                    {
                                        word.Valid = 0;
                                        List<WordSuggestion> suggestedWords = new List<WordSuggestion>();
                                        List<float> suggestedDistance = new List<float>();
                                        foreach (var suggestion in sortedSuggestions)
                                        {
                                            suggestedWords.Add(new WordSuggestion(suggestion.term, suggestion.distance));
                                        }
                                        word.Suggestions = suggestedWords.ToArray();
                                        word.Start = paragraph.IndexOf(word.WordOriginal, characterCounter - word.WordOriginal.Length);
                                        wordsError.Add(word);
                                    }
                                }
                            }
                        }                        
                    }
                }
            }

            if (wordsError.Count > 0)
            {
                for (int i = 0; i < wordsError.Count; i++)
                {
                    WordChecked wordError = wordsError[i];
                    string word = wordError.WordOriginal;
                    if ((word != null) && (word.Length > 0))
                    {
                        newNumberErrors++;

                        string[] sentences = paragraph.Split(new char[] { '.', '!', '?', ';', '\n', '\"', '-', '�', '(', ')', '_' }, StringSplitOptions.RemoveEmptyEntries);
                        string sentenceContainingWord = sentences.FirstOrDefault(sentence =>
                            sentence.Split(' ', ',', ':', ';', '\n', '\"', '-', '�', '(', ')', '_').Contains(word, StringComparer.OrdinalIgnoreCase));

                        if ((sentenceContainingWord != null) && (sentenceContainingWord.Length > 0))
                        {
                            var suggestions = _symSpell.LookupCompound(sentenceContainingWord, MaxEditDistanceLookup);

                            List<WordSuggestion> finalSuggestions = new List<WordSuggestion>();
                            foreach (var suggestion in suggestions)
                            {
                                // Debug.LogError(suggestion.term + " " + suggestion.distance.ToString() + " " + suggestion.count.ToString("N0"));
                                string sentenceSuggested = suggestion.term;
                                foreach (WordSuggestion wordSuggestion in wordError.Suggestions)
                                {
                                    if (sentenceSuggested.IndexOf(wordSuggestion.Word.ToLower()) != -1)
                                    {
                                        finalSuggestions.Add(wordSuggestion);
                                    }
                                }
                            }

                            foreach (WordSuggestion wordSuggestion in wordError.Suggestions)
                            {
                                if (!finalSuggestions.Contains(wordSuggestion))
                                {
                                    finalSuggestions.Add(wordSuggestion);
                                }
                            }

                            wordError.Suggestions = finalSuggestions.ToArray<WordSuggestion>();
                            wordError.SentenceWrong = sentenceContainingWord.ToLower().Trim();
                        }
                    }
                }
            }
            SystemEventController.Instance.DispatchSystemEvent(EventSymSpellControllerClearUnderlines);
            if (_currentNumberErrors != newNumberErrors)
            {
                _currentNumberErrors = newNumberErrors;
                ReportWordErrors();
            }
        }
    }
}
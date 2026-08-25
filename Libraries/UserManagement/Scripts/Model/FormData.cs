using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.UserManagement
{
    [CreateAssetMenu(menuName = "Game/FormData")]
    public class FormData : ScriptableObject
    {
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

        [SerializeField] private string[] questions;

        private int _indexQuestion = 0;
        private string[] _responses;

        public int IndexQuestion
        {
            get { return _indexQuestion; }
            set { _indexQuestion = value; }
        }
        
        public string[] Questions
        {
            get { return questions; }
        }

        public void Initialize()
        {
            _responses = new string[questions.Length];
            _indexQuestion = 0;
        }

        public bool RegisterResponse(string response)
        {
            _responses[_indexQuestion] = response;
            _indexQuestion++;
            return (_indexQuestion >= questions.Length);
        }

        public bool AreMoreQuestions()
        {
            return (_indexQuestion + 1 < questions.Length);
        }

        public string PackJSONData()
        {
            List<QuestionForm> questionsResponded = new List<QuestionForm>();
            for (int i = 0; i < _responses.Length; i++)
            {
                questionsResponded.Add(new QuestionForm(i, _responses[i]));
            }
            SerializedQuestionsForm serializedQuestions = new SerializedQuestionsForm();
            serializedQuestions.Questions = questionsResponded.ToArray();
            return JsonUtility.ToJson(serializedQuestions, true);
        }
    }
}
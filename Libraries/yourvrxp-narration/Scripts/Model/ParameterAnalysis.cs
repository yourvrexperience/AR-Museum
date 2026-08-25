namespace yourvrexperience.Narration
{
    [System.Serializable]
    public class ParameterAnalysis
    {
        public string parameter;
        public string type;
        public string value;

        public ParameterAnalysis(string parameter, string type, object value)
        {
            this.parameter = parameter;
            this.type = type;
            this.value = value.ToString();
        }
    }
}
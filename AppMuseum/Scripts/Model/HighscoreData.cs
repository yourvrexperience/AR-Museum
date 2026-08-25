namespace yourvrexperience.template6dof
{
	[System.Serializable]
	public class HighscoreData
	{
		public int Score;
		public int Time;

		public HighscoreData(int score, int time)
		{
			Score = score;
			Time = time;
		}

        public HighscoreData(HighscoreData highscore)
		{
			Score = highscore.Score;
			Time = highscore.Time;
		}
    }
}
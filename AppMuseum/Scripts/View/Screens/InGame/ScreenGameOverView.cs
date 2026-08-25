using TMPro;
using UnityEngine;
using yourvrexperience.Narration;
using yourvrexperience.Utils;
using yourvrexperience.VR;

namespace yourvrexperience.template6dof
{
	public class ScreenGameOverView : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenGameOverView";

		public const float TotalTimeAnimationScore = 3;

		[SerializeField] private TextMeshProUGUI title;
		[SerializeField] private TextMeshProUGUI description;
		[SerializeField] private TextMeshProUGUI scoreTitle;
		[SerializeField] private TextMeshProUGUI scoreValue;
		[SerializeField] private TextMeshProUGUI timeTitle;
		[SerializeField] private TextMeshProUGUI timeValue;

		private float _timer = 0;
		private int _score = 0;

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			title.text = LanguageController.Instance.GetText("screen.level.game.over.title");
			description.text = LanguageController.Instance.GetText("screen.level.game.over.description");

			scoreTitle.text = LanguageController.Instance.GetText("screen.hud.score");
			timeTitle.text = LanguageController.Instance.GetText("screen.hud.time");

			scoreValue.text = "0";
			_score = GameLevelData.Instance.CurrentScore;
			timeValue.text = yourvrexperience.Utils.Utilities.GetFormattedTimeMinutes(GameLevelData.Instance.CurrentTime);

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR	|| ENABLE_NREAL
			RefocusScreen refocusComponent = this.gameObject.GetComponent<RefocusScreen>();
			if (refocusComponent == null)
			{
				refocusComponent = this.gameObject.AddComponent<RefocusScreen>();
			}
			refocusComponent.Activate(VRInputController.Instance.Camera, ScreenController.Instance.DistanceScreen, 1, 0.4f);
#endif			
		}

 		void Update()
        {
			if (_timer != -1)
			{
				_timer += Time.deltaTime;
				if (_timer < TotalTimeAnimationScore)
				{
					float progressScore = ((float)_score * (_timer / TotalTimeAnimationScore));
					scoreValue.text = ((int)progressScore).ToString();
				}
				else
				{
					_timer = -1;
				}
			}
        }		
	}
}
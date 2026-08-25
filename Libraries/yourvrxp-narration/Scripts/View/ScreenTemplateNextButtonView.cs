using yourvrexperience.Utils;
using static yourvrexperience.Narration.NarrationController;

namespace yourvrexperience.Narration
{
	public class ScreenTemplateNextButtonView : ScreenNarrationNextButtonView, IScreenView
	{		
		public const string ScreenName = "ScreenTemplateNextButtonView";

		public override string NameScreen 
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);
		}

		protected override void UpdateIconButton(TypeActionNext action)
        {
			base.UpdateIconButton(action);
			switch (_action)
            {
				case TypeActionNext.Play:
					break;
					
				case TypeActionNext.Pause:
					break;

				case TypeActionNext.Walk:
					break;
			}
        }

		protected override void OnButtonPause()
		{
		}

		protected override void OnButtonAIInteraction()
		{
		}

		protected override void OnSkipNext()
		{
		}

        protected override void OnRestart()
        {
        }

		public override void Destroy()
		{
			base.Destroy();
		}
		
		protected override void OnSystemEvent(string nameEvent, object[] parameters)
        {
			base.OnSystemEvent(nameEvent, parameters);
		}

		protected override void Update()
        {
			base.Update();
		}
	}
}
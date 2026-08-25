
using System;
using TMPro;
using UnityEngine;

namespace yourvrexperience.Utils
{
    public class TimeCounterDisplay : MonoBehaviour
    {
		[SerializeField] private TextMeshProUGUI Text;

        private int _totalTime = -1;
        private float _timeAcum = 0;

        public void ResetText()
        {
            Text.text = "";
        }

        public void Activate(bool shouldReset = true)
		{
			if (shouldReset || _totalTime == -1)
            {
                _totalTime = 0;
            }
            _timeAcum = 0;
            Text.text = LanguageController.Instance.GetText("text.time") + " " + yourvrexperience.Utils.Utilities.GetFormattedTimeMinutes(_totalTime);
        }

        public void Deactivate()
        {
            _totalTime = -1;
        }

        private void Update()
        {
            if (_totalTime >= 0)
            {
                _timeAcum += Time.deltaTime;
                if (_timeAcum >= 1)
                {
                    _timeAcum -= 1;
                    _totalTime += 1;
                    Text.text = LanguageController.Instance.GetText("text.time") + " " + yourvrexperience.Utils.Utilities.GetFormattedTimeMinutes(_totalTime);
                }
            }
        }
    }
}
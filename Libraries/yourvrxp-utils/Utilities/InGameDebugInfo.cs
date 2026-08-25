
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.Utils
{
    public class InGameDebugInfo : MonoBehaviour
    {
		private static InGameDebugInfo _instance;
		public static InGameDebugInfo Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType<InGameDebugInfo>();
				}
				return _instance;
			}
		}

		public Text Information;

        public void SetText(string message)
        {
            Information.text = message;
        }
    }
}
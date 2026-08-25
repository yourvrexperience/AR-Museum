using UnityEngine;

namespace yourvrexperience.template6dof
{
	[System.Serializable]
	public class EasterEggIcon : MonoBehaviour
	{
		public GameObject IconDisabled;
		public GameObject IconEnabled;

		public void Activate()
        {
			IconDisabled.SetActive(false);
			IconEnabled.SetActive(true);
		}

		public void DeActivate()
		{
			IconDisabled.SetActive(true);
			IconEnabled.SetActive(false);
		}

		public void Hide()
		{
			IconDisabled.SetActive(false);
			IconEnabled.SetActive(false);
		}
	}
}
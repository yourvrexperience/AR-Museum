using yourvrexperience.Utils;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using yourvrexperience.Narration;
using yourvrexperience.Networking;

namespace yourvrexperience.template6dof
{
	public class PanelNetworkPlayersView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI titleScreen;
		[SerializeField] private TextMeshProUGUI numberScreen;

		private bool _shouldShow;

		void Start()
		{
			_shouldShow = false;
			if (MainController.Instance.IsMultiplayer)
			{
				if (NetworkController.Instance.IsServer)
				{
					_shouldShow = true;
				}
			}
			this.gameObject.SetActive(_shouldShow);

			if (_shouldShow)
			{
				titleScreen.text = LanguageController.Instance.GetText("panel.users.title");
				numberScreen.text = (NetworkController.Instance.Connections.Count - 1).ToString();
				NetworkController.Instance.NetworkEvent += OnNetworkEvent;
			}

			if (MainController.Instance.EnableEditionPOIs)
			{
				this.gameObject.SetActive(false);
			}
		}

        private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
        {
            if (nameEvent.Equals(NetworkController.EventNetworkControllerConfirmationConnectionWithRoom))
			{
				numberScreen.text = (NetworkController.Instance.Connections.Count - 1).ToString();
			}
        }

        void OnDestroy()
		{
			if (_shouldShow)
			{
				if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
			}
		}
	}
}

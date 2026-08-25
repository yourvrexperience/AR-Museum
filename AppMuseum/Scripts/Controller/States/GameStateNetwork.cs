using System;
using UnityEngine;
using UnityEngine.Assertions;
using yourvrexperience.Narration;
using yourvrexperience.Networking;
using yourvrexperience.Utils;

namespace yourvrexperience.template6dof
{
	public class GameStateNetwork : IGameState
    {
		private bool _hasStartedSession = false;
		private string _nameRoom = "NameRoom";

		public void Initialize()
		{
			UIEventController.Instance.Event += OnUIEvent;
			SystemEventController.Instance.Event += OnSystemEvent;
			NetworkController.Instance.NetworkEvent += OnNetworkEvent;

			ScreenController.Instance.CreateScreen(ScreenNetworkView.ScreenName, true, false);

			if (NetworkController.Instance.IsConnected)
			{
				NetworkController.Instance.Disconnect();
			}
		}

        public void Destroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
		}

        private void OnUIEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(ScreenNetworkView.EventScreenNetworkConnect))
			{
				_nameRoom = (string)parameters[0];
				MainController.Instance.NumberClients = -1;
				MainController.Instance.RoomName = _nameRoom;
				NetworkController.Instance.Initialize();
				MainController.Instance.ChangeGameState(MainController.StatesGame.Connecting);
			}
			if (nameEvent.Equals(ScreenNetworkView.EventScreenNetworkBack))
			{
				if (GameLevelData.Instance.TotalAreas > 1)
				{
					MainController.Instance.ChangeGameState(MainController.StatesGame.Floor);
				}
				else
				{
					MainController.Instance.ChangeGameState(MainController.StatesGame.MainMenu);
				}
			}            
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
        {

        }
		
		private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
        {
			if (nameEvent.Equals(NetworkController.EventNetworkControllerListRoomsUpdated))
			{
				if (!_hasStartedSession)
				{
					_hasStartedSession = true;
					NetworkController.Instance.CreateRoom(_nameRoom, 20);
				}
			}
			if (nameEvent.Equals(NetworkController.EventNetworkControllerConnectionWithRoom))
			{
				MainController.Instance.ChangeGameState(MainController.StatesGame.Loading);
			}
        }

		public void Run()
		{
		}
	}
}
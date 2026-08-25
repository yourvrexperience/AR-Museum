using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.Utils
{
	public class InitialPositionPlayer : MonoBehaviour
	{
		public const string EventNetworkInitialRequestPosition = "EventNetworkInitialRequestPosition";
		public const string EventNetworkInitialPositionStarted = "EventNetworkInitialPositionStarted";
		public const string EventNetworkInitialPositionPlayerConnected = "EventNetworkInitialPositionPlayerConnected";
		public const string EventNetworkInitialPositionPlayerResponse = "EventNetworkInitialPositionPlayerResponse";
		
		[SerializeField] private GameObject[] StartingPositions;

		void Awake()
		{
			foreach (GameObject startingPosition in StartingPositions)
			{
				startingPosition.SetActive(false);
			}
		}

		void Start()
		{
			SystemEventController.Instance.Event += OnSystemEvent;
			SystemEventController.Instance.DispatchSystemEvent(InitialPositionPlayer.EventNetworkInitialPositionStarted);
		}

		void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(EventNetworkInitialRequestPosition))
			{
				SystemEventController.Instance.DispatchSystemEvent(InitialPositionPlayer.EventNetworkInitialPositionStarted);
			}
			if (nameEvent.Equals(EventNetworkInitialPositionPlayerConnected))
			{
				GameObject originPlayer = (GameObject)parameters[0];
				int numberOfPlayers = (int)parameters[1];
				int finalIndex = numberOfPlayers % StartingPositions.Length;
				Vector3 startingPosition = StartingPositions[finalIndex].transform.position;
				Quaternion startingRotation = StartingPositions[finalIndex].transform.rotation;
				SystemEventController.Instance.DispatchSystemEvent(EventNetworkInitialPositionPlayerResponse, originPlayer, startingPosition, startingRotation);
			}
		}
	}
}
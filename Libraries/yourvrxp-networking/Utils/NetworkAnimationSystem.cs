using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.Networking
{
	[RequireComponent(typeof(AnimatorSystem))]
	public class NetworkAnimationSystem : MonoBehaviour
	{		
		public const string EventNetworkAnimationSystemTrigger = "EventNetworkAnimationSystemTrigger";

		[SerializeField] private AnimatorSystem animatorSystem;
		private NetworkPrefab _networkPrefab;

		public AnimatorSystem AnimatorSystemComponent
		{
			get { return animatorSystem; }
		}
		public NetworkPrefab NetworkPrefabComponent
		{
			get {
				if (_networkPrefab == null)
				{
					_networkPrefab = this.GetComponent<NetworkPrefab>();
				}
				return _networkPrefab;
			}
		}

		void Start()
		{
			animatorSystem.AnimationEvent += OnAnimatorEvent;
			NetworkController.Instance.NetworkEvent += OnNetworkEvent;
		}

		void OnDestroy()
		{
			if (animatorSystem != null) animatorSystem.AnimationEvent -= OnAnimatorEvent;
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
		}

		private void OnAnimatorEvent(string triggerAnimation)
		{
			if (NetworkPrefabComponent.NetworkGameIDView.AmOwner())
			{
				NetworkController.Instance.DispatchNetworkEvent(EventNetworkAnimationSystemTrigger, -1, -1, NetworkPrefabComponent.NetworkGameIDView.GetViewID(), triggerAnimation);
			}
		}

		protected void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			if (nameEvent.Equals(EventNetworkAnimationSystemTrigger))
			{
				if (!NetworkPrefabComponent.NetworkGameIDView.AmOwner())
				{
					int viewID = (int)parameters[0];
					if (NetworkPrefabComponent.NetworkGameIDView.GetViewID() == viewID)
					{
						string triggerAnimation = (string)parameters[1];
						animatorSystem.ChangeAnimation(triggerAnimation);
					}
				}
			}
		}
	}
}
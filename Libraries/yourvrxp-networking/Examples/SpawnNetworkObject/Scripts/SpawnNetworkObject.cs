using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Networking;
using yourvrexperience.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.Networking
{
	public class SpawnNetworkObject : BasicConnection
	{
		public RectTransform UserArea;
		public GameObject Center;
		public GameObject SpawnPrefab;
		public GameObject BulletPrefab;

		private int _counterBullet = 0;

		protected override void OnSystemEvent(string nameEvent, object[] parameters)
		{
			base.OnSystemEvent(nameEvent, parameters);

			if (nameEvent.Equals(PlayerNetwork.EventPlayerNetworkStarted))
			{
				PlayerNetwork newPlayer = (PlayerNetwork)parameters[0];
				bool ownedPlayer = (bool)parameters[1];
				newPlayer.transform.parent = UserArea.transform;

				if (ownedPlayer)
				{
					newPlayer.transform.position = Center.transform.position;
					if (NetworkController.Instance.IsServer)
					{
						newPlayer.PlayerColor = Color.red;
					}
					else
					{
						newPlayer.PlayerColor = Color.green;
					}
				}
			}
		}

		protected override void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			base.OnNetworkEvent(nameEvent, originNetworkID, targetNetworkID, parameters);

			if (nameEvent.Equals(NetworkController.EventNetworkControllerConnectionWithRoom))
			{
				NetworkController.Instance.CreateNetworkPrefab(false, SpawnPrefab.name, SpawnPrefab, SpawnPrefab.name, new Vector3(0, 0, 0), Quaternion.identity, 0);				
			}
			if (nameEvent.Equals(PlayerNetwork.EventPlayerNetworkAskBullet))
			{
				if (NetworkController.Instance.IsServer)
				{
					int playerOwnerBullet = (int)parameters[0];
					string colorBullet = (string)parameters[1];
					string localPositionBullet = (string)parameters[2];
					string worldPositionBullet = (string)parameters[3];
					string targetMouse = (string)parameters[4];
					NetworkController.Instance.DispatchNetworkEvent(PlayerNetwork.EventPlayerNetworkCreateBullet, -1, -1, _counterBullet++, playerOwnerBullet, colorBullet, localPositionBullet, worldPositionBullet, targetMouse);
				}
			}
			if (nameEvent.Equals(PlayerNetwork.EventPlayerNetworkCreateBullet))
			{
				int bulletID = (int)parameters[0];
				int playerOwnerBullet = (int)parameters[1];
				Color colorBullet = yourvrexperience.Utils.Utilities.UnpackColor((string)parameters[2]);
				Vector3 localPositionBullet = yourvrexperience.Utils.Utilities.DeserializeVector3((string)parameters[3]);
				Vector3 worldPositionBullet = yourvrexperience.Utils.Utilities.DeserializeVector3((string)parameters[4]);
				Vector3 targetMouse = yourvrexperience.Utils.Utilities.DeserializeVector3((string)parameters[5]);
				Vector2 realTargetMouse = Vector2.zero;
				RectTransformUtility.ScreenPointToLocalPointInRectangle(UserArea, targetMouse, null, out realTargetMouse);
				Vector2 forwardBullet = new Vector2(realTargetMouse.x, realTargetMouse.y) - new Vector2(localPositionBullet.x, localPositionBullet.y);
				forwardBullet.Normalize();
				GameObject bullet = Instantiate(BulletPrefab);
				bullet.transform.parent = UserArea.transform;
				bullet.GetComponent<Bullet>().Initialize(bulletID, playerOwnerBullet, worldPositionBullet, forwardBullet, colorBullet);
			}
			if (nameEvent.Equals(PlayerNetwork.EventPlayerNetworkImpact))
			{
				int bulletID = (int)parameters[1];
				Bullet[] bullets = GameObject.FindObjectsOfType<Bullet>();
				foreach (Bullet bullet in bullets)
				{
					if (bullet.Id == bulletID)
					{
						bullet.transform.parent = null;
						GameObject.Destroy(bullet.gameObject);
						return;
					}
				}
			}
			if (nameEvent.Equals(NetworkController.EventNetworkControllerDisconnected))
			{
				Bullet[] bullets = GameObject.FindObjectsOfType<Bullet>();
				foreach (Bullet bullet in bullets)
				{
					bullet.transform.parent = null;
					GameObject.Destroy(bullet.gameObject);
				}
			}
		}
	}
}
using yourvrexperience.Utils;
using UnityEngine;
using System;
using yourvrexperience.Networking;

namespace yourvrexperience.template6dof
{
	public class BulletsController : MonoBehaviour
	{
		public const string EventBulletsControllerFreeze = "EventBulletsControllerFreeze";
		public const string EventBulletsControllerRequestShoot = "EventBulletsControllerRequestShoot";
		public const string EventBulletsControllerRunShoot = "EventBulletsControllerRunShoot";

        private static BulletsController _instance;

        public static BulletsController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(BulletsController)) as BulletsController;
                }
                return _instance;
            }
        }

		[SerializeField] private GameObject bullet;

		private BulletView[] _bullets;

		private int _counterBullets = 0;

		public void Initialize(int poolSize)
		{	
			SystemEventController.Instance.Event += OnSystemEvent;

			_bullets = new BulletView[poolSize];
			for (int i = 0; i < poolSize; i++)
			{
				_bullets[i] = (Instantiate(bullet) as GameObject).GetComponent<BulletView>();
				_bullets[i].Initialize();
				_bullets[i].transform.parent = this.transform;
			}

			NetworkController.Instance.NetworkEvent += OnNetworkEvent;
		}

        void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
		}

		private void FreeBulletByID(int bulletID)
		{
			foreach (BulletView bullet in _bullets)
			{
				if (bullet.Id == bulletID) bullet.Free();
			}
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(MainController.EventMainControllerReleaseGameResources))
			{
				if (_instance != null)
				{
					_instance = null;
					foreach (BulletView bullet in _bullets)
					{
						GameObject.Destroy(bullet.gameObject);
					}
					GameObject.Destroy(this.gameObject);
				}			
			}
			if (nameEvent.Equals(EventBulletsControllerFreeze))
			{
				bool shouldFreeze = (bool)parameters[0];
				if (shouldFreeze)
				{
					FreezeBullets();
				}
				else
				{
					ResumeBullets();
				}
			}
        }

        private void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
        {
            if (nameEvent.Equals(EventBulletsControllerRequestShoot))
			{
				if (NetworkController.Instance.IsServer)
				{
					int playerID = (int)parameters[0];
					Vector3 position = (Vector3)parameters[1];
					Vector3 forward = (Vector3)parameters[2];
					float speed = (float)parameters[3];
					_counterBullets++;
					NetworkController.Instance.DispatchNetworkEvent(EventBulletsControllerRunShoot, -1, -1, _counterBullets, playerID, position, forward, speed);
				}
			}
			if (nameEvent.Equals(EventBulletsControllerRunShoot))
			{
				int bulletID = (int)parameters[0];
				int playerID = (int)parameters[1];
				Vector3 position = (Vector3)parameters[2];
				Vector3 forward = (Vector3)parameters[3];
				float speed = (float)parameters[4];
				ShootLocalBullet(position, forward, speed, playerID, bulletID);
			}
			if (nameEvent.Equals(EventBulletsControllerFreeze))
			{
				bool shouldFreeze = (bool)parameters[0];
				if (shouldFreeze)
				{
					FreezeBullets();
				}
				else
				{
					ResumeBullets();
				}
			}
        }

		public void ShootBullet(Vector3 position, Vector3 forward, float speed)
		{
			int playerID = -1;
			if (MainController.Instance.NumberClients == 1)
			{
				_counterBullets++;
				ShootLocalBullet(position, forward, speed, playerID, _counterBullets);
			}
			else
			{
				playerID = NetworkController.Instance.UniqueNetworkID;
				NetworkController.Instance.DispatchNetworkEvent(EventBulletsControllerRequestShoot, -1, -1, playerID, position, forward, speed);
			}
		}

		private void ShootLocalBullet(Vector3 position, Vector3 forward, float speed, int playerID, int bulletID)
		{
			foreach (BulletView bullet in _bullets)
			{
				if (bullet.IsFree())
				{
					bullet.Shoot(bulletID, playerID, position, forward, speed);
					return;
				}
			}
		}

		public void FreezeBullets()
		{
			foreach (BulletView bullet in _bullets)
			{
				if (!bullet.IsFree())
				{
					bullet.Freeze();
				}
			}
		}

		public void ResumeBullets()
		{
			foreach (BulletView bullet in _bullets)
			{
				if (!bullet.IsFree())
				{
					bullet.Resume();
				}
			}
		}
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Networking;
using yourvrexperience.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.Networking
{
	public class Bullet : MonoBehaviour
	{
		public const float SpeedBullet = 400;
		public const float LifeBullet = 4;

		public Image Background;

		private int _id = -1;
		private int _owner = -1;
		private Vector2 _direction = Vector2.zero;

		private float _timer = 0;

		public int Id 
		{
			get { return _id; }
		}
		public int Owner 
		{
			get { return _owner; }
		}

		public void Initialize(int id, int owner, Vector2 position, Vector2 forward, Color color)
		{
			_id = id;
			_owner = owner;
			this.transform.position = position;
			_direction = forward;
			Background.color = color;
		}

		void Update()
		{
			if (_owner != -1)
			{
				_timer += Time.deltaTime;
				if (_timer < LifeBullet)
				{
					Vector2 increment = _direction * SpeedBullet * Time.deltaTime;
					this.transform.position += new Vector3(increment.x, increment.y, 0);
				}
				else
				{
					_owner = -1;
					GameObject.Destroy(this.gameObject);
				}
			}
		}
	}
}
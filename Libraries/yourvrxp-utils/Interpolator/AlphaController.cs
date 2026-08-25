using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;

namespace yourvrexperience.Utils
{
	public class AlphaController : MonoBehaviour
	{
		private static AlphaController _instance;
		public static AlphaController Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(AlphaController)) as AlphaController;
					if (!_instance)
					{
						GameObject container = new GameObject();
						container.name = "AlphaController";
						_instance = container.AddComponent(typeof(AlphaController)) as AlphaController;
					}
				}
				return _instance;
			}
		}

		private List<AlphaData> _inteporlateObjects = new List<AlphaData>();
        private List<AlphaData> _inteporlateQueue = new List<AlphaData>();

        void OnDestroy()
		{
			Destroy();
		}

		public void Destroy()
		{
			if (_instance != null)
			{
				GameObject.Destroy(_instance);
			}
			_instance = null;
		}

		public bool Stop(GameObject actor)
		{
			for (int i = 0; i < _inteporlateObjects.Count; i++)
			{
                AlphaData item = _inteporlateObjects[i];
				if (item.GameActor == actor)
				{
					item.Destroy();
					_inteporlateObjects.RemoveAt(i);
					return true;
				}
			}
			return false;
		}

        public void Clear()
        {
            for (int i = 0; i < _inteporlateObjects.Count; i++)
            {
                AlphaData item = _inteporlateObjects[i];
                item.Destroy();
            }
            _inteporlateObjects.Clear();
        }

        public void Interpolate(GameObject actor, float origin, float goal, float time, bool setTargetWhenFinished = false, bool loop = false)
		{
            _inteporlateQueue.Add(new AlphaData(actor, origin, goal, time, 0, setTargetWhenFinished, loop));
		}

        void Update()
		{
            try
            {
                for (int i = 0; i < _inteporlateObjects.Count; i++)
                {
                    AlphaData itemData = _inteporlateObjects[i];
                    if (itemData.Inperpolate())
                    {
                        itemData.Destroy();
                        _inteporlateObjects.RemoveAt(i);
                        i--;
                    }
                }
            }
            catch (Exception err) { };
            for (int j = 0; j < _inteporlateQueue.Count; j++)
            {
                AlphaData newItem = _inteporlateQueue[j];
                bool found = false;
                for (int i = 0; i < _inteporlateObjects.Count; i++)
                {
                    AlphaData item = _inteporlateObjects[i];
                    if (item.GameActor == newItem.GameActor)
                    {
                        item.ResetData(newItem.GameActor.GetComponent<CanvasGroup>().alpha, newItem.Goal, newItem.TotalTime, 0);
                        found = true;
                    }
                }
                if (!found)
                {
                    _inteporlateObjects.Add(newItem);
                }
                else
                {
                    newItem.Destroy();
                    newItem = null;
                }
            }
            _inteporlateQueue.Clear();
        }
	}
}
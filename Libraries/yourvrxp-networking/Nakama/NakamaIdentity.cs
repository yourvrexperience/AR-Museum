using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if ENABLE_NAKAMA
using Nakama;
#endif
using UnityEngine;

namespace yourvrexperience.Networking
{
	public class NakamaIdentity : MonoBehaviour
	{
		public int Owner = -1;
		public int NetID = -1;
		public int IndexPrefab = -1;

#if ENABLE_NAKAMA
		void Awake()
		{
			Owner = -1;
			NetID = -1;
			IndexPrefab = -1;
		}

		public void Set(int owner, int netID, int indexPrefab)
		{
			Owner = owner;
			NetID = netID;
			IndexPrefab = indexPrefab;
		}
#endif		
	}
}

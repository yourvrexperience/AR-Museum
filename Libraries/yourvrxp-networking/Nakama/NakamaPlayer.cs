#if ENABLE_NAKAMA
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

namespace yourvrexperience.Networking
{
	public class NakamaPlayer
	{
		public string ID;
		public int UID = -1;
		public string MatchID;
		public IUserPresence UserPresence;

		public NakamaPlayer(string id, string matchID, IUserPresence userPresence)
        {
			ID = id;
			MatchID = matchID;
			UserPresence = userPresence;
		}

		public bool Equals(IUserPresence userPresence)
        {
			return (UserPresence.UserId == userPresence.UserId);
		}


		public bool Equals(NakamaPlayer player)
		{
			return (UserPresence.UserId == player.UserPresence.UserId);
		}
	}
}
#endif
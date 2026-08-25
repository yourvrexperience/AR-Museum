#if ENABLE_MIRROR
using Mirror;
using Mirror.Discovery;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.Networking
{
	public class MirrorCustomDiscovery : 
#if ENABLE_MIRROR	
	NetworkDiscovery
#else
	MonoBehaviour
#endif
	{
#if ENABLE_MIRROR	
		public void SetUpDiscoveryPort(int portDiscover)
		{
			serverBroadcastListenPort = 10000 + portDiscover;
		}
#endif
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace yourvrexperience.Networking
{
	public interface INetworkPickable
	{
		GameObject GetGameObject();
		bool ToggleControl();
		void RequestAuthority();
		void ReleaseAuthority();
		void ActivatePhysics(bool activation, bool force = false);
	}
}
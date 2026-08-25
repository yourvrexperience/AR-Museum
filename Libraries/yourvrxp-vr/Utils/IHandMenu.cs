using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
using UnityEngine;

namespace yourvrexperience.VR
{
	public delegate void HandMenuBaseEvent(string nameEvent, params object[] parameters);

	public interface IHandMenu
	{
		event HandMenuBaseEvent Event;

		void Activation(bool activate);
	}
}
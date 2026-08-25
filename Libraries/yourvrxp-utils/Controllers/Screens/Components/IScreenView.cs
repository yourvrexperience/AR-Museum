using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace yourvrexperience.Utils
{
	public interface IScreenView 
    {
		string NameScreen { get; }
		Transform Content { get; }

		void Initialize(params object[] _list);
		void Destroy();
		void ActivateContent(bool value);
	}
}
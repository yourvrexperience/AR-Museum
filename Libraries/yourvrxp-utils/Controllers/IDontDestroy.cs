using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace yourvrexperience.Utils
{
	public interface IDontDestroy 
    {
		bool CanDestroyOnLoad { get; }
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.Utils
{
	public interface ISlotView
    {
		ItemMultiObjectEntry Data { get; }
		void Initialize(params object[] _list);
		bool Destroy();
		void ItemSelected(bool dispatchEvent = true);
		void ApplyGenericAction(params object[] parameters);
	}
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.Utils
{
	[RequireComponent(typeof(Renderer))]
	public class EnablerRenderer : MonoBehaviour
	{
		public void Activate(bool enabled)
		{
			this.transform.GetComponent<Renderer>().enabled = enabled;
		}
	}
}
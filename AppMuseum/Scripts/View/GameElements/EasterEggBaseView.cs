using yourvrexperience.Utils;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using yourvrexperience.Narration;
using UnityEngine.EventSystems;
using static yourvrexperience.template6dof.LevelView;

namespace yourvrexperience.template6dof
{
	public class EasterEggBaseView : MonoBehaviour
	{
		public GameObject Reference;
		public MeshRenderer RenderComponent;
		public GameObject ContainerStart;
		public GameObject StartOn;
		public GameObject StartOff;
		public int Index;
	}
}

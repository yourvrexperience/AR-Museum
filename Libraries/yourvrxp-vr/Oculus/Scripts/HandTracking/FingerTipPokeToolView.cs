#if ENABLE_OCULUS
using Oculus.Interaction;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace yourvrexperience.VR
{
	public class FingerTipPokeToolView : MonoBehaviour
	{
		[SerializeField] private MeshRenderer _sphereMeshRenderer = null;

#if ENABLE_OCULUS	
/*
		public InteractableTool InteractableTool { get; set; }

		public bool EnableState
		{
			get
			{
				if (_sphereMeshRenderer != null)
				{
					return _sphereMeshRenderer.enabled;
				}
				else
				{
					return true;
				}				
			}
			set
			{
				if (_sphereMeshRenderer != null)
				{
					_sphereMeshRenderer.enabled = value;
				}				
			}
		}

		public bool ToolActivateState { get; set; }

		public float SphereRadius { get; private set; }

		private void Awake()
		{
			Assert.IsNotNull(_sphereMeshRenderer);
		}

		public void SetFocusedInteractable(Interactable interactable)
		{
			// nothing to see here
		}
		*/
#endif
	}
}
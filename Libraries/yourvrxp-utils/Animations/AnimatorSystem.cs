using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.Utils
{
	[RequireComponent(typeof(Animator))]
	public class AnimatorSystem : MonoBehaviour
	{
		public delegate void AnimationChangedEvent(string triggerAnimation);
		public event AnimationChangedEvent AnimationEvent;

		public void DispatchAnimationChangedEvent(string triggerAnimation)
		{
			if (AnimationEvent != null)
			{
				AnimationEvent(triggerAnimation);
			}
		}

		[SerializeField]
		private string defaultTriggerAnimation;

		private Animator _animator;
		private string _currentTriggerAnimation;

		public string CurrentTriggerAnimation
		{
			get { return _currentTriggerAnimation; }
		}
		private Animator AnimatorComponent 
		{
			get 
			{
				if (_animator == null)
				{
					_animator = this.GetComponent<Animator>();
				}
				return _animator;
			}
		}

		void Start()
		{
			if (!string.IsNullOrEmpty(defaultTriggerAnimation))
			{
				ChangeAnimation(defaultTriggerAnimation);
			}
		}

		public void ChangeAnimation(string triggerAnimation, bool forceChange = false)
		{
			if (forceChange || _currentTriggerAnimation != triggerAnimation)
			{
				_currentTriggerAnimation = triggerAnimation;
				AnimatorComponent.SetTrigger(triggerAnimation);
				DispatchAnimationChangedEvent(triggerAnimation);
			}
		}
	}
}

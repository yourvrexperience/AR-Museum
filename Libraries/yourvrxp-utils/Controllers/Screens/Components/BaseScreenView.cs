using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
using yourvrexperience.VR;
#endif

namespace yourvrexperience.Utils
{
	public class BaseScreenView : MonoBehaviour, IScreenView
    {
		public const string EventBaseScreenViewCreated = "EventBaseScreenViewCreated";
		public const string EventBaseScreenViewDestroyed = "EventBaseScreenViewDestroyed";
		public const string EventBaseScreenViewEnableInteraction = "EventBaseScreenViewEnableInteraction";
		public const string EventBaseScreenViewSetCanvasOrder = "EventBaseScreenViewSetCanvasOrder";

		protected Transform _content;
		protected Transform _background;

		protected Canvas _canvas;
		protected GraphicRaycaster _raycaster;

		public Transform Content
		{ 
			get { return _content; } 
		}
		public Canvas Canvas
		{
			get { return _canvas; }
		}
		public Transform Background
		{ 
			get { return _background; } 
		}

		public virtual string NameScreen 
		{ 
			get { return this.name; }
		}

		public virtual void Initialize(params object[] parameters)
		{
			_content = this.transform.Find("Content");
			_background = this.transform.Find("Background");
			_canvas = this.GetComponent<Canvas>();
			_raycaster = this.GetComponent<GraphicRaycaster>();

			Debug.Assert(_content != null);

			UIEventController.Instance.Event += OnUILocalEvent;
			SystemEventController.Instance.DispatchSystemEvent(EventBaseScreenViewCreated, this.gameObject, NameScreen);
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			if (VRInputController.Instance != null) VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerResetAllInputs);
#endif			
		}

		public virtual void ActivateContent(bool value)
		{
			Content.gameObject.SetActive(value);

			if (value)
            {
				_raycaster.enabled = true;
			}
			else
            {
				_raycaster.enabled = false;
			}
		}

		public virtual void Destroy()
		{
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUILocalEvent;
			SystemEventController.Instance.DelaySystemEvent(EventBaseScreenViewDestroyed, 0.1f, NameScreen);	
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			if (VRInputController.Instance != null) VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerResetAllInputs);
#endif			
		}

        private void OnUILocalEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(EventBaseScreenViewEnableInteraction))
			{				
				bool interactivity = (bool)parameters[0];
				yourvrexperience.Utils.Utilities.ApplyEnabledInteraction(this.gameObject.transform, interactivity);
			}
			if (nameEvent.Equals(EventBaseScreenViewSetCanvasOrder))
            {
				if (parameters[0] is string)
                {
					string targetName = (string)parameters[0];
					if (NameScreen.IndexOf(targetName) != -1)
					{
						_canvas.sortingOrder = (int)parameters[1];
					}
				}
				else
                {
					if (parameters[0] is GameObject)
                    {
						if (this.gameObject == (GameObject)parameters[0])
                        {
							_canvas.sortingOrder = (int)parameters[1];
						}
                    }
				}
			}
        }
    }
}
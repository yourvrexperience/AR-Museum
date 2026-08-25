using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;
using System;
#if ENABLE_VUFORIA
using Vuforia;
#endif

namespace yourvrexperience.template6dof
{
    public class VuforiaController : MonoBehaviour
	{
		private static VuforiaController _instance;

        public static VuforiaController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(VuforiaController)) as VuforiaController;
                }
                return _instance;
            }
        }

		[SerializeField] private Camera arVuforiaCamera;
		[SerializeField] private BoxCollider cameraVision;
#if ENABLE_VUFORIA		
		[SerializeField] private VuforiaBehaviour vuforiaBehaviour;
#endif
		private bool _hasAreaBeenDetected = false;

		public bool HasAreaBeenDetected
        {
			get { return _hasAreaBeenDetected;  }
			set { 
				_hasAreaBeenDetected = value;  
				if (_hasAreaBeenDetected)
				{
					MainController.Instance.ApplyOclusionNavigation();
					SystemEventController.Instance.DispatchSystemEvent(ARMaxSTController.EventARMaxSTControllerAreaRecognized);
				}
				else
				{
					SystemEventController.Instance.DispatchSystemEvent(ARMaxSTController.EventARMaxSTControllerAreaLost);
				}
			}
        }
		public Camera ARVuforiaCamera
        {
			get { return arVuforiaCamera;  }
			set { arVuforiaCamera = value; }
        }

		void Start()
		{
			SystemEventController.Instance.Event += OnSystemEvent;
		}

        void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		public bool CheckVisiblePoint(Vector3 position)
		{
			return cameraVision.bounds.Contains(position);
		}

#if ENABLE_VUFORIA		
		public void SetWorldCenter(ObserverBehaviour observerBehaviour)
		{
			vuforiaBehaviour.SetWorldCenter(WorldCenterMode.DEVICE, observerBehaviour);
		}
#endif

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (!this.gameObject.activeSelf) return;

			if (nameEvent.Equals(LevelView.EventLevelViewStarted))
			{
				if (MainController.Instance.EnableEditionPOIs)
				{
					SystemEventController.Instance.DispatchSystemEvent(LevelView.EventLevelViewForcePOIsVisible);
				}
				
#if UNITY_EDITOR
				_hasAreaBeenDetected = true;
#endif
			}
        }
    }
}
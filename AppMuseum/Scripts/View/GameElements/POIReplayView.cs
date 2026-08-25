using yourvrexperience.Utils;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace yourvrexperience.template6dof
{
	public class POIReplayView : MonoBehaviour
	{
		public const string EventPOIReplayViewDisplayScreen = "EventPOIReplayViewDisplayScreen";
		public const string EventPOIReplayViewDestroyScreen = "EventPOIReplayViewDestroyScreen";
		public const string EventPOIReplayViewPlayPOI = "EventPOIReplayViewPlayPOI";
		public const string EventPOIReplayViewEnablePOIs = "EventPOIReplayViewEnablePOIs";

		public const float DistanceToDisplayIcon = 3;

		[SerializeField] private int POIIndex;
		[SerializeField] private GameObject IconContainer;
		[SerializeField] private Text LabelIndex;

		private bool _enabled = true;
		private bool _activated;
		private int _layerPOI;

		void Start()
		{
			if (LabelIndex != null)
			{
				LabelIndex.text = (POIIndex+1).ToString();
				LabelIndex.gameObject.SetActive(MainController.Instance.EnableEditionPOIs);
#if UNITY_EDITOR
				yourvrexperience.Utils.Utilities.ResetMaterials(LabelIndex.gameObject);
#endif				
			} 
		}

		public void SetPOIIndex(int poiIndex)
		{
			POIIndex = poiIndex;
		}

		public void DeActivate()
		{
			if (_activated)
			{
				RemoveListeners();
			}
			_activated = false;
			this.gameObject.SetActive(false);
		}

		public void Activate()
		{
			if (!_activated)
			{
				SystemEventController.Instance.Event += OnSystemEvent;
			}
			_activated = true;
			this.gameObject.SetActive(true);
			_layerPOI = LayerMask.GetMask("POI");			
		}

		private void RemoveListeners()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		void OnDestroy()
		{
			if (_activated)
			{
				RemoveListeners();
			}
		}

        private void DeactivateIconContainer()
		{
			if (IconContainer.activeSelf)
			{
				IconContainer.SetActive(false);
				SystemEventController.Instance.DispatchSystemEvent(ScreenReplayPOIView.EventScreenReplayPOIViewDestroy);				
			}			
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {						
			if (nameEvent.Equals(MainController.EventMainControllerReleaseGameResources))
			{
				DeActivate();
				_enabled = false;
			}
            if (nameEvent.Equals(EventPOIReplayViewEnablePOIs))
			{
				_enabled = (bool)parameters[0];				
				if (!_enabled)
				{
					IconContainer.SetActive(false);
				}				
			}
        }

		void Update()
		{
			if (_activated && _enabled)
			{
				if (Vector3.Distance(MainController.Instance.PlayerView.transform.position, this.transform.position) < DistanceToDisplayIcon)
				{
					bool isReplayVisible = yourvrexperience.Utils.Utilities.IsVisibleFrom(this.transform.position, Camera.main);
#if ENABLE_VUFORIA
					isReplayVisible = VuforiaController.Instance.CheckVisiblePoint(this.transform.position);
#elif !UNITY_EDITOR && !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL || UNITY_WEBGL)
					isReplayVisible = ARMaxSTController.Instance.CheckVisiblePoint(this.transform.position);
#endif					
					if (isReplayVisible)
					{
						/*
						if (IconContainer.GetComponent<yourvrexperience.Utils.Billboard>() == null)
						{
							IconContainer.AddComponent<yourvrexperience.Utils.Billboard>();
						}
						*/
						if (!IconContainer.activeSelf)
						{
							bool shouldEnableReplay = true;
							float distanceToTourGuide = Vector3.Distance(MainController.Instance.GameInputController.Camera.transform.position, MainController.Instance.GuideTourView.gameObject.transform.position);
							if (yourvrexperience.Utils.Utilities.IsVisibleFrom(MainController.Instance.GuideTourView.gameObject.GetComponent<Collider>().bounds, MainController.Instance.GameInputController.Camera))
							{
								shouldEnableReplay = false;
							}
							if (shouldEnableReplay)
							{
								IconContainer.SetActive(true);
								SystemEventController.Instance.DispatchSystemEvent(EventPOIReplayViewDisplayScreen, POIIndex, IconContainer.transform);
							}
						}
						else
						{
							if (yourvrexperience.Utils.Utilities.IsVisibleFrom(MainController.Instance.GuideTourView.gameObject.GetComponent<Collider>().bounds, MainController.Instance.GameInputController.Camera))
							{
								DeactivateIconContainer();
							}
						}
					}
					else
					{
						DeactivateIconContainer();
					}
				}
				else
				{
					DeactivateIconContainer();
				}
			}
		}
	}
}

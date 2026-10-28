using yourvrexperience.Utils;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using yourvrexperience.Narration;
using static yourvrexperience.template6dof.NianticController;
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NIANTICXR || ENABLE_QR_XR || ENABLE_QR_META
using yourvrexperience.VR;
#endif

namespace yourvrexperience.template6dof
{
	public class POIReplayView : MonoBehaviour
	{
		public const string EventPOIReplayViewDisplayScreen = "EventPOIReplayViewDisplayScreen";
		public const string EventPOIReplayViewDestroyScreen = "EventPOIReplayViewDestroyScreen";
		public const string EventPOIReplayViewPlayPOI = "EventPOIReplayViewPlayPOI";
		public const string EventPOIReplayViewEnablePOIs = "EventPOIReplayViewEnablePOIs";
		public const string EventPOIReplayViewEnableSecrets = "EventPOIReplayViewEnableSecrets";

		public const float DistanceToDisplayIcon = 3;
		public const float DistanceGuideIsCloseToPOI = 0.75f;

		[SerializeField] private int POIIndex;
		[SerializeField] private GameObject IconContainer;
		[SerializeField] private Text LabelIndex;

		private bool _enabled = true;
		private bool _activated;
		private int _layerPOI;
		private bool _displayingScreen = false;
		private bool _isReplayPlaying = false;
		private string _replayQR = "";

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
			SystemEventController.Instance.DelaySystemEvent(NarrationController.EventNarrationControllerRequestQRCode, 0.1f + POIIndex * 0.1f, POIIndex);			
#if (ENABLE_QR_MODE || ENABLE_QR_XR || ENABLE_QR_META) && !UNITY_EDITOR			
			if (MainController.Instance.EnableEditionPOIs)
			{
				this.gameObject.transform.root.gameObject.SetActive(false);
			}
#endif			
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
			if (!MainController.Instance.EnableEditionPOIs)
			{
				if (IconContainer.activeSelf)
				{
					IconContainer.SetActive(false);
					SystemEventController.Instance.DispatchSystemEvent(ScreenReplayPOIView.EventScreenReplayPOIViewDestroy);	
				}			
			}
		}

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {				
			if (nameEvent.Equals(NarrationController.EventNarrationControllerReportQRCode))	
			{
				int poiIndex = (int)parameters[0];
				if (POIIndex == poiIndex)
				{
					_replayQR = (string)parameters[1];
				}
			}
			if (_enabled)
			{
				if (nameEvent.Equals(QRImageController.EventQRImageControllerReportQRDetected))
				{
					string qrDetected = (string)parameters[0];					
					if (qrDetected.Equals(_replayQR))
					{
						MainController.Instance.LevelView.PlayingQR = _replayQR;
						MainController.Instance.LevelView.PlayingPOI = MainController.Instance.LevelView.GetSelectedPOI(POIIndex);
						if (MainController.Instance.EnableEditionPOIs)						
						{
							SystemEventController.Instance.DispatchSystemEvent(GameSubStateEditPOI.EventGameStateRunForceEditionSelection, this.gameObject);
						}
						else
						{
							SystemEventController.Instance.DispatchSystemEvent(EventPOIReplayViewDisplayScreen, POIIndex, IconContainer.transform);
						}						
					}					
				}
				if (nameEvent.Equals(QRImageController.EventQRImageControllerReportQRLost))
				{
					string qrLost = (string)parameters[0];					
					if (qrLost.Equals(_replayQR))
					{
						if (MainController.Instance.EnableEditionPOIs)
						{
							SystemEventController.Instance.DispatchSystemEvent(ScreenSelectedEditionPOIView.EventScreenSelectedEditionPOIViewDestroy);
						}
						else
						{
							SystemEventController.Instance.DispatchSystemEvent(ScreenReplayPOIView.EventScreenReplayPOIViewDestroy, POIIndex);
						}						
					}					
				}
			}
            if (nameEvent.Equals(EventPOIReplayViewEnablePOIs))
			{
				_enabled = (bool)parameters[0];		
				if (MainController.Instance.EnableEditionPOIs)
				{
					IconContainer.SetActive(_enabled);
				}
				else
				{
					if (!_enabled)
					{
						IconContainer.SetActive(false);
						MainController.Instance.HighlightedPOI.SetActive(false);
					}				
				}
			}
			if (nameEvent.Equals(MainController.EventMainControllerReleaseGameResources))
			{
				DeActivate();
				_enabled = false;
			}
			if (nameEvent.Equals(NarrationController.EventNarrationControllerFinished) 
				|| nameEvent.Equals(NarrationController.EventNarrationControllerDoStop)
				|| nameEvent.Equals(NarrationController.EventNarrationControllerDestroyNoMainNarrations))
			{
				_isReplayPlaying = false;
			}		
        }

		void Update()
		{
			if (_activated && _enabled && !MainController.Instance.EnableEditionPOIs)
			{
#if (ENABLE_QR_MODE || ENABLE_QR_XR || ENABLE_QR_META) && !UNITY_EDITOR
				return;
#endif				
				if (Vector3.Distance(MainController.Instance.PlayerView.transform.position, this.transform.position) < DistanceToDisplayIcon)
				{
					bool isReplayVisible = yourvrexperience.Utils.Utilities.IsVisibleFrom(this.transform.position, Camera.main);
#if !UNITY_EDITOR
#if ENABLE_VUFORIA
					isReplayVisible = VuforiaController.Instance.CheckVisiblePoint(this.transform.position);
#elif ENABLE_MAXST
					isReplayVisible = ARMaxSTController.Instance.CheckVisiblePoint(this.transform.position);
#endif					
#endif

					if (isReplayVisible)
					{
						if (!IconContainer.activeSelf)
						{
							if (MainController.Instance.GuideTourView != null)
							{
								bool shouldEnableReplay = yourvrexperience.Utils.Utilities.IsVisibleFrom(MainController.Instance.GuideTourView.transform.position, Camera.main);
								IconContainer.SetActive(!shouldEnableReplay);
							}
						}
						else
						{
							if (_isReplayPlaying) return;

							Vector3	positionCurrentController = Vector3.zero;
							Vector3	forwardCurrentController = Vector3.zero;
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL || ENABLE_NIANTICXR || ENABLE_QR_XR || ENABLE_QR_META )
#if ENABLE_NIANTICXR 
							positionCurrentController = VRInputController.Instance.VRController.GetOriginByLineRenderer();
							forwardCurrentController = VRInputController.Instance.VRController.GetForwardByLineRenderer();
#else
							if (VRInputController.Instance.VRController.CurrentController != null)
							{
								positionCurrentController = VRInputController.Instance.VRController.CurrentController.transform.position;
								forwardCurrentController = VRInputController.Instance.VRController.CurrentController.transform.forward;
							}
#endif
#else
							positionCurrentController = Camera.main.transform.position;
							forwardCurrentController = Camera.main.transform.forward;
#endif						

							RaycastHit ray = new RaycastHit();
							GameObject highlightedPOI = RaycastingTools.GetRaycastObject(positionCurrentController, forwardCurrentController, 100, ref ray, GameLevelData.Instance.LayerReplay);
							if ((highlightedPOI != null) && (highlightedPOI == this.gameObject))
							{
								MainController.Instance.HighlightedPOI.transform.position = highlightedPOI.transform.position;
								MainController.Instance.HighlightedPOI.SetActive(true);

								if (MainController.Instance.GameInputController.ActionPrimaryUp())
								{
									_isReplayPlaying = true;
									MainController.Instance.HighlightedPOI.SetActive(false);
									MainController.Instance.LevelView.PlayingQR = _replayQR;
									MainController.Instance.LevelView.PlayingPOI = MainController.Instance.LevelView.GetSelectedPOI(POIIndex);		
									SystemEventController.Instance.DispatchSystemEvent(EventPOIReplayViewDisplayScreen, POIIndex, IconContainer.transform);
								}
							}
							else
							{
								MainController.Instance.HighlightedPOI.SetActive(false);
							}
						}
					}
					else
					{
						_isReplayPlaying = false;
						DeactivateIconContainer();
					}
				}
				else
				{
					_isReplayPlaying = false;
					DeactivateIconContainer();
				}
			}							
		}
	}	
}

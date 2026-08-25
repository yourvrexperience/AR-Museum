#if ENABLE_OCULUS
using Oculus.Interaction;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using yourvrexperience.Utils;
using UnityEngine.Assertions;

namespace yourvrexperience.VR
{
    public class OculusController : MonoBehaviour
#if ENABLE_OCULUS	
	, IVRController
#endif	
    {
		public Camera OculusCamera;
        public GameObject OculusLeftController;
        public GameObject OculusRightController;

#if ENABLE_OCULUS
        private static OculusController _instance;

        public static OculusController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(OculusController)) as OculusController;
                    return _instance;
                }
				return _instance;
            }
        }

        private LineRenderer _raycastLineLeft;
        private LineRenderer _raycastLineRight;
        
        private XR_HAND _handSelected = XR_HAND.none;

        private OVRInputModule _ovrInputModule;
        private OVRGazePointer _ovrGazePointer;
		private OculusHandsManager _ovrHandsManager;
		private OVRHand _ovrHandRight;
		private OVRHand _ovrHandLeft;
		private bool _pinchMantained = false;
		private bool _palmToFace = false;
		private XR_HAND _handMantained;
		private Vector3 _positionCollisionRaycasted;
		private	Vector3 _originLineLeft;
		private	Vector3 _targetLineLeft;
		private	Vector3 _originLineRight;
		private	Vector3 _targetLineRight;

		private Camera _mainCamera;

		public Camera Camera
		{
			get { 
				if (_mainCamera == null)
				{
					_mainCamera = OculusCamera.GetComponentInChildren<Camera>();
				}
				return _mainCamera;
			}
		}
		public GameObject Container 
		{
			get { return this.gameObject; }
		}
		public GameObject HeadController
		{
			get { return OculusCamera.gameObject; }
		}
        public GameObject HandLeftController
		{
			get { 
				if ((OculusHandsManager.Instance != null) && OculusHandsManager.Instance.HandsBeingTracked)
				{
					return OculusHandsManager.Instance.LeftHandContainer;
				}
				else
				{
					return OculusLeftController; 
				}				
			}
		}
		public GameObject HandRightController
		{
			get { 
				if ((OculusHandsManager.Instance != null) && OculusHandsManager.Instance.HandsBeingTracked)
				{
					return OculusHandsManager.Instance.RightHandContainer;
				}
				else
				{
					return OculusRightController; 
				}
			}
		}
        public LineRenderer RaycastLineLeft
        {
            get { return _raycastLineLeft; }
        }
        public LineRenderer RaycastLineRight
        {
            get { return _raycastLineRight; }
        }
        public GameObject CurrentController
        {
            get {
				if ((_ovrHandsManager != null) && _ovrHandsManager.HandsBeingTracked)
				{
					if (_ovrHandsManager.ReferenceToRay != null)
					{
						return _ovrHandsManager.ReferenceToRay.gameObject;
					}
					else
					{
						return null;
					}					
				}
				else
				{
					if (_raycastLineRight != null)
					{
						if (_handSelected == XR_HAND.right)
						{
							return _raycastLineRight.gameObject;
						}
						else
						{
							return _raycastLineLeft.gameObject;
						}
					}
				}
				return null;
			}			
        }
        public GameObject OtherController
        {
            get {
				if ((_ovrHandsManager != null) && _ovrHandsManager.HandsBeingTracked)
				{
					if (_ovrHandsManager.ReferenceToRay != null)
					{
						return _ovrHandsManager.ReferenceToRay.gameObject;
					}
					else
					{
						return null;
					}					
				}
				else
				{
					if (_handSelected != XR_HAND.right)
					{
						return _raycastLineRight.gameObject;
					}
					else
					{
						return _raycastLineLeft.gameObject;
					}
				}
            }
        }
		private OculusHandsManager OvrHandsManager
		{
			get {
				if (_ovrHandsManager == null)
				{
					_ovrHandsManager = GameObject.FindObjectOfType<OculusHandsManager>();
				}
				return _ovrHandsManager;
			}
		}
		private OVRInputModule OvrInputModule
		{
			get {
					if (_ovrInputModule == null)
                    {
                        _ovrInputModule = GameObject.FindObjectOfType<OVRInputModule>();
                    }
					return _ovrInputModule;
			}
		}
        public XR_HAND HandSelected
        {
            get { return _handSelected; }
            set {
				_handSelected = value;
				VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerChangedHandSide, _handSelected);
				if (CurrentController != null)
				{
					DisableRays();
					CurrentController.SetActive(true);
					OvrInputModule.rayTransform = CurrentController.transform;
					if (_ovrGazePointer == null)
					{
						_ovrGazePointer = GameObject.FindObjectOfType<OVRGazePointer>();
					}
					_ovrGazePointer.rayTransform = CurrentController.transform;                     
				}
             }
        }
		public Vector3 PositionCollisionRaycasted  
		{ 
			get { return _positionCollisionRaycasted; }
			set { _positionCollisionRaycasted = value; 

				Vector3 positionOriginRay = CurrentController.transform.position;
				float distanceToCollider = 	Vector3.Distance(_positionCollisionRaycasted, positionOriginRay);
				if (_handSelected == XR_HAND.right)
				{
					float originalDistanceRight = Vector3.Distance(_originLineRight, _targetLineRight);
					if (originalDistanceRight > distanceToCollider)
					{
						_raycastLineRight.SetPosition(1, new Vector3(0, 0, distanceToCollider));
					}
					else
					{
						_raycastLineRight.SetPosition(1, new Vector3(0, 0, originalDistanceRight));
					}
				}
				else
				{
					float originalDistanceLeft = Vector3.Distance(_originLineLeft, _targetLineLeft);
					if (originalDistanceLeft > distanceToCollider)
					{
						_raycastLineLeft.SetPosition(1, new Vector3(0, 0, distanceToCollider));
					}
					else
					{
						_raycastLineLeft.SetPosition(1, new Vector3(0, 0, originalDistanceLeft));
					}
				}
			}
		}

        public bool HandTrackingActive 
		{
			get {
				if (_ovrHandsManager != null)
				{
					return _ovrHandsManager.HandsBeingTracked;
				}
				else
				{
					return false;
				}
			}
		}

        void Start()
        {
            _raycastLineLeft = HandLeftController.GetComponentInChildren<LineRenderer>();
            _raycastLineRight = HandRightController.GetComponentInChildren<LineRenderer>();
			
			_originLineLeft = _raycastLineLeft.GetPosition(0);
			_targetLineLeft = _raycastLineLeft.GetPosition(1);
			
			_originLineRight = _raycastLineRight.GetPosition(0);
			_targetLineRight = _raycastLineRight.GetPosition(1);

            DisableRays();
			SystemEventController.Instance.Event += OnSystemEvent;
			VRInputController.Instance.Event += OnVREvent;
        }

		private void OnDestroy()
        {
            if (Instance !=  null)
            {
                Deactivate();
				if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
				if (VRInputController.Instance != null) VRInputController.Instance.Event -= OnVREvent;
				if (OvrHandsManager != null) OvrHandsManager.Destroy();

                GameObject.Destroy(_instance.gameObject);
                _instance = null;
            }
        }

		private void InitOculusHandManager()
		{
			if ((OvrHandsManager != null) && (_ovrHandRight != null) && (_ovrHandLeft != null))
			{
				OvrHandsManager.Initialize(_ovrHandLeft.gameObject, OculusLeftController, _ovrHandRight.gameObject, OculusRightController);
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))
			{
				if (Instance)
				{
					DontDestroyOnLoad(Instance.gameObject);
				}
			}
			if (nameEvent.Equals(ObjectReporter.EventObjectReporterResponse))
			{
				GameObject target = (GameObject)parameters[0];
				if (_ovrHandsManager == null)
				{
					if (target.GetComponent<OculusHandsManager>() != null)
					{
						_ovrHandsManager = target.GetComponent<OculusHandsManager>();
						InitOculusHandManager();
					}
				}
				if (_ovrHandRight == null)
				{
					if (target.GetComponent<OVRHand>() != null)
					{
						bool isRightHand = bool.Parse((string)parameters[1]);
						if (isRightHand)
						{
							_ovrHandRight = target.GetComponent<OVRHand>();
							InitOculusHandManager();
						}						
					}
				}
				if (_ovrHandLeft == null)
				{
					if (target.GetComponent<OVRHand>() != null)
					{
						bool isRightHand = bool.Parse((string)parameters[1]);
						if (!isRightHand)
						{
							_ovrHandLeft = target.GetComponent<OVRHand>();
							InitOculusHandManager();
						}						
					}
				}
			}
		}

		private void OnVREvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(VRInputController.EventVRInputControllerAssignCurrentPointer))
			{
				HandSelected = XR_HAND.right;
			}
			if (nameEvent.Equals(HandTeleportPoseRecognizer.EventHandTeleportPoseRecognizerTriggered))
			{
				_handMantained = (XR_HAND)parameters[0];
				_pinchMantained = true;				
			}
			if (nameEvent.Equals(HandTeleportPoseRecognizer.EventHandTeleportPoseRecognizerReleased))
			{
				_handMantained = (XR_HAND)parameters[0];
				_pinchMantained = false;				
			}
			if (nameEvent.Equals(HandPalmToFacePoseRecognizer.EventHandPalmToFacePoseRecognizerTriggered))
			{
				_handMantained = (XR_HAND)parameters[0];
				_palmToFace = true;				
			}
			if (nameEvent.Equals(HandPalmToFacePoseRecognizer.EventHandPalmToFacePoseRecognizerReleased))
			{
				_handMantained = (XR_HAND)parameters[0];
				_palmToFace = false;				
			}
			if (nameEvent.Equals(IndexPointerPoseRecognizer.EventIndexPointerPoseRecognizerTriggered))
			{
				XR_HAND targetHand = (XR_HAND)parameters[0];
				VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerIndexTriggered, true, targetHand);
			}
			if (nameEvent.Equals(IndexPointerPoseRecognizer.EventIndexPointerPoseRecognizerReleased))
			{
				XR_HAND targetHand = (XR_HAND)parameters[0];
				VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerIndexTriggered, false, targetHand);
			}
			if (nameEvent.Equals(OculusHandsManager.EventOculusHandsManagerStateChanged))
			{
				VRInputController.Instance.DispatchVREvent(VRInputController.EventVRInputControllerChangedHandTrackingState, (bool)parameters[0]);
			}
		}

        private void DisableRays()
        {
            if (_raycastLineLeft != null) _raycastLineLeft.gameObject.SetActive(false);
            if (_raycastLineRight != null) _raycastLineRight.gameObject.SetActive(false);
        }

        public void Deactivate()
        {
            if (HandLeftController != null) HandLeftController.SetActive(false);
            if (HandRightController != null) HandRightController.SetActive(false);

            if (_raycastLineLeft != null) _raycastLineLeft.gameObject.SetActive(false);
            if (_raycastLineRight != null) _raycastLineRight.gameObject.SetActive(false);
        }

        public void Activate()
        {
            HandLeftController.SetActive(true);
            HandRightController.SetActive(true);

            if (_raycastLineLeft != null) _raycastLineLeft.gameObject.SetActive(false);
            if (_raycastLineRight != null) _raycastLineRight.gameObject.SetActive(false);
        }

		public Vector2 GetVector2Joystick(XR_HAND hand)
		{
			if (_ovrHandsManager != null)
			{
				if (_ovrHandsManager.HandsBeingTracked)
				{
					if (_pinchMantained)
					{
						if (_handMantained == hand)
						{
							return new Vector2(1, 1);
						}						
					}
				}
			}

			switch (hand)
			{
				case XR_HAND.right:
					return OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);

				case XR_HAND.left:
					return OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);

				case XR_HAND.both:
					return OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch) + OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
			}
			return Vector2.zero;
		}

		public bool GetThumbstickDown(XR_HAND hand)
		{
			switch (hand)
			{
				case XR_HAND.right:
					return OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);

				case XR_HAND.left:
					return OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);

				case XR_HAND.both:
					return OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch) || OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
			}
			return false;
		}

		public bool GetThumbstickUp(XR_HAND hand)
		{
			switch (hand)
			{
				case XR_HAND.right:
					return OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);

				case XR_HAND.left:
					return OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);

				case XR_HAND.both:
					return OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch) || OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
			}
			return false;
		}
		public bool GetThumbstick(XR_HAND hand)
		{
			switch (hand)
			{
				case XR_HAND.right:
					return OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);

				case XR_HAND.left:
					return OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);

				case XR_HAND.both:
					return OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch) || OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
			}
			return false;
		}

		public bool GetIndexTriggerDown(XR_HAND hand, bool consume = true)
		{
			if ((OculusHandsManager.Instance != null) && (OculusHandsManager.Instance.HandsBeingTracked))
			{
				switch (hand)
				{
					case XR_HAND.right:
						return OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch);
					case XR_HAND.left:
						return OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch);
					case XR_HAND.both:
						return OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch) || OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch);
				}
			}
			else
			{
				switch (hand)
				{
					case XR_HAND.right:
						return OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
					case XR_HAND.left:
						return OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
					case XR_HAND.both:
						return OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch) || OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
				}
			}
			return false;
		}

		public bool GetIndexTriggerUp(XR_HAND hand, bool consume = true)
		{
			switch (hand)
			{
				case XR_HAND.right:
					return OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
				case XR_HAND.left:
					return OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
				case XR_HAND.both:
					return OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch) || OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
			}
			return false;
		}

		public bool GetIndexTrigger(XR_HAND hand)
		{
			switch (hand)
			{
				case XR_HAND.right:
					return OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
				case XR_HAND.left:
					return OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
				case XR_HAND.both:
					return OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch) || OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
			}
			return false;
		}
		public bool GetHandTriggerDown(XR_HAND hand, bool consume = true)
		{
			switch (hand)
			{
				case XR_HAND.right:
					return OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);
				case XR_HAND.left:
					return OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);
				case XR_HAND.both:
					return OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch) || OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);
			}
			return false;
		}
		public bool GetHandTriggerUp(XR_HAND hand, bool consume = true)
		{
			switch (hand)
			{
				case XR_HAND.right:
					return OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);
				case XR_HAND.left:
					return OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);
				case XR_HAND.both:
					return OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch) || OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);
			}
			return false;
		}
		public bool GetHandTrigger(XR_HAND hand)
		{
			switch (hand)
			{
				case XR_HAND.right:
					return OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);
				case XR_HAND.left:
					return OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);
				case XR_HAND.both:
					return OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch) || OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);
			}
			return false;
		}

		public bool GetOneButtonDown(XR_HAND hand)
		{
			if (OculusHandsManager.Instance != null)
			{
				if (OculusHandsManager.Instance.HandsBeingTracked)
				{
					if (_palmToFace)
					{
						_palmToFace = false;
						return true;
					}
					return false;
				} 
			}

			switch (hand)
			{
				case XR_HAND.right:
					return OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch);
				case XR_HAND.left:
					return OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch);
				case XR_HAND.both:
					return OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch) || OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch);
			}
			return false;
		}
		public bool GetOneButtonUp(XR_HAND hand)
		{
			switch (hand)
			{
				case XR_HAND.right:
					return OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch);
				case XR_HAND.left:
					return OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTouch);
				case XR_HAND.both:
					return OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch) || OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTouch);
			}
			return false;
		}
		public bool GetOneButton(XR_HAND hand)
		{
			switch (hand)
			{
				case XR_HAND.right:
					return OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch);
				case XR_HAND.left:
					return OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch);
				case XR_HAND.both:
					return OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch) || OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch);
			}
			return false;
		}
		public bool GetTwoButtonDown(XR_HAND hand)
		{
			switch (hand)
			{
				case XR_HAND.right:
					return OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch);
				case XR_HAND.left:
					return OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch);
				case XR_HAND.both:
					return OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch) || OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch);
			}
			return false;
		}
		public bool GetTwoButtonUp(XR_HAND hand)
		{
			switch (hand)
			{
				case XR_HAND.right:
					return OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTouch);
				case XR_HAND.left:
					return OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.LTouch);
				case XR_HAND.both:
					return OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTouch) || OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.LTouch);
			}
			return false;
		}
		public bool GetTwoButton(XR_HAND hand)
		{
			switch (hand)
			{
				case XR_HAND.right:
					return OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch);
				case XR_HAND.left:
					return OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.LTouch);
				case XR_HAND.both:
					return OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch) || OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.LTouch);
			}
			return false;
		}

        public void UpdateHandSideController()
        {
			if (GetIndexTriggerDown(XR_HAND.right) || GetHandTriggerDown(XR_HAND.right))
			{
				HandSelected = XR_HAND.right;
			}
			if (GetIndexTriggerDown(XR_HAND.left) || GetHandTriggerDown(XR_HAND.left))
			{
				HandSelected = XR_HAND.left;
			}
        }

		public void ResetState()
		{
		}

		void Update()
		{
			UpdateHandSideController();
		}
#endif
    }
}
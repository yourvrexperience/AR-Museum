using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
using yourvrexperience.VR;
#endif
#if ENABLE_ULTIMATEXR
using UltimateXR.Manipulation;
#endif

namespace yourvrexperience.Utils
{
	[RequireComponent(typeof(Collider))]
	[RequireComponent(typeof(Rigidbody))]
#if ENABLE_ULTIMATEXR
	[RequireComponent(typeof(UxrGrabbableObject))]
#endif	
    public class ObjectPickable : MonoBehaviour, IObjectPickable
    {
		public const string EventObjectPickableGrabbed = "EventObjectPickableGrabbed";
		public const string EventObjectPickableReleased = "EventObjectPickableReleased";

		public event PickedObjectEvent PickedEvent;

		public void DispatchPickedEvent(bool isGrabbed)
		{
			if (PickedEvent != null)
			{
				PickedEvent(isGrabbed);
			}
		}

        [SerializeField] protected float grabbedObjectDistance = 1;
		[SerializeField] protected float SizeCube = 1;
		[SerializeField] protected string CastingTarget = "Floor";
		[SerializeField] protected string CastingForbidden = "Forbidden";		
#if ENABLE_ULTIMATEXR
        private UxrGrabbableObject _grabbableObject;
#endif

		protected bool _isGrabbed = false;
		protected bool _isEnabled = true;

        protected Collider _collider;
        protected Rigidbody _rigidBody;
        protected int _layer;
		protected int _forbiddenMaskLayer = -1;
		protected int _floorMaskLayer = -1;


		public bool IsEnabled
		{
			get { return _isEnabled; }
			set { _isEnabled = value; }
		}
		public bool IsGrabbed
		{
			get { return _isGrabbed; }
			set { _isGrabbed = value; }
		}
		public float GrabbedObjectDistance
		{
			get { return grabbedObjectDistance; }
			set { grabbedObjectDistance = value; }
		}

        public Rigidbody GetRigidBody()
        {
            return _rigidBody;
        }		

		void Awake()
		{
            _layer = this.gameObject.layer;
            _collider = this.GetComponent<Collider>();
            _rigidBody = this.GetComponent<Rigidbody>();
		}

        protected virtual void Start()
        {
			_floorMaskLayer = LayerMask.GetMask(CastingTarget);
			_forbiddenMaskLayer = LayerMask.GetMask(CastingForbidden);

#if (ENABLE_ULTIMATEXR)
            _grabbableObject = GetComponent<UxrGrabbableObject>();
			if (_grabbableObject != null)
			{
				_grabbableObject.Grabbed += UxrGrabbedEvent;
				_grabbableObject.Released += UxrReleasedEvent;
			}
#endif
		}

#if ENABLE_ULTIMATEXR
        private void UxrGrabbedEvent(object sender, UxrManipulationEventArgs args)
        {
            SetIsGrabbed(_grabbableObject.IsBeingGrabbed);
            DispatchPickedEvent(_isGrabbed);
        }

        private void UxrReleasedEvent(object sender, UxrManipulationEventArgs args)
        {
            SetIsGrabbed(_grabbableObject.IsBeingGrabbed);
            DispatchPickedEvent(_isGrabbed);
        }
#endif
        void OnDestroy()
        {
        }

       	public void ResetForces()
        {
#if UNITY_6000_0_OR_NEWER
			_rigidBody.linearVelocity = Vector3.zero;
#else
			_rigidBody.velocity = Vector3.zero;
#endif
			_rigidBody.angularVelocity = Vector3.zero;
        }
 
       	public bool GetGravity()
        {
            if (_rigidBody != null)
			{
				return _rigidBody.useGravity;
			}
			else
			{
				return false;
			}
        }

        public bool GetKinematic()
        {
            if (_rigidBody != null)
			{
				return _rigidBody.isKinematic;
			}
			else
			{
				return false;
			}
        }

        public bool GetIsTrigger()
        {
            if (_collider != null)
			{
				return _collider.isTrigger;
			}
			else
			{
				return false;
			}
        }
		public bool ToggleControl()
		{
            SetIsGrabbed(!_isGrabbed);			
            DispatchPickedEvent(_isGrabbed);
			return _isGrabbed;
		}

		protected virtual void SetIsGrabbed(bool newValue)
		{
			if (newValue)
			{
				SystemEventController.Instance.DispatchSystemEvent(EventObjectPickableGrabbed, this.gameObject);
			}
			else
			{
				SystemEventController.Instance.DispatchSystemEvent(EventObjectPickableReleased, this.gameObject);
			}
			_isGrabbed = newValue;
		}

		public virtual void ActivatePhysics(bool activation)
        {
			_rigidBody.useGravity = activation;
			_rigidBody.isKinematic = !activation;
			_collider.isTrigger = !activation;
        }

		public virtual void ForcePhysics(bool gravity, bool kinematic, bool trigger)
        {
			_rigidBody.useGravity = gravity;
			_rigidBody.isKinematic = kinematic;
			_collider.isTrigger = trigger;
        }

        public Vector3 GetPositionRaycastAgainstSurface(int layerMaskSurface, ref RaycastHit hitData)
        {
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
            Vector3 positionController = VRInputController.Instance.VRController.CurrentController.transform.position;
            Vector3 forwardController = VRInputController.Instance.VRController.CurrentController.transform.forward;

            Vector3 positionPlacement = RaycastingTools.GetRaycastOriginForward(positionController, forwardController, ref hitData, 10000, layerMaskSurface);
#else
            Vector3 positionPlacement = RaycastingTools.GetMouseCollisionPoint(Camera.main, ref hitData, layerMaskSurface);
#endif
            return positionPlacement;
        }

        protected virtual void MoveToPosition()
        {
            Vector3 positionController = Camera.main.transform.position;
            Vector3 forwardController = Camera.main.transform.forward;
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
            positionController = VRInputController.Instance.VRController.CurrentController.transform.position;
            forwardController = VRInputController.Instance.VRController.CurrentController.transform.forward;
#endif

			Vector3 nextPosition = positionController + forwardController * grabbedObjectDistance;
			this.transform.position = nextPosition;
        }

		protected Vector3 GetShiftFromFloor()
		{
			return new Vector3(0, SizeCube, 0) * Mathf.Abs(this.transform.localScale.y/2);
		}

		public void FloorAdjustment()
		{
			RaycastHit hitData = new RaycastHit();
			Vector3 positionFloor = RaycastingTools.GetRaycastOriginForward(this.transform.position, Vector3.down, ref hitData, 1000, _floorMaskLayer);
			if (positionFloor != Vector3.zero)
			{
				this.transform.position = positionFloor + GetShiftFromFloor();
			}			
		}

        protected virtual void Update()
        {
			if (_isEnabled)
			{
				if (_isGrabbed)
				{
#if ENABLE_ULTIMATEXR
					if (!_grabbableObject.IsBeingGrabbed)
					{
						MoveToPosition();
					}
#else
					MoveToPosition();
#endif
				}
			}
        }

    }
}
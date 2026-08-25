using System;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.VR
{
	public class TeleportController : MonoBehaviour
	{
		public const string EventTeleportControllerStarted = "EventTeleportControllerStarted";
        public const string EventTeleportControllerEnable = "EventTeleportControllerEnable";
        public const string EventTeleportControllerActivation = "EventTeleportControllerActivation";
        public const string EventTeleportControllerConfirmation = "EventTeleportControllerConfirmation";
        public const string EventTeleportControllerKeyReleased = "EventTeleportControllerKeyReleased";
        public const string EventTeleportControllerDeactivation = "EventTeleportControllerDeactivation";
        public const string EventTeleportControllerAllowedLayers = "EventTeleportControllerAllowedLayers";
        public const string EventTeleportControllerDestroyedMarker = "EventTeleportControllerDestroyedMarker";
		public const string EventTeleportControllerUpdateTransformForward = "EventTeleportControllerUpdateTransformForward";

        private const int ParabolaPrecision = 450;

		private const float SensitivityTriggerTeleport = 0.7f;

        [SerializeField] private XR_HAND TeleportHand = XR_HAND.none;
        [SerializeField] private bool IsHandTracking = false;
		[SerializeField] private bool IsDirectional = false;
        [SerializeField] private GameObject CameraController;
        [SerializeField] private GameObject MarkerDestinationNoDirection;
		[SerializeField] private GameObject MarkerDestinationDirectional;
        [SerializeField] private Material LineMaterial;
        [SerializeField] private LayerMask AllowedLayers;
        [SerializeField] private LayerMask ForbiddenLayers;        
        [SerializeField] private float MaxTeleportDistance = 4f;
        [SerializeField] private float MatScale = 5;
        [SerializeField] private Vector3 DestinationNormal;
        [SerializeField] private float LineWidth = 0.05f;
        [SerializeField] private float Curvature = 0.2f;
        [SerializeField] private Color GoodDestinationColor = new Color(0, 0.6f, 1f, 0.2f);
        [SerializeField] private Color BadDestinationColor = new Color(0.8f, 0, 0, 0.2f);
        [SerializeField] private List<GameObject> TargetsAllowedDestination = new List<GameObject>();

        private bool _enabled = false;
        private Vector3 _FinalHitLocation;
        private Vector3 _FinalHitNormal;        
        private GameObject _FinalHitGameObject;
        private LineRenderer _lineRenderer;

        private GameObject _lineParent;
        private GameObject _line1;
        private GameObject _line2;

        private GameObject _markerDestination;

        private Transform _forwardDirection;
		private Transform _originalForwardDirection;

        private bool _activateTeleport = false;
        private bool _calculateParabola = false;

        private bool _hitSomething = false;
		private Vector2 _joystickTeleport;
		private Quaternion _finalRotation;

        public bool ActivateTeleport
        {
            get { return _activateTeleport; }
        }
        public Transform ForwardDirection
        {
            get { return _forwardDirection; }
            set {  _forwardDirection = value; }
        }
		public Transform OriginalForwardDirection
        {
            get { return _originalForwardDirection; }
        }
		public XR_HAND GetHand()
		{
			return TeleportHand;
		}
		public bool IsRightHand()
		{
			return TeleportHand == XR_HAND.right;
		}

        public void Start()
        {
			VRInputController.Instance.Event += OnVREvent;
            
            _lineParent = new GameObject("Line");
			_lineParent.transform.parent = this.gameObject.transform;
            _lineParent.transform.localScale = CameraController.transform.localScale;
            _line1 = new GameObject("Line1");

            _line1.transform.SetParent(_lineParent.transform);
            _lineRenderer = _line1.AddComponent<LineRenderer>();
            _line2 = new GameObject("Line2");
            _line2.transform.SetParent(_lineParent.transform);
            _lineRenderer.startWidth = LineWidth * CameraController.transform.localScale.magnitude;
            _lineRenderer.endWidth = LineWidth * CameraController.transform.localScale.magnitude;
            _lineRenderer.material = LineMaterial;
            _lineRenderer.SetPosition(0, Vector3.zero);
            _lineRenderer.SetPosition(1, Vector3.zero);

			_originalForwardDirection = this.transform;
            _forwardDirection = this.transform;
        }

		public void OnDestroy()
        {
            if (VRInputController.Instance != null) VRInputController.Instance.Event -= OnVREvent;
        }

        private void DestroyMarkerTeleport()
        {
            if (_markerDestination != null)
            {
                GameObject.Destroy(_markerDestination);
                _markerDestination = null;
            }
            if (_lineRenderer != null)
            {
                if (_lineRenderer.gameObject != null)
                {
                    _lineRenderer.gameObject.SetActive(false);
                }
            }
			VRInputController.Instance.DispatchVREvent(EventTeleportControllerDestroyedMarker);
        }

        private bool AllowDestination(Ray ray, out RaycastHit hit, float raycastLength)
        {
            bool hitSomething = Physics.Raycast(ray, out hit, raycastLength, AllowedLayers);

            if (hitSomething)
            {
                if (TargetsAllowedDestination == null)
                {
                    return true;
                }
                else
                {
                    if (TargetsAllowedDestination.Count == 0)
                    {
                        return true;
                    }
                    else
                    {
                        foreach (GameObject item in TargetsAllowedDestination)
                        {
                            if (item == hit.collider.gameObject)
                            {
                                return true;
                            }
                        }
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
        }

        internal void ComputeParabola()
        {
            if (_markerDestination == null)
            {
				if (IsDirectional)
				{
					_markerDestination = Instantiate(MarkerDestinationDirectional);
				}
				else
				{
					_markerDestination = Instantiate(MarkerDestinationNoDirection);
				}
				_markerDestination.transform.localScale = new Vector3(1, 1, 1);
                _lineRenderer.gameObject.SetActive(true);
            }

            //	Line renderer position storage (two because line renderer texture will stretch if one is used)
            List<Vector3> positions1 = new List<Vector3>();

            //	first Vector3 positions array will be used for the curve and the second line renderer is used for the straight down after the curve
            float totalDistance1 = 0;

            //	Variables need for curve
            Quaternion currentRotation = _forwardDirection.transform.rotation;
            Vector3 originalPosition = transform.position;
            Vector3 currentPosition = transform.position;
            Vector3 lastPostion;
            positions1.Add(currentPosition);

            lastPostion = transform.position - _forwardDirection.forward;
            Vector3 currentDirection = _forwardDirection.forward;
            Vector3 downForward = new Vector3(_forwardDirection.forward.x * 0.01f, -1, _forwardDirection.forward.z * 0.01f);
            RaycastHit hit = new RaycastHit();
            _FinalHitLocation = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int step = 0; step < ParabolaPrecision; step++)
            {
                Quaternion downRotation = Quaternion.LookRotation(downForward);
                currentRotation = Quaternion.RotateTowards(currentRotation, downRotation, Curvature);

                Ray newRay = new Ray(currentPosition, currentPosition - lastPostion);

                float length = (MaxTeleportDistance * 0.01f) * CameraController.transform.localScale.magnitude;
                if (currentRotation == downRotation)
                {
                    length = (MaxTeleportDistance * MatScale) * CameraController.transform.localScale.magnitude;
                    positions1.Add(currentPosition);
                }

                float raycastLength = length * 1.1f;

                if (Physics.Raycast(newRay, out hit, raycastLength, ForbiddenLayers)) break;

                //	Check if we hit something
                _hitSomething = AllowDestination(newRay, out hit, raycastLength);

                // don't allow to teleport to negative normals (we don't want to be stuck under floors)
                if ((hit.normal.y > 0) && (Vector3.Distance(originalPosition, hit.point) > 1))
                {
                    _FinalHitLocation = hit.point;
                    _FinalHitNormal = hit.normal;
                    _FinalHitGameObject = hit.collider.gameObject;

                    totalDistance1 += (currentPosition - _FinalHitLocation).magnitude;
                    positions1.Add(_FinalHitLocation);

                    DestinationNormal = _FinalHitNormal;

                    break;
                }

                //	Convert the rotation to a forward vector and apply to our current position
                currentDirection = currentRotation * Vector3.forward;
                lastPostion = currentPosition;
                currentPosition += currentDirection * length;

                totalDistance1 += length;
                positions1.Add(currentPosition);

                if (currentRotation == downRotation)
                    break;
            }

            _lineRenderer.enabled = true;

            _lineRenderer.positionCount = positions1.Count;
            _lineRenderer.SetPositions(positions1.ToArray());

            _markerDestination.transform.position = positions1[positions1.Count - 1];
			if (IsDirectional)
			{
				Vector3 eulerDirection = new Vector3(0, 180 - yourvrexperience.Utils.Utilities.GetAngleFromNormal(_joystickTeleport), 0);
				Vector3 eulerBase = new Vector3(0, _forwardDirection.transform.rotation.eulerAngles.y, 0);
				_finalRotation = Quaternion.Euler(eulerBase) * Quaternion.Inverse(Quaternion.Euler(eulerDirection)); 
				_markerDestination.transform.rotation = _finalRotation;
			}

            _lineRenderer.material.color = (_hitSomething ? GoodDestinationColor : BadDestinationColor);
        }

        private void OnVREvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(EventTeleportControllerUpdateTransformForward))
			{
				bool shouldUpdate = (bool)parameters[0];
				if (shouldUpdate)
				{
					if (TeleportHand == (XR_HAND)parameters[1])
					{
						_forwardDirection = (Transform)parameters[2];
					}					
				}
				else
				{
					_forwardDirection = _originalForwardDirection;
				}
			}
            if (nameEvent.Equals(EventTeleportControllerAllowedLayers))
            {
                int valueLayer = (int)parameters[0];
                AllowedLayers = valueLayer;
            }
            if (nameEvent.Equals(EventTeleportControllerEnable))
            {
                _enabled = (bool)parameters[0];
                if (!_enabled)
                {
                    _activateTeleport = false;
                    _calculateParabola = false;
                    DestroyMarkerTeleport();
                }
				else
				{
					XR_HAND enabledHand = (XR_HAND)parameters[1];
					bool isDirectional = (bool)parameters[2];
					if (isDirectional != IsDirectional)
					{
						if (_markerDestination != null)
						{
							GameObject.Destroy(_markerDestination);
							_markerDestination = null;
						}
					}					
					IsDirectional = isDirectional;					 
					if ((enabledHand != TeleportHand) && (enabledHand != XR_HAND.both))
					{
						_enabled = false;
					}
				}
            }
            if (nameEvent.Equals(EventTeleportControllerActivation))
            {
				if (TeleportHand == (XR_HAND)parameters[0])
				{
					_activateTeleport = true;
					_calculateParabola = true;
				}
            }
            if (nameEvent.Equals(EventTeleportControllerDeactivation))
            {
				_activateTeleport = false;
				_calculateParabola = false;
				DestroyMarkerTeleport();
            }
            if (nameEvent.Equals(EventTeleportControllerKeyReleased))
            {
				if (TeleportHand == (XR_HAND)parameters[0])
				{
					if (_activateTeleport)
					{
						if (_calculateParabola)
						{
							if (_markerDestination != null)
							{
								_activateTeleport = false;
								_calculateParabola = false;
								Vector3 shiftToTarget = _markerDestination.transform.position - VRInputController.Instance.CameraGO.transform.position;
								DestroyMarkerTeleport();
								if (_hitSomething)
								{
									if (IsDirectional)
									{
										VRInputController.Instance.DispatchVREvent(EventTeleportControllerConfirmation, shiftToTarget, _finalRotation, _FinalHitGameObject);
									}
									else
									{
										VRInputController.Instance.DispatchVREvent(EventTeleportControllerConfirmation, shiftToTarget, Quaternion.identity, _FinalHitGameObject);
									}									
								}
							}
						}
					}
				}
            }
        }

        private void Update()
        {
            if (_enabled)
            {
                _joystickTeleport = VRInputController.Instance.VRController.GetVector2Joystick(TeleportHand);
				if (!_activateTeleport)
				{
					if (_joystickTeleport.sqrMagnitude > SensitivityTriggerTeleport)
					{
						VRInputController.Instance.DispatchVREvent(EventTeleportControllerActivation, TeleportHand);
					}
				}
				else
				{
					if (_joystickTeleport.sqrMagnitude < SensitivityTriggerTeleport)
					{
						VRInputController.Instance.DispatchVREvent(EventTeleportControllerKeyReleased, TeleportHand);
					}
				}

				if (_activateTeleport)
				{
					if (_calculateParabola)
					{
						ComputeParabola();
					}
				}
            }
        }
    }
}
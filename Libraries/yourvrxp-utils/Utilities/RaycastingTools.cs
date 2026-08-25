using System;
using System.Collections.Generic;
using UnityEngine;
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
using yourvrexperience.VR;
#endif

namespace yourvrexperience.Utils
{
	public static class RaycastingTools
	{
		public static Vector3 GetMouseCollisionPoint(Camera camera, ref RaycastHit hitCollision, int mask = -1)
		{
			Ray ray = camera.ScreenPointToRay(Input.mousePosition);

			if (mask != -1)
			{
				if (Physics.Raycast(ray, out hitCollision, Mathf.Infinity, mask))
				{
					return hitCollision.point;
				}
			}
			else
			{
				if (Physics.Raycast(ray, out hitCollision, Mathf.Infinity))
				{
					return hitCollision.point;
				}
			}

			return Vector3.zero;
		}


		public static GameObject GetMouseCollisionObject(Camera camera, ref RaycastHit hitCollision, int mask = -1)
		{
			Ray ray = camera.ScreenPointToRay(Input.mousePosition);


			if (mask != -1)
			{
				if (Physics.Raycast(ray, out hitCollision, Mathf.Infinity, mask))
				{
					return hitCollision.collider.gameObject;
				}
			}
			else
			{
				if (Physics.Raycast(ray, out hitCollision, Mathf.Infinity))
				{
					return hitCollision.collider.gameObject;
				}
			}

			return null;
		}

		public static Vector3 GetRaycastOriginForward(Vector3 origin, Vector3 forward, ref RaycastHit hitCollision, float distance, int mask = -1)
		{
			Vector3 fwd = forward.normalized;

			if (mask != -1)
			{
				if (Physics.Raycast(origin, fwd, out hitCollision, distance, mask))
				{
					return hitCollision.point;
				}
			}
			else
			{
				if (Physics.Raycast(origin, fwd, out hitCollision, distance))
				{
					return hitCollision.point;
				}
			}

			return Vector3.zero;
		}

		public static bool IsFreePathToTarget(GameObject origin, GameObject target, string ignoreLayer = "Floor")
		{
			Vector3 sizeOrigin = ColliderInfoExtensions.GetColliderSize(origin);
			Vector3 sizeTarget = ColliderInfoExtensions.GetColliderSize(target);
			
			Vector3 normalToTarget = target.gameObject.transform.position - origin.gameObject.transform.position;
			normalToTarget.Normalize();

			Vector3 startingPoint = normalToTarget * (sizeOrigin.sqrMagnitude/2) + origin.gameObject.transform.position;
			Vector3 endingPoint = target.gameObject.transform.position;

			RaycastHit hitCollision = new RaycastHit();
			float distance = Vector3.Distance(startingPoint, endingPoint);
			if (Physics.Raycast(startingPoint, normalToTarget, out hitCollision, distance, ~LayerMask.GetMask(ignoreLayer)))
			{
				if (hitCollision.collider.gameObject == target)
				{
					return true;
				}
				else
				{
					return false;
				}				
			}

			return true;
		}

		public static Vector3 GetRaycastOriginForwardIgnoreLayer(Vector3 origin, Vector3 forward, ref RaycastHit hitCollision, float distance, int mask = -1)
		{
			Vector3 fwd = forward.normalized;

			if (mask != -1)
			{				
				if (Physics.Raycast(origin, fwd, out hitCollision, distance, ~mask))
				{
					return hitCollision.point;
				}
			}
			else
			{
				if (Physics.Raycast(origin, fwd, out hitCollision, distance))
				{
					return hitCollision.point;
				}
			}

			return Vector3.zero;
		}

		public static GameObject GetRaycastObject(Vector3 origin, Vector3 forward, float distance, ref RaycastHit hitCollision, int mask = -1)
		{
			Vector3 fwd = forward.normalized;

			if (mask != -1)
			{
				if (Physics.Raycast(origin, fwd, out hitCollision, distance, mask))
				{
					return hitCollision.collider.gameObject;
				}
			}
			else
			{
				if (Physics.Raycast(origin, fwd, out hitCollision, distance))
				{
					return hitCollision.collider.gameObject;
				}
			}
			return null;
		}

		public static void GetMouseAllCollisions(Camera camera, ref RaycastHit[] hitCollision, int masks)
		{
			Ray ray = camera.ScreenPointToRay(Input.mousePosition);

			hitCollision = Physics.RaycastAll(ray, Mathf.Infinity, masks);
		}

		public static bool IsThereObstacleBetweenPosition(Vector3 origin, Vector3 target, params string[] _masksToIgnore)
		{
			Ray ray = new Ray(origin, (target - origin).normalized);
			RaycastHit hit;
			int layerMask = Physics.IgnoreRaycastLayer;
			if (_masksToIgnore != null)
			{
				for (int i = 0; i < _masksToIgnore.Length; i++)
				{
					layerMask |= (1 << LayerMask.NameToLayer(_masksToIgnore[i]));
				}
				layerMask = ~layerMask;
			}
			if (Physics.Raycast(ray, out hit, Vector3.Distance(origin, target), layerMask))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public static Vector3 GetTargetPositionMouse(Camera camera, ref RaycastHit collisionObject, float distanceCollision)
		{
			Vector3 currentCollisionPoint = RaycastingTools.GetMouseCollisionPoint(camera, ref collisionObject, Physics.DefaultRaycastLayers);
			Vector3 currentForwardMouse = (currentCollisionPoint - camera.transform.position).normalized;
			return camera.transform.position + currentForwardMouse * distanceCollision;
		}

		public static bool GetCollidedObjectBySegmentTargetIgnore(Vector3 _goalPosition, Vector3 _originPosition, params string[] _masksToIgnore)
		{
			Vector3 fwd = new Vector3(_goalPosition.x - _originPosition.x, _goalPosition.y - _originPosition.y, _goalPosition.z - _originPosition.z);
			float distanceTotal = Vector3.Distance(_goalPosition, _originPosition);
			fwd.Normalize();
			Ray ray = new Ray();
			ray.direction = fwd;
			RaycastHit hitCollision = new RaycastHit();

			int layerMask = Physics.IgnoreRaycastLayer;
			if (_masksToIgnore != null)
			{
				for (int i = 0; i < _masksToIgnore.Length; i++)
				{
					layerMask |= (1 << LayerMask.NameToLayer(_masksToIgnore[i]));
				}
				layerMask = ~layerMask;
			}
			if (layerMask == 0)
			{
				if (Physics.Raycast(_originPosition, fwd, out hitCollision, distanceTotal))
				{
					return true;
				}
			}
			else
			{
				if (Physics.Raycast(_originPosition, fwd, out hitCollision, distanceTotal, layerMask))
				{
					return true;
				}
			}

			return false;
		}

		public static RaycastHit GetRaycastHitInfoByRay(Vector3 _origin, Vector3 _forward, params string[] _masksToIgnore)
		{
			Vector3 fwd = _forward;
			fwd.Normalize();
			RaycastHit hitCollision = new RaycastHit();

			int layerMask = Physics.IgnoreRaycastLayer;
			if (_masksToIgnore != null)
			{
				for (int i = 0; i < _masksToIgnore.Length; i++)
				{
					layerMask |= (1 << LayerMask.NameToLayer(_masksToIgnore[i]));
				}
				layerMask = ~layerMask;
			}
			Physics.Raycast(_origin, fwd, out hitCollision, Mathf.Infinity, layerMask);
			return hitCollision;
		}

		public static bool GetRaycastHitInfoByRay(Vector3 _origin, Vector3 _forward, ref RaycastHit _hitCollision, params string[] _masksToIgnore)
		{
			Vector3 fwd = _forward;
			fwd.Normalize();

			int layerMask = Physics.IgnoreRaycastLayer;
			if (_masksToIgnore != null)
			{
				for (int i = 0; i < _masksToIgnore.Length; i++)
				{
					layerMask |= (1 << LayerMask.NameToLayer(_masksToIgnore[i]));
				}
				layerMask = ~layerMask;
			}
			if (Physics.Raycast(_origin, fwd, out _hitCollision, Mathf.Infinity, layerMask))
			{
				return true;
			}
			return false;
		}

		public static bool GetRaycastAgainstMask(Vector3 _origin, Vector3 _direction, out Vector3 point, int mask = -1)
		{
			point = Vector3.zero;
			Ray ray = new Ray(_origin, _direction.normalized);

			bool hit = (mask != -1)
				? Physics.Raycast(ray, out RaycastHit h, Mathf.Infinity, mask)
				: Physics.Raycast(ray, out h, Mathf.Infinity);

			if (hit) { point = h.point; return true; }
			return false;
		}


		public static Vector3 GetPositionInteraction(RectTransform targetRect, bool debugObject = false)
		{
			Vector3 collidedPositionRaycast = Vector3.zero;
			Vector3 screenPosition = Vector3.zero;
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
			Vector3 positionCurrentController = Vector3.zero;
			Vector3 forwardCurrentController = Vector3.zero;
			if (VRInputController.Instance.VRController.CurrentController != null)
			{
				positionCurrentController = VRInputController.Instance.VRController.CurrentController.transform.position;
				forwardCurrentController = VRInputController.Instance.VRController.CurrentController.transform.forward;

				RaycastHit hitPositionData = new RaycastHit();
				Vector3 positionRaycast = RaycastingTools.GetRaycastOriginForward(positionCurrentController, forwardCurrentController, ref hitPositionData, 100);
				collidedPositionRaycast = targetRect.InverseTransformPoint(positionRaycast);
                if (debugObject)
                {
                    GameObject objReference = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    objReference.transform.position = positionRaycast;
                    objReference.transform.localScale = new Vector3(0.05f,0.05f,0.05f);
                }
			}
#else
			collidedPositionRaycast = targetRect.InverseTransformPoint(Input.mousePosition);
#endif
			return collidedPositionRaycast;
		}

		public static Vector3 CheckCollisionPointUI(RectTransform area, ref Vector3 screenPosition, ref Vector3 collidedPositionRaycast, Transform content)
		{
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
			Vector3 positionCurrentController = Vector3.zero;
			Vector3 forwardCurrentController = Vector3.zero;
			if (VRInputController.Instance.VRController.CurrentController != null)
			{
				positionCurrentController = VRInputController.Instance.VRController.CurrentController.transform.position;
				forwardCurrentController = VRInputController.Instance.VRController.CurrentController.transform.forward;

				RaycastHit hitPositionData = new RaycastHit();
				collidedPositionRaycast = RaycastingTools.GetRaycastOriginForward(positionCurrentController, forwardCurrentController, ref hitPositionData, 100);
				if (content != null)
				{
					collidedPositionRaycast = content.GetComponent<RectTransform>().InverseTransformPoint(collidedPositionRaycast);
				}				
			}
#else
			screenPosition = Input.mousePosition;
			Vector2 localPosition = Vector2.zero;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPosition, null, out localPosition);
			collidedPositionRaycast = localPosition;
#endif
			return collidedPositionRaycast;
		}

#if ENABLE_OCULUS
		public static void HandVRRaycastObject(bool rightHand, float distance = 10, string debugMessage = "", bool debugSphere = false, float debugTime = 4, float debugSphereSize = 0.05f)
		{
			OVRInput.Controller selectedController = OVRInput.Controller.RTouch;
			if (rightHand)
			{
				selectedController = OVRInput.Controller.RTouch;
			}
			else
			{
				selectedController = OVRInput.Controller.LTouch;
			}
			bool isHandTriggerPressed = ((OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger,OVRInput.Controller.LTouch) > 0.4f)  || (OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger,OVRInput.Controller.RTouch) > 0.4f));
			if (isHandTriggerPressed && OVRInput.GetDown(OVRInput.Button.One, selectedController))
			{
				OVRHand[] handsVR = GameObject.FindObjectsOfType<OVRHand>();
				foreach (OVRHand hand in handsVR)
				{
					bool shouldRunCasting = false;
					if (rightHand)
					{
						shouldRunCasting = !hand.IsDominantHand;
					}
					else
					{
						shouldRunCasting = hand.IsDominantHand;
					}
					if (shouldRunCasting)
					{
						Vector3 originHand = hand.transform.position;
						Vector3 forwardHand = hand.transform.forward;
						RaycastHit rayHit = new RaycastHit();
						GameObject collidedThing =	RaycastingTools.GetRaycastObject(originHand, forwardHand, distance, ref rayHit);
						if (collidedThing != null)
						{
							if (debugMessage.Length > 0)
							{
								Debug.Log("<color=red>"+ debugMessage + " " + yourvrexperience.Utils.Utilities.GetFullPathNameGO(collidedThing)  + "</color>");
							}
							Vector3 collidedPoint = rayHit.point;
							if (debugSphere)
							{
								GameObject refSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
								refSphere.transform.localScale = new Vector3(debugSphereSize, debugSphereSize, debugSphereSize);
								refSphere.transform.position = collidedPoint;
								GameObject.Destroy(refSphere, debugTime);
							}
						}
					}
				}
			}
		}
#endif
	}
}

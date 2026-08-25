using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace yourvrexperience.Utils
{
	public delegate void PickedObjectEvent(bool isGrabbed);

	public interface IObjectPickable
	{
		event PickedObjectEvent PickedEvent;   
		bool IsGrabbed { get; set; }
		float GrabbedObjectDistance { get; set; }

		void ActivatePhysics(bool activation);
		void ForcePhysics(bool gravity, bool kinematic, bool trigger);
		bool GetGravity();
		bool GetKinematic();
		bool GetIsTrigger();
		bool ToggleControl();
		void FloorAdjustment();
		void ResetForces();
		Rigidbody GetRigidBody();
	}
}
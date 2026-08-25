using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace yourvrexperience.Utils
{
    public class PatrolWaypoints : StateMachine
    {
        public const string EventPatrolWaypointsHasStarted = "EventPatrolWaypointsHasStarted";
        public const string EventPatrolWaypointsHasBeenDestroyed = "EventPatrolWaypointsHasBeenDestroyed";
        public const string EventPatrolWaypointsWaypointToCero = "EventPatrolWaypointsWaypointToCero";

		public const string SeparatorPatrol = "<patrol>";
		public const string SeparatorWaypoints = "<waypoint>";
		public const string SeparatorMasks = "<mask>";

        public delegate void MovingEvent();
        public delegate void StandingEvent();

        public event MovingEvent MoveEvent;
        public event StandingEvent StandEvent;

        public void DispatchMovingEvent()
        {
            if (MoveEvent != null)
                MoveEvent();
        }
        public void DispatchStandingEvent()
        {
            if (StandEvent != null)
                StandEvent();
        }


        public enum WaypointActions { Synchronization = 0, UpdateWaypoint, GoToWaypoint, StayInWaypoint, LookToWaypoint }

        public Waypoint[] Waypoints;
        public int CurrentWaypoint = 0;
        public float Speed = 5;
        public bool EnableRotationToWaypoint = false;
        public bool AutoStart = false;
        public string[] MasksToIgnore;

        private bool _activated = false;
        private float _timeDone = 0;

        private RotateToTarget _rotateComponent;
        private bool _hasRigidBody;

        private NavMeshAgent _navigationComponent;
        private bool _activatedNavigation = false;
        private Vector3 _currenAnchorPoint = Vector3.zero;

        public float TimeDone
        {
            get { return _timeDone; }
            set { _timeDone = value; }
        }
		public bool Activated
		{
			get { return _activated; }
		}

        protected virtual void Start()
        {
            _rotateComponent = this.GetComponent<RotateToTarget>();
            _hasRigidBody = this.GetComponent<Rigidbody>() != null;

            for (int i = 0; i < Waypoints.Length; i++)
            {
                if (Waypoints[i] != null)
                {
                    if (Waypoints[i].Target != null)
                    {
                        Waypoints[i].Position = Waypoints[i].Target.transform.position;
                        GameObject.Destroy(Waypoints[i].Target);
                    }
                }
            }

            _navigationComponent = this.GetComponent<NavMeshAgent>();
            if (_navigationComponent != null) _navigationComponent.enabled = false;

            if (AutoStart)
            {
                ActivatePatrol(10);
            }

            SystemEventController.Instance.DispatchSystemEvent(EventPatrolWaypointsHasStarted, this, AutoStart);
        }

        protected virtual void OnDestroy()
        {
            if (SystemEventController.Instance != null) SystemEventController.Instance.DispatchSystemEvent(EventPatrolWaypointsHasBeenDestroyed, this);
        }
        
        public void CopyPatrolWaypoints(List<Waypoint> waypoints)
        {
            for (int i = 0; i < Waypoints.Length; i++)
            {
                waypoints.Add(Waypoints[i].Clone());
            }
        }

        public string Pack()
        {
			string output = "";
			string packetWaypoints = "";
            for (int i = 0; i < Waypoints.Length; i++)
            {
                packetWaypoints += Waypoints[i].ToString();
				if (i + 1 < Waypoints.Length)
				{
					packetWaypoints += SeparatorWaypoints;
				}
            }

			output += packetWaypoints + SeparatorPatrol;
			output += Speed + SeparatorPatrol;
			output += EnableRotationToWaypoint + SeparatorPatrol;
			output += AutoStart + SeparatorPatrol;

			string maskToIgnore = "";
			for (int i = 0; i < MasksToIgnore.Length; i++)
			{
				maskToIgnore += MasksToIgnore[i];
				if (i + 1 < MasksToIgnore.Length)
				{
					maskToIgnore += SeparatorMasks;
				}
			}

			output += maskToIgnore;

			return output;
        }

		public void UnPack(string data)
		{
			string[] dataArray = data.Split(new String[]{SeparatorPatrol}, data.Length, StringSplitOptions.None);

			string[] waypointsArray = dataArray[0].Split(new String[]{SeparatorWaypoints}, data.Length, StringSplitOptions.None);
			Waypoints = new Waypoint[waypointsArray.Length];
			for (int i = 0; i < Waypoints.Length; i++)
			{
				Waypoints[i] = new Waypoint(waypointsArray[i]);
			}

			Speed = float.Parse(dataArray[1]);
			EnableRotationToWaypoint = bool.Parse(dataArray[2]);
			AutoStart = bool.Parse(dataArray[3]);

			string[] masksArray = dataArray[4].Split(new String[]{SeparatorMasks}, data.Length, StringSplitOptions.None);
			MasksToIgnore = new string[masksArray.Length];
			for (int i = 0; i < MasksToIgnore.Length; i++)
			{
				MasksToIgnore[i] = masksArray[i];
			}
		}

        public void SetPatrolWaypoints(List<Waypoint> waypoints)
        {
            Waypoints = new Waypoint[waypoints.Count];
            for (int i = 0; i < waypoints.Count; i++)
            {
                Waypoints[i] = waypoints[i].Clone();
            }
        }

        private Vector3 GetPreviousPositionWaypoint(int _waypointIndex)
        {
            int finalIndexCheck = _waypointIndex;
            do
            {
                finalIndexCheck = finalIndexCheck - 1;
                if (finalIndexCheck < 0)
                {
                    finalIndexCheck = Waypoints.Length - 1;
                }
            } while (Waypoints[finalIndexCheck].Action != Waypoint.ActionsPatrol.GO);

            return Waypoints[finalIndexCheck].Position;
        }

        private void WalkToCurrentWaypoint()
        {
            _timeDone += Time.deltaTime;
            float duration = Waypoints[CurrentWaypoint].Duration;
            Vector3 origin = GetPreviousPositionWaypoint(CurrentWaypoint);
            Vector3 forwardTarget = (Waypoints[CurrentWaypoint].Position - origin);
            float increaseFactor = _timeDone / duration;
            Vector3 nextPosition = origin + (increaseFactor * forwardTarget);
            if (_hasRigidBody)
            {
                transform.GetComponent<Rigidbody>().MovePosition(new Vector3(nextPosition.x, transform.position.y, nextPosition.z));
            }
            else
            {
                transform.position = nextPosition;
            }
        }

        private void WalkWithSpeedToWaypoint()
        {
            Vector3 directionToTarget = Waypoints[CurrentWaypoint].Position - this.transform.position;
            directionToTarget.Normalize();
            Vector3 nextPosition = this.transform.position + (directionToTarget * Speed * Time.deltaTime);
            if (_hasRigidBody)
            {
                transform.GetComponent<Rigidbody>().MovePosition(new Vector3(nextPosition.x, transform.position.y, nextPosition.z));
            }
            else
            {
                transform.position = nextPosition;
            }

        }

        private bool ReachedCurrentWaypoint()
        {
            if (Vector3.Distance(this.transform.position, Waypoints[CurrentWaypoint].Position) < 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ActivatePatrol(float speed = -1)
        {
            if (Waypoints.Length > 0)
            {
                Speed = (speed!=-1?speed:Speed);
                _activated = true;
                ChangeState((int)WaypointActions.Synchronization);
            }
        }

        public bool AreThereAnyWaypoints()
        {
            return Waypoints.Length > 0;
        }

        public void DeactivatePatrol()
        {
            _activated = false;
			if (IsThereRotationComponent())
			{
				_rotateComponent.DeactivateRotation();
			}
            if (_navigationComponent != null)
            {
                if (_navigationComponent.enabled) _navigationComponent.enabled = false;
            }
        }

        private bool IsThereRotationComponent()
        {
            return _rotateComponent != null;
        }

        protected override void ChangeState(int newState)
        {
            base.ChangeState(newState);

            switch ((WaypointActions)_state)
            {
                case WaypointActions.Synchronization:
                    Vector3 previousGoWaypoint = Waypoints[CurrentWaypoint].Position;
                    _activatedNavigation = false;
                    if (_navigationComponent != null)
                    {
                        Vector3 origin = this.transform.position;
                        origin.y = transform.position.y;
                        Vector3 target = previousGoWaypoint;
                        target.y = transform.position.y;
                        if (RaycastingTools.IsThereObstacleBetweenPosition(origin, target, MasksToIgnore))
                        {
                            if (_navigationComponent != null)
                            {
                                _activatedNavigation = true;
                                _navigationComponent.enabled = true;
                                _navigationComponent.SetDestination(target);
                            }
                        }
                        else
                        {
                            if (_navigationComponent != null)
                            {
                                _activatedNavigation = false;
                                if (_navigationComponent.enabled)
                                {
                                    yourvrexperience.Utils.Utilities.ActivatePhysics(this.gameObject, false);
                                    _navigationComponent.isStopped = true;
                                    _navigationComponent.enabled = false;
                                }
                            }
                        }
                    }
                    if (!_activatedNavigation)
                    {
                        if (IsThereRotationComponent())
                        {
                            _rotateComponent.ActivateRotation(previousGoWaypoint);
                        }
                    }
                    DispatchMovingEvent();
                    break;

                case WaypointActions.UpdateWaypoint:
                    break;

                case WaypointActions.GoToWaypoint:
                    DispatchMovingEvent();
                    yourvrexperience.Utils.Utilities.ActivatePhysics(this.gameObject, true);
                    break;

                case WaypointActions.StayInWaypoint:
                    _currenAnchorPoint = this.transform.position;
                    DispatchStandingEvent();
                    if (IsThereRotationComponent())
                    {
                        _rotateComponent.DeactivateRotation();
                    }
                    yourvrexperience.Utils.Utilities.ActivatePhysics(this.gameObject, true);
                    break;

                case WaypointActions.LookToWaypoint:
                    _currenAnchorPoint = this.transform.position;
                    DispatchStandingEvent();
                    yourvrexperience.Utils.Utilities.ActivatePhysics(this.gameObject, true);
                    break;
            }
        }

        public void UpdateLogic()
        {
            if (_activated == false) return;

            switch ((WaypointActions)_state)
            {
                case WaypointActions.Synchronization:
                    WalkWithSpeedToWaypoint();
                    if (IsThereRotationComponent())
                    {
						_rotateComponent.UpdateLogic();
                    }
                    if (ReachedCurrentWaypoint() == true)
                    {
                        ChangeState((int)WaypointActions.UpdateWaypoint);
                    }
                    break;

                case WaypointActions.UpdateWaypoint:
                    _activatedNavigation = false;
                    if (_navigationComponent != null)
                    {
                        if (_navigationComponent.enabled)
                        {
                            yourvrexperience.Utils.Utilities.ActivatePhysics(this.gameObject, false);
                            _navigationComponent.isStopped = true;
                            _navigationComponent.enabled = false;
                        }
                    }
                    CurrentWaypoint++;
                    if (CurrentWaypoint > Waypoints.Length - 1)
                    {
                        CurrentWaypoint = 0;
                    }
                    if (IsThereRotationComponent())
                    {
                        _rotateComponent.ActivateRotation(Waypoints[CurrentWaypoint].Position);
                    }
                    _timeDone = 0;
                    switch (Waypoints[CurrentWaypoint].Action)
                    {
                        case Waypoint.ActionsPatrol.GO:
                            Vector3 origin = GetPreviousPositionWaypoint(CurrentWaypoint);
                            origin.y = transform.position.y;
                            Vector3 target = Waypoints[CurrentWaypoint].Position;
                            target.y = transform.position.y;
                            if (RaycastingTools.IsThereObstacleBetweenPosition(origin, target, MasksToIgnore))
                            {
                                if (_navigationComponent != null)
                                {
                                    _activatedNavigation = true;
                                    _navigationComponent.enabled = true;
                                    _navigationComponent.SetDestination(target);
                                }
                            }
                            ChangeState((int)WaypointActions.GoToWaypoint);
                            break;

                        case Waypoint.ActionsPatrol.STAY:
                            ChangeState((int)WaypointActions.StayInWaypoint);
                            break;

                        case Waypoint.ActionsPatrol.LOOK:
                            ChangeState((int)WaypointActions.LookToWaypoint);
                            break;
                    }

                    if (AutoStart)
                    {
                        if (CurrentWaypoint == 0)
                        {
                            SystemEventController.Instance.DispatchSystemEvent(EventPatrolWaypointsWaypointToCero, this.gameObject);
                        }
                    }
                    break;

                case WaypointActions.GoToWaypoint:
                    if (!_activatedNavigation)
                    {
                        WalkToCurrentWaypoint();
						if (IsThereRotationComponent())
						{
							_rotateComponent.UpdateLogic();
						}
                        if (_timeDone > Waypoints[CurrentWaypoint].Duration)
                        {
                            ChangeState((int)WaypointActions.UpdateWaypoint);
                        }
                    }
                    else
                    {
                        if (IsThereRotationComponent())
                        {
                            _rotateComponent.ActivateRotation(this.transform.position + _navigationComponent.velocity.normalized);
                        }
                        if (Vector3.Distance(this.transform.position, Waypoints[CurrentWaypoint].Position) < 1)
                        {
                            ChangeState((int)WaypointActions.UpdateWaypoint);
                        }
                    }
                    break;

                case WaypointActions.StayInWaypoint:
                    _timeDone += Time.deltaTime;
                    this.transform.position = _currenAnchorPoint;
                    if (IsThereRotationComponent())
                    {
                        _rotateComponent.DeactivateRotation();
                    }
                    if (_timeDone > Waypoints[CurrentWaypoint].Duration)
                    {
                        ChangeState((int)WaypointActions.UpdateWaypoint);
                    }
                    break;

                case WaypointActions.LookToWaypoint:
                    _timeDone += Time.deltaTime;
                    this.transform.position = _currenAnchorPoint;
                    if (IsThereRotationComponent())
                    {
                        _rotateComponent.ActivateRotation(Waypoints[CurrentWaypoint].Position);
                    }
                    if (_timeDone > Waypoints[CurrentWaypoint].Duration)
                    {
                        ChangeState((int)WaypointActions.UpdateWaypoint);
                    }
                    break;
            }
        }
    }
}
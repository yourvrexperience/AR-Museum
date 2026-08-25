using yourvrexperience.Utils;
using UnityEngine;
using System;
using UnityEngine.AI;
using System.Collections.Generic;

namespace yourvrexperience.template6dof
{
	[RequireComponent(typeof(Collider))]
	[RequireComponent(typeof(NavMeshAgent))]		
	public class NavigationAgentView : MonoBehaviour
	{
		public const string EventNavigationAgentViewRelease = "EventNavigationAgentViewRelease";

		public delegate void NavigationEndedEvent(GameObject source);
		public event NavigationEndedEvent EventEnd;
		public void DispatchNavigationEndedEvent(GameObject source)
		{
			if (EventEnd != null) EventEnd(source);
		}

		private NavMeshAgent _navigation;

		public NavMeshAgent Navigation
		{
			get 
			{
				if (_navigation == null)
				{
					_navigation = this.GetComponent<NavMeshAgent>();
				}				
				return _navigation;
			}
		}

		private bool _navigationRunning = false;
		private Vector3 _target;

		public bool NavigationRunning
		{
			get { return _navigationRunning; }
		}
		public float NavigationSpeed
		{
			get { return _navigation.speed; }
		}

		void Start()
		{
			SystemEventController.Instance.Event += OnSystemEvent;			
		}

		public void Destroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		void OnDestroy()
		{
			Destroy();
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(MainController.EventMainControllerReleaseGameResources))
			{
				GameObject.Destroy(this.gameObject);
			}
			if (nameEvent.Equals(EventNavigationAgentViewRelease))
			{				
				GameObject.Destroy(this.gameObject);
			}
		}

		public bool IsActiveNavigation()
		{
			return Navigation.isOnNavMesh;
		}

		public void SetGlobalPosition(Vector3 origin)
		{
			this.transform.position = origin;
		}
		
		public void SetLocalPosition(Vector3 origin)
		{
			this.transform.localPosition = origin;
		}

		public void NavigateTo(Vector3 origin, Vector3 target)
		{
			_navigationRunning = true;
			_target = target;
			this.transform.position = origin;
			Navigation.SetDestination(_target);
		}

        public void StopNavigation()
        {
            _navigationRunning = false;
            Navigation.isStopped = true;
        }
		
		public void SetDestination(Vector3 target)
		{
			_navigationRunning = true;
			_target = target;
			Navigation.SetDestination(_target);
		}

		public List<Vector3> GetPathNavigation()
		{
			List<Vector3> path = new List<Vector3>();
			NavMeshPath pathToTarget = Navigation.path;
			foreach (Vector3 waypoint in pathToTarget.corners)
			{
				path.Add(waypoint);
			}
			return path;
		}

		public List<Vector3> GetPathToTarget(Vector3 origin, Vector3 target)
		{
			List<Vector3> path = new List<Vector3>();			 
			this.transform.position = origin;
			NavMeshPath pathToTarget = new NavMeshPath();			
			if (Navigation.CalculatePath(target, pathToTarget))
			{
				foreach (Vector3 waypoint in pathToTarget.corners)
				{
					path.Add(waypoint);
				}
			}
			return path;
		}

		void Update()
		{
			if (_navigationRunning)
			{
				Vector2 origin = new Vector2(this.transform.position.x, this.transform.position.z);
				Vector2 target = new Vector2(_target.x, _target.z);
				float distanceToTarget = yourvrexperience.Utils.Utilities.DistanceXZ(origin, target);
				if (distanceToTarget < 0.25f)
				{
					_navigationRunning = false;					
					DispatchNavigationEndedEvent(this.gameObject);
				}
			}
		}
    }
}

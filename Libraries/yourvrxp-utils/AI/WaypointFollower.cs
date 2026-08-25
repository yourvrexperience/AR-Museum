using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace yourvrexperience.Utils
{
    public class WaypointFollower : MonoBehaviour
    {
        private List<Transform> _waypoints;
        private float _speed = 1.0f;
        private float _accuracy = 0.01f;
        private float _segmentTime = 1;
        private float _duration = 1;
        private bool _loop = false;
        private bool _isActive = false;
        public float Duration 
        {
            get { return _duration; }
        }

        public void Init(List<Transform> waypoints, float speed = 1.0f, float accuracy = 0.01f, float segmentTime = 1, bool loop = false)
        {
            Stop();

            _waypoints = waypoints;
            _speed = speed;
            _accuracy = accuracy;
            _segmentTime = segmentTime;
            _duration = segmentTime / speed;
            _loop = loop;
            if (_waypoints.Count < 4)
            {
                Debug.LogError("Need at least 4 waypoints for cubic Bezier spline.");
                return;
            }
            _isActive = true;
            StartCoroutine(FollowWaypoints());
        }
        
        public void Stop()
        {
            if (_isActive)
            {
                _isActive = false;
                StopCoroutine(FollowWaypoints());
            }
        }
        
        IEnumerator FollowWaypoints()
        {
            Vector3 p0, p1, p2, p3;

            int limitWaypoints = _waypoints.Count - 3;
            if (_loop)
            {
                limitWaypoints = int.MaxValue;	
            }

            for (int i = 0; i < limitWaypoints; i += 3)
            {
                p0 = _waypoints[i % _waypoints.Count].position;
                p1 = _waypoints[(i + 1) % _waypoints.Count].position;
                p2 = _waypoints[(i + 2) % _waypoints.Count].position;
                p3 = _waypoints[(i + 3) % _waypoints.Count].position;

                for (float t = 0; t <= _segmentTime; t += Time.deltaTime * _speed)
                {
                    Vector3 newPosition = SplineBezier.CalculateBezierPoint(t, p0, p1, p2, p3);
                    transform.position = newPosition;
                    yield return null;
                }
            }
        }
    }
}
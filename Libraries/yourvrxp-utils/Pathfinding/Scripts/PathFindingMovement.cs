using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace yourvrexperience.Utils
{
    public class PathFindingMovement : MonoBehaviour
    {
        public Action<PathFindingMovement> CompletedPath;

        private void ReportCompletedPath()
        {
            CompletedPath?.Invoke(this);
        }

        private float _speedMovement;
        private float _speedRotation;
        private float _yaw;
        private bool _applyGravity;
        private float _directionLeft;
        private bool _ignoreRigidBody;
        private Rigidbody _actorRigidBody;
        private CharacterController _controller;
        private Collider _collider;

        private int _currentWaypoint = -1;
        private float _distanceDetection = -1;
        private List<Vector3> _path;

        public float Yaw
        {
            get { return _yaw; }
            set
            {
                SetYaw(value);
            }
        }
        public void SetYaw(float yaw, bool _force = false)
        {
            _yaw = yaw;
            if (!_ignoreRigidBody && (this.gameObject.GetComponent<Rigidbody>() != null) && (!this.gameObject.GetComponent<Rigidbody>().isKinematic))
            {
                Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, -_yaw, 0));
                this.gameObject.GetComponent<Rigidbody>().MoveRotation(deltaRotation);
            }
            else
            {
                this.gameObject.transform.eulerAngles = new Vector3(0f, -_yaw, 0f);
            }
        }
        public bool ApplyGravity
        {
            get { return _applyGravity; }
        }
        public float DirectionLeft
        {
            get { return _directionLeft; }
            set { _directionLeft = value; }
        }

        public void Initialization(float yaw, bool applyGravity, bool ignoreRigidBody)
        {
            Yaw = yaw;
            _applyGravity = applyGravity;
            _directionLeft = 0;
            _ignoreRigidBody = ignoreRigidBody;
            _actorRigidBody = this.GetComponent<Rigidbody>();
            _controller = this.GetComponent<CharacterController>();
            _collider = this.GetComponent<Collider>();
            if (_actorRigidBody != null)
            {
                _actorRigidBody.isKinematic = _ignoreRigidBody;
                _actorRigidBody.useGravity = applyGravity;
                if (_collider != null)
                {
                    _collider.isTrigger = !_ignoreRigidBody;
                }
            }
        }

        public bool GoTo(Vector3 origin, Vector3 target, float speedMovement, float speedRotation, float distanceDetection, bool forceAnyway = true)
        {
            _speedMovement = speedMovement;
            _speedRotation = speedRotation;
            _distanceDetection = distanceDetection;

            _path = new List<Vector3>();

            Vector3 targetFound = PathFindingController.Instance.GetPath(origin, target, _path, 0, false, -1);
            bool foundPath = true;
            if (targetFound != Vector3.zero)
            {
                _currentWaypoint = 0;
            }
            else
            {
                if (forceAnyway)
                {
                    _currentWaypoint = 0;
                    _path.Add(target);
                }
                else
                {
                    foundPath = false;
                }
            }
            return foundPath;
        }

        public Vector2 MoveTowardsGoal(Vector3 goal, float speedMovement, float speedRotation)
        {
            float yaw = Yaw * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(this.gameObject.transform.position.x, this.gameObject.transform.position.y, this.gameObject.transform.position.z);

            Vector3 normalVector = new Vector3(goal.x, goal.y, goal.z) - pos;
            normalVector.Normalize();

            Vector2 v1 = new Vector2((float)Mathf.Cos(yaw), (float)Mathf.Sin(yaw));
            Vector2 v2 = new Vector2(goal.x - pos.x, goal.z - pos.z);

            float moduloV2 = v2.magnitude;
            if (moduloV2 == 0)
            {
                v2.x = 0.0f;
                v2.y = 0.0f;
            }
            else
            {
                v2.x = v2.x / moduloV2;
                v2.y = v2.y / moduloV2;
            }
            float angulo = (v1.x * v2.x) + (v1.y * v2.y);

            float increment = speedRotation;
            if (angulo > 0.95) increment = (1 - angulo);

            // ASK DIRECTION OF THE ROTATION TO REACH THE GOAL
            float directionLeft = AskDirectionPoint(new Vector2(pos.x, pos.z), yaw, new Vector2(goal.x, goal.z));
            float yawGoal = yaw;
            if (directionLeft > 0)
            {
                yawGoal += increment;
            }
            else
            {
                yawGoal -= increment;
            }
            Vector2 vf = new Vector2((float)Mathf.Cos(yawGoal), (float)Mathf.Sin(yawGoal));
            vf.Normalize();

            // MOVE AND ROTATE
            yawGoal = yawGoal * Mathf.Rad2Deg;
            if ((speedMovement != -1) && (speedMovement != 0))
            {
                Vector3 movement = new Vector3((vf.x * speedMovement * Time.deltaTime),
                                                0,
                                                (vf.y * speedMovement * Time.deltaTime)) + ((normalVector.z != 0) ? Vector3.zero : (ApplyGravity ? (Physics.gravity * Time.deltaTime) : Vector3.zero));                
                if (_controller == null)
                {
                    if (_actorRigidBody == null)
                    {
                        this.gameObject.transform.position += movement;
                    }
                    else
                    {
                        if (_actorRigidBody.isKinematic && _ignoreRigidBody)
                        {
                            this.gameObject.transform.position += movement;
                        }
                        else
                        {
                            this.GetComponent<Rigidbody>().MovePosition(new Vector3(_actorRigidBody.position.x + (vf.x * speedMovement * Time.deltaTime)
                                                                                     , _actorRigidBody.position.y
                                                                                     , _actorRigidBody.position.z + (vf.y * speedMovement * Time.deltaTime)));
                        }
                    }
                }
                else
                {
                    _controller.Move(movement);
                }
            }
            DirectionLeft = directionLeft;
            Yaw = yawGoal;
            return vf;
        }

        public Vector2 MoveLocalTowardsGoal(Vector3 goal, float speedMovement, float speedRotation)
        {
            float yaw = Yaw * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(this.gameObject.transform.localPosition.x, this.gameObject.transform.localPosition.y, this.gameObject.transform.localPosition.z);

            Vector3 normalVector = new Vector3(goal.x, goal.y, goal.z) - pos;
            normalVector.Normalize();

            Vector2 v1 = new Vector2((float)Mathf.Cos(yaw), (float)Mathf.Sin(yaw));
            Vector2 v2 = new Vector2(goal.x - pos.x, goal.z - pos.z);

            float moduloV2 = v2.magnitude;
            if (moduloV2 == 0)
            {
                v2.x = 0.0f;
                v2.y = 0.0f;
            }
            else
            {
                v2.x = v2.x / moduloV2;
                v2.y = v2.y / moduloV2;
            }
            float angulo = (v1.x * v2.x) + (v1.y * v2.y);

            float increment = speedRotation;
            if (angulo > 0.95) increment = (1 - angulo);

            // ASK DIRECTION OF THE ROTATION TO REACH THE GOAL
            float directionLeft = AskDirectionPoint(new Vector2(pos.x, pos.z), yaw, new Vector2(goal.x, goal.z));
            float yawGoal = yaw;
            if (directionLeft > 0)
            {
                yawGoal += increment;
            }
            else
            {
                yawGoal -= increment;
            }
            Vector2 vf = new Vector2((float)Mathf.Cos(yawGoal), (float)Mathf.Sin(yawGoal));
            vf.Normalize();

            // MOVE AND ROTATE
            yawGoal = yawGoal * Mathf.Rad2Deg;
            if ((speedMovement != -1) && (speedMovement != 0))
            {
                Vector3 movement = new Vector3((vf.x * speedMovement * Time.deltaTime),
                                                0,
                                                (vf.y * speedMovement * Time.deltaTime)) + ((normalVector.z != 0) ? Vector3.zero : (ApplyGravity ? (Physics.gravity * Time.deltaTime) : Vector3.zero));

                this.gameObject.transform.localPosition += movement;
            }
            DirectionLeft = directionLeft;
            Yaw = yawGoal;
            return vf;
        }

        public float AskDirectionPoint(Vector2 pos, float yaw, Vector2 objetive)
        {
            // Create Plane
            Vector3 p1 = new Vector3(pos.x, 0.0f, pos.y);
            Vector3 p2 = new Vector3((float)(pos.x + Mathf.Cos(yaw)), 0.0f, (float)(pos.y + Mathf.Sin(yaw)));
            Vector3 p3 = new Vector3(pos.x, 1.0f, pos.y);

            Debug.DrawLine(new Vector3(pos.x, 1, pos.y), new Vector3(p2.x, 1, p2.z), Color.red);
            Debug.DrawLine(new Vector3(pos.x, 1, pos.y), new Vector3(p3.x, 2, p3.z), Color.blue);

            Vector3 p = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 q = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 r = new Vector3(0.0f, 0.0f, 0.0f);

            p = p2 - p1;
            q = p3 - p1;

            r.x = (p.y * q.z) - (p.z * q.y);
            r.y = (p.z * q.x) - (p.x * q.z);
            r.z = (p.x * q.y) - (p.y * q.x);

            float moduloR = r.magnitude;
            if (moduloR == 0)
            {
                r.x = 0.0f;
                r.y = 0.0f;
                r.z = 0.0f;
            }
            else
            {
                r.x = r.x / moduloR;
                r.y = r.y / moduloR;
                r.z = r.z / moduloR;
            }
            float d = -((r.x * p1.x) + (r.y * p1.y) + (r.z * p1.z));

            // Check if point objective is in one side or another of the planeppos is in the center of the plane
            return (((objetive.x * r.x) + (objetive.y * r.z)) + d);
        }

        public bool IsMoving()
        {
            return ((_path != null) && (_currentWaypoint != -1));
        }

        public void Stop()
        {
            _currentWaypoint = -1;
            _path = null;
        }

        public void Force()
        {
            if (_path != null)
            {
                _currentWaypoint = _path.Count - 1;
            }            
        }

        public bool Move()
        {
            if ((_path != null) && (_currentWaypoint!=-1))
            {
                if (_currentWaypoint <= _path.Count)
                {
                    MoveTowardsGoal(_path[_currentWaypoint], _speedMovement, _speedRotation);
                    float distanceToWaypoint = Utilities.DistanceXZ(_path[_currentWaypoint], this.gameObject.transform.position);
                    if (distanceToWaypoint < _distanceDetection)
                    {
                        _currentWaypoint++;
                        if (_currentWaypoint >= _path.Count)
                        {
                            ReportCompletedPath();
                            _currentWaypoint = -1;
                            _path = null;
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
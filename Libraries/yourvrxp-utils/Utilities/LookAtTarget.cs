
using UnityEngine;

namespace yourvrexperience.Utils
{
    public class LookAtTarget : MonoBehaviour
    {
        private Transform _target;

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        public Transform GetTarget()
        {
            return _target;
        }

        void Update()
        {
            if (_target != null)
            {
                transform.rotation = Quaternion.LookRotation(_target.forward);
            }
        }
    }
}
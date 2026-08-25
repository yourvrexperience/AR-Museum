
using UnityEngine;

namespace yourvrexperience.Utils
{
    public class MoveVelocity : MonoBehaviour
    {
        private Vector3 _velocity = Vector3.zero;

        public void Move(Vector3 velocity, bool hasPhysics, bool reorient = false)
        {
            if (reorient) this.transform.forward = velocity;
            Rigidbody rigidbodyObject = this.GetComponent<Rigidbody>();
            if (hasPhysics && (rigidbodyObject != null))
            {
                rigidbodyObject.isKinematic = false;
                rigidbodyObject.useGravity = true;
                rigidbodyObject.AddForce(velocity, ForceMode.Impulse);
            }
            else
            {
                if (rigidbodyObject != null)
                {
                    rigidbodyObject.isKinematic = true;
                    rigidbodyObject.useGravity = false;
                } 
                _velocity = velocity;
            }
        }

        void Update()
        {
            if (_velocity != Vector3.zero)
            {
                transform.transform.position += Time.deltaTime * _velocity;
            }            
        }
    }
}
using UnityEngine;

namespace yourvrexperience.Utils
{
    public static class ColliderInfoExtensions
    {
        public static Vector3 GetColliderSize(GameObject gameObject)
        {
            Collider collider = gameObject.GetComponent<Collider>();
            
            if (collider == null)
            {
                Debug.LogError("GameObject does not have a Collider component.");
                return Vector3.zero;
            }
            
            return collider.bounds.size;
        }	

        public static Bounds GetTotalBounds(GameObject gameObject)
        {
            var colliders = gameObject.GetComponentsInChildren<Collider>();
            if (colliders.Length == 0)
            {
                Debug.LogError("No colliders found in the game object or its children.");
                return new Bounds();
            }
            
            var bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }
            return bounds;
        }
    }
}
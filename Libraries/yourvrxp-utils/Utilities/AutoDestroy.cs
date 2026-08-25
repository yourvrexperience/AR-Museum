
using UnityEngine;

namespace yourvrexperience.Utils
{
    public class AutoDestroy : MonoBehaviour
    {
        void Awake()
        {
            GameObject.Destroy(this.gameObject);
        }
    }
}

using Unity.VisualScripting;
using UnityEngine;

namespace yourvrexperience.Utils
{
    public class IgnoreZTestComponent : MonoBehaviour
    {
        void Start()
        {
            yourvrexperience.Utils.Utilities.ApplyZTestTop(this.transform);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace yourvrexperience.Utils
{
    public class FaceCamera : MonoBehaviour
    {
        void Update()
        {
            FaceTowardsCamera();
        }

        void FaceTowardsCamera()
        {
            Vector3 toCamera = Camera.main.transform.position - transform.position;
            Quaternion rotation = Quaternion.LookRotation(toCamera);
            transform.rotation = rotation;
        }
    }
}
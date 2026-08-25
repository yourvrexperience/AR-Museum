using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace yourvrexperience.Utils
{
    public class FaceARCamera : MonoBehaviour
    {
		public bool FaceNormalAxis = true;
		
        void Update()
        {
            FaceTowardsCamera();
        }

        void FaceTowardsCamera()
        {
            Vector3 toCamera = Camera.main.transform.position - transform.position;
			if (FaceNormalAxis)
			{
				this.transform.localRotation = Quaternion.Euler(new Vector3(0,yourvrexperience.Utils.Utilities.GetAngleFromNormal(new Vector2(toCamera.x, toCamera.z)),0));
			}
			else
			{
				this.transform.localRotation = Quaternion.Euler(new Vector3(0,yourvrexperience.Utils.Utilities.GetAngleFromNormal(new Vector2(toCamera.x, toCamera.y)),0));
			}
        }
    }
}
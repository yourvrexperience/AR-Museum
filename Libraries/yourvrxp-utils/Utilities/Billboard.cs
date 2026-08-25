using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.Utils
{
	public class Billboard : MonoBehaviour
	{
        [SerializeField] private bool Direction;

        private Camera mainCamera;

        void Start()
        {
            mainCamera = Camera.main;
        }

        public void SetDirection(bool direction)
        {
            Direction = direction;
        }

        void LateUpdate()
        {
            if (mainCamera != null)
            {
                transform.LookAt(mainCamera.transform);

                if (Direction)
                {
                    transform.forward = -transform.forward;
                }                
            }
        }
    }
}
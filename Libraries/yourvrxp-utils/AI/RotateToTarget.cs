using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.Utils
{
    public class RotateToTarget : MonoBehaviour
    {
        public Vector3 Target;
        public float RotationSpeed = 1;
        private bool _activated = false;

        public string Pack()
        {
			return RotationSpeed.ToString();
        }

		public void UnPack(string data)
		{
			RotationSpeed = float.Parse(data);
		}

        public void ActivateRotation(Vector3 target)
        {
            Target = target;
            _activated = true;
        }

        public void DeactivateRotation()
        {
            _activated = false;
        }

        public void UpdateLogic()
        {
            if (_activated == false) return;

            Vector3 lookPos = Target - transform.position;
            lookPos.y = 0;
            Quaternion rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, Time.deltaTime * RotationSpeed);
        }
    }
}
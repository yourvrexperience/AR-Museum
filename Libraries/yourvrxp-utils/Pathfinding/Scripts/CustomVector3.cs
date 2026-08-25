using System;
using UnityEngine;
using System.Collections;

namespace yourvrexperience.Utils
{
    [System.Serializable]
    public class CustomVector3
    {
		public float x;
        public float y;
        public float z;

        public CustomVector3()
        {
            x = 0;
            y = 0;
            z = 0;
        }

        public CustomVector3(float _x, float _y, float _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public CustomVector3(CustomVector3 _data)
		{
            x = _data.x;
            y = _data.y;
            z = _data.z;
        }

        public override string ToString()
        {
            return "("+x+","+y+"," +z + ")";
        }

        public Vector3 GetVector3()
        {
            return new Vector3(x,y,z);
        }

        public void SetVector3(Vector3 _data)
        {
            x = _data.x;
            y = _data.y;
            z = _data.z;
        }

    }
}
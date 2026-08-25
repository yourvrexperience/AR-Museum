using System;
using UnityEngine;
using System.Collections;

namespace yourvrexperience.Utils
{
    [System.Serializable]
    public class PrecalculatedData
    {
		public CustomVector3[][] Data;

		public PrecalculatedData(CustomVector3[][] _data)
		{
            Data = _data;
        }

        public void DebugLog()
        {
            for (int i = 0; i < Data.Length; i++)
            {
                if (Data[i] != null)
                {
                    for (int j = 0; j < Data[i].Length; j++)
                    {
                       if (Data[i][j] != null) Debug.LogError("Data[" + i + "][" + j + "]=" + Data[i][j].ToString());
                    }
                }
            }
        }
	}
}
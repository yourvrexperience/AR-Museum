using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace yourvrexperience.Utils
{
    interface IInterpolateData
    {
        GameObject GameActor { get; }
        object Goal { get; set; }
        float TotalTime { get; set; }
        int TypeData { get; }

        void Destroy();
        bool Inperpolate();
        void ResetData(Transform origin, object goal, float totalTime, float timeDone);

    }
}

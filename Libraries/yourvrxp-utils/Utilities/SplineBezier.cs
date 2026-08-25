using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.Utils
{
	public static class SplineBezier
	{
		public static Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
		{
			float u = 1 - t;
			float tt = t * t;
			float uu = u * u;
			float uuu = uu * u;
			float ttt = tt * t;

			Vector3 p = uuu * p0; // (1 - t)^3 * p0
			p += 3 * uu * t * p1; // 3(1 - t)^2 * t * p1
			p += 3 * u * tt * p2; // 3(1 - t) * t^2 * p2
			p += ttt * p3; // t^3 * p3

			return p;
		}
	}
}

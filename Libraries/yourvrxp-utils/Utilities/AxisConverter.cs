using UnityEngine;

namespace yourvrexperience.Utils
{
    public static class AxisConverter
    {
        public static Vector3 ConvertVector(Vector3 v)
        {
            return new Vector3(v.x, v.z, v.y);
        }

        public static Quaternion ConvertRotation(Quaternion q)
        {
            return new Quaternion(-q.x, -q.z, -q.y, q.w);
        }

        public static Vector3 ConvertScale(Vector3 s)
        {
            return new Vector3(s.x, s.z, s.y);
        }

        public static void ConvertPose(ref Vector3 position, ref Quaternion rotation)
        {
            position = ConvertVector(position);
            rotation = ConvertRotation(rotation);
        }

        public static Matrix4x4 ConvertMatrix(Matrix4x4 m)
        {
            return Swap * m * Swap;
        }

        public static readonly Matrix4x4 Swap = BuildSwap();

        private static Matrix4x4 BuildSwap()
        {
            Matrix4x4 c = Matrix4x4.zero;
            c.m00 = 1f; // X -> X
            c.m12 = 1f; // input Z -> output Y
            c.m21 = 1f; // input Y -> output Z
            c.m33 = 1f;
            return c;
        }

        public static Vector3 ConvertVectorIfNeeded(Vector3 v, bool convert)
        {
            return convert ? ConvertVector(v) : v;
        }

        public static Quaternion ConvertRotationIfNeeded(Quaternion q, bool convert)
        {
            return convert ? ConvertRotation(q) : q;
        }

        public static Vector3 ConvertScaleIfNeeded(Vector3 s, bool convert)
        {
            return convert ? ConvertScale(s) : s;
        }

        public static void CapturePose(Transform obj, Transform areaAnchor, bool isNormalAxis, out Vector3 storedPos, out Quaternion storedRot)
        {
            // 1) same frame for both (area-anchor local), independent of parenting
            Vector3 localPos = areaAnchor.InverseTransformPoint(obj.position);
            Quaternion localRot = Quaternion.Inverse(areaAnchor.rotation) * obj.rotation;

            // 2) same convention for both (swap only in AR/MaxST mode)
            bool swap = !isNormalAxis;
            storedPos = ConvertVectorIfNeeded(localPos, swap);
            storedRot = ConvertRotationIfNeeded(localRot, swap);
        }

        public static void ApplyPose(Transform obj, Transform areaAnchor, bool isNormalAxis, Vector3 storedPos, Quaternion storedRot)
        {
            bool swap = !isNormalAxis;
            Vector3 localPos = ConvertVectorIfNeeded(storedPos, swap);
            Quaternion localRot = ConvertRotationIfNeeded(storedRot, swap);

            obj.position = areaAnchor.TransformPoint(localPos);
            obj.rotation = areaAnchor.rotation * localRot;
        }

        public static Quaternion UprightFacing(Camera cam)
        {
            Vector3 flat = cam.transform.forward;
            flat.y = 0f;
            if (flat.sqrMagnitude < 1e-4f) flat = -cam.transform.up; // camera pointing straight up/down
            return Quaternion.LookRotation(flat.normalized, Vector3.up);
        }
    }
}
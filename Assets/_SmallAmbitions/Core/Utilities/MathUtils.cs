using UnityEngine;

namespace SmallAmbitions
{
    public static class MathUtils
    {
        public static bool IsNearlyZero(float value)
        {
            return Mathf.Approximately(value, 0f);
        }

        public static bool IsNearlyZero(Vector2 vector)
        {
            return IsNearlyZero(vector.x) && IsNearlyZero(vector.y);
        }

        public static bool IsNearlyZero(Vector3 vector)
        {
            return IsNearlyZero(vector.x) && IsNearlyZero(vector.y) && IsNearlyZero(vector.z);
        }

        public static float SafeDivide(float numerator, float denominator, float fallback = 0f)
        {
            return denominator != 0f ? numerator / denominator : fallback;
        }

        public static float SqrDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            float dz = a.z - b.z;

            return dx * dx + dy * dy + dz * dz;
        }

        public static float SqrDistance(Component a, Component b, float fallback = float.MaxValue)
        {
            return a && b
                ? SqrDistance(a.transform.position, b.transform.position)
                : fallback;
        }

        public static float SqrDistance(GameObject a, GameObject b, float fallback = float.MaxValue)
        {
            return a && b
                ? SqrDistance(a.transform.position, b.transform.position)
                : fallback;
        }
    }
}

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
            return (a - b).sqrMagnitude;
        }
    }
}

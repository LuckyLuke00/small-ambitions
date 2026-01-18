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
            return IsNearlyZero(vector.sqrMagnitude);
        }

        public static bool IsNearlyZero(Vector3 vector)
        {
            return IsNearlyZero(vector.sqrMagnitude);
        }

        public static float SafeDivide(float numerator, float denominator, float fallback = 0f)
        {
            return IsNearlyZero(denominator)
                ? fallback
                : numerator / denominator;
        }

        public static float SqrDistance(Vector3 a, Vector3 b)
        {
            return (a - b).sqrMagnitude;
        }

        public static float SqrDistance(Transform a, Transform b, float fallback = float.MaxValue)
        {
            return a != null && b != null
                ? SqrDistance(a.position, b.position)
                : fallback;
        }

        public static float SqrDistance(Component a, Component b, float fallback = float.MaxValue)
        {
            return SqrDistance(a ? a.transform : null, b ? b.transform : null, fallback);
        }

        public static float SqrDistance(GameObject a, GameObject b, float fallback = float.MaxValue)
        {
            return SqrDistance(a ? a.transform : null, b ? b.transform : null, fallback);
        }
    }
}

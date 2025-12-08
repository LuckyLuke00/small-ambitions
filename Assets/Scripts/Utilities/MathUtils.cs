using UnityEngine;

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

    public static float SafeDivide(float numerator, float denominator, float fallback = 0f)
    {
        return denominator != 0f ? numerator / denominator : fallback;
    }
}

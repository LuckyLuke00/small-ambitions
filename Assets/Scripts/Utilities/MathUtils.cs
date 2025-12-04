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
}

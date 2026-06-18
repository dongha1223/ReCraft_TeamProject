using UnityEngine;

public static class MathUtil
{
    public static float GetAngleFromPosition(Vector2 owner, Vector2 target)
    {
        return Mathf.Atan2(target.y - owner.y, target.x - owner.x) * Mathf.Rad2Deg;
    }

    public static Vector2 GetNewPoint(Vector3 start, float angle, float r)
    {
        float rad = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad) * r + start.x, Mathf.Sin(rad) * r + start.y);
    }

    public static Vector2 QuadraticCurve(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        return Vector2.Lerp(Vector2.Lerp(a, b, t), Vector2.Lerp(b, c, t), t);
    }

    public static Vector2 CubicCurve(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
    {
        return Vector2.Lerp(QuadraticCurve(a, b, c, t), QuadraticCurve(b, c, d, t), t);
    }
}

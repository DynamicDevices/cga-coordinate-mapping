using System;
using System.Numerics;

public static class Vector3Extensions
{
    public static Vector3 Normalized(this Vector3 vector)
    {
        float length = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z);
        if (length > 0)
        {
            return new Vector3(vector.X / length, vector.Y / length, vector.Z / length);
        }
        return vector;
    }

    public static Vector3 Cross(Vector3 a, Vector3 b)
    {
        return new Vector3(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);
    }

    public static float Dot(Vector3 a, Vector3 b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }

    public static float Distance(Vector3 a, Vector3 b)
    {
        Vector3 diff = a - b;
        return (float)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z);
    }

    public static Vector3 Zero => new Vector3(0, 0, 0);
}

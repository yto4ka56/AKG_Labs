namespace Lab3.Math;

public struct Vector4
{
    public float X, Y, Z, W;

    public Vector4(float x, float y, float z, float w = 1.0f)
    {
        X = x; Y = y; Z = z; W = w;
    }

    public static Vector4 operator -(Vector4 a, Vector4 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z, 0);
    public static Vector4 operator +(Vector4 a, Vector4 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
    public static Vector4 operator *(Vector4 a, float scalar) => new(a.X * scalar, a.Y * scalar, a.Z * scalar, a.W * scalar);
    
    public float Length() => (float)System.Math.Sqrt(X * X + Y * Y + Z * Z);

    public static Vector4 Normalize(Vector4 v)
    {
        float l = v.Length();
        return l > 0 ? new Vector4(v.X / l, v.Y / l, v.Z / l, v.W) : v;
    }

    public static Vector4 Cross(Vector4 a, Vector4 b) =>
        new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X, 0);

    public static float Dot(Vector4 a, Vector4 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    public static Vector4 Lerp(Vector4 a, Vector4 b, float t) =>
        new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t, a.W + (b.W - a.W) * t);
}
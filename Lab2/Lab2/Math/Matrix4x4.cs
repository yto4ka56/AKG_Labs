namespace Lab2.Math;

public class Matrix4x4
{
    public float[,] M = new float[4, 4];

    public static Matrix4x4 Identity()
    {
        var res = new Matrix4x4();
        for (int i = 0; i < 4; i++) res.M[i, i] = 1.0f;
        return res;
    }

    public static Vector4 Multiply(Matrix4x4 m, Vector4 v)
    {
        return new Vector4(
            m.M[0, 0] * v.X + m.M[0, 1] * v.Y + m.M[0, 2] * v.Z + m.M[0, 3] * v.W,
            m.M[1, 0] * v.X + m.M[1, 1] * v.Y + m.M[1, 2] * v.Z + m.M[1, 3] * v.W,
            m.M[2, 0] * v.X + m.M[2, 1] * v.Y + m.M[2, 2] * v.Z + m.M[2, 3] * v.W,
            m.M[3, 0] * v.X + m.M[3, 1] * v.Y + m.M[3, 2] * v.Z + m.M[3, 3] * v.W
        );
    }

    public static Matrix4x4 Multiply(Matrix4x4 a, Matrix4x4 b)
    {
        var res = new Matrix4x4();
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                for (int k = 0; k < 4; k++)
                    res.M[i, j] += a.M[i, k] * b.M[k, j];
        return res;
    }

    public static Matrix4x4 CreateTranslation(float tx, float ty, float tz)
    {
        var res = Identity();
        res.M[0, 3] = tx; res.M[1, 3] = ty; res.M[2, 3] = tz;
        return res;
    }

    public static Matrix4x4 CreateScale(float s)
    {
        var res = Identity();
        res.M[0, 0] = s; res.M[1, 1] = s; res.M[2, 2] = s;
        return res;
    }

    public static Matrix4x4 CreateRotationX(float a)
    {
        var res = Identity();
        res.M[1, 1] = (float)System.Math.Cos(a); res.M[1, 2] = -(float)System.Math.Sin(a);
        res.M[2, 1] = (float)System.Math.Sin(a); res.M[2, 2] = (float)System.Math.Cos(a);
        return res;
    }

    public static Matrix4x4 CreateRotationY(float a)
    {
        var res = Identity();
        res.M[0, 0] = (float)System.Math.Cos(a); res.M[0, 2] = (float)System.Math.Sin(a);
        res.M[2, 0] = -(float)System.Math.Sin(a); res.M[2, 2] = (float)System.Math.Cos(a);
        return res;
    }

    public static Matrix4x4 CreateRotationZ(float a)
    {
        var res = Identity();
        res.M[0, 0] = (float)System.Math.Cos(a); res.M[0, 1] = -(float)System.Math.Sin(a);
        res.M[1, 0] = (float)System.Math.Sin(a); res.M[1, 1] = (float)System.Math.Cos(a);
        return res;
    }

    public static Matrix4x4 CreateLookAt(Vector4 eye, Vector4 target, Vector4 up)
    {
        Vector4 zAxis = Vector4.Normalize(eye - target);
        Vector4 xAxis = Vector4.Normalize(Vector4.Cross(up, zAxis));
        Vector4 yAxis = Vector4.Cross(zAxis, xAxis);
        var res = Identity();
        res.M[0, 0] = xAxis.X; res.M[0, 1] = xAxis.Y; res.M[0, 2] = xAxis.Z; res.M[0, 3] = -Vector4.Dot(xAxis, eye);
        res.M[1, 0] = yAxis.X; res.M[1, 1] = yAxis.Y; res.M[1, 2] = yAxis.Z; res.M[1, 3] = -Vector4.Dot(yAxis, eye);
        res.M[2, 0] = zAxis.X; res.M[2, 1] = zAxis.Y; res.M[2, 2] = zAxis.Z; res.M[2, 3] = -Vector4.Dot(zAxis, eye);
        return res;
    }

    public static Matrix4x4 CreatePerspective(float fov, float aspect, float znear, float zfar)
    {
        var res = new Matrix4x4();
        float h = 1.0f / (float)System.Math.Tan(fov / 2);
        res.M[0, 0] = h / aspect;
        res.M[1, 1] = h;
        res.M[2, 2] = zfar / (znear - zfar);
        res.M[2, 3] = (znear * zfar) / (znear - zfar);
        res.M[3, 2] = -1;
        return res;
    }

    public static Matrix4x4 CreateViewport(float w, float h)
    {
        var res = Identity();
        res.M[0, 0] = w / 2; res.M[0, 3] = w / 2;
        res.M[1, 1] = -h / 2; res.M[1, 3] = h / 2;
        return res;
    }
}
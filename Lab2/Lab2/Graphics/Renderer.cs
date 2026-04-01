using System;
using Avalonia.Media.Imaging;
using Lab2.Math;

namespace Lab2.Graphics;

public static class Renderer
{
    public static unsafe void Clear(WriteableBitmap bmp, float[] zBuffer, uint color = 0xFF000000)
    {
        using var buf = bmp.Lock();
        uint* ptr = (uint*)buf.Address;
        int size = bmp.PixelSize.Width * bmp.PixelSize.Height;
        new Span<uint>(ptr, size).Fill(color);
        Array.Fill(zBuffer, float.NegativeInfinity);
    }

    public static unsafe void DrawTriangle(WriteableBitmap bmp, float[] zBuffer, Vector4 v1, Vector4 v2, Vector4 v3, uint color)
    {
        int width = bmp.PixelSize.Width;
        int height = bmp.PixelSize.Height;

        // Сортировка вершин по Y
        if (v1.Y > v2.Y) (v1, v2) = (v2, v1);
        if (v1.Y > v3.Y) (v1, v3) = (v3, v1);
        if (v2.Y > v3.Y) (v2, v3) = (v3, v2);

        using var buf = bmp.Lock();
        uint* ptr = (uint*)buf.Address;

        int totalHeight = (int)v3.Y - (int)v1.Y;
        for (int i = 0; i < totalHeight; i++)
        {
            bool secondHalf = i > v2.Y - v1.Y || v2.Y == v1.Y;
            int segmentHeight = secondHalf ? (int)(v3.Y - v2.Y) : (int)(v2.Y - v1.Y);
            if (segmentHeight == 0) continue;

            float t1 = (float)i / totalHeight;
            float t2 = (float)(i - (secondHalf ? v2.Y - v1.Y : 0)) / segmentHeight;

            Vector4 A = Vector4.Lerp(v1, v3, t1);
            Vector4 B = secondHalf ? Vector4.Lerp(v2, v3, t2) : Vector4.Lerp(v1, v2, t2);

            if (A.X > B.X) (A, B) = (B, A);

            int minX = (int)MathF.Max(0, A.X);
            int maxX = (int)MathF.Min(width - 1, B.X);
            int y = (int)v1.Y + i;

            if (y < 0 || y >= height) continue;

            for (int x = minX; x <= maxX; x++)
            {
                float phi = B.X != A.X ? (x - A.X) / (B.X - A.X) : 1f;
                float z = A.Z + (B.Z - A.Z) * phi;
                int idx = y * width + x;

                if (idx >= 0 && idx < zBuffer.Length && zBuffer[idx] < z)
                {
                    zBuffer[idx] = z;
                    ptr[idx] = color;
                }
            }
        }
    }
}
using System;
using Lab2.Math;

namespace Lab2.Graphics;

public static class Renderer
{
    public static unsafe void Clear(uint* ptr, int width, int height, float[] zBuffer, uint color = 0xFF000000)
    {
        int size = width * height;
        new Span<uint>(ptr, size).Fill(color);
        Array.Fill(zBuffer, float.MaxValue);
    }

    private static void Swap(ref Vector4 a, ref Vector4 b) { var temp = a; a = b; b = temp; }

    public static unsafe void DrawTriangle(uint* ptr, int width, int height, float[] zBuffer, Vector4 v1, Vector4 v2, Vector4 v3, uint color)
    {
        
        if (v1.Y > v2.Y) Swap(ref v1, ref v2);
        if (v1.Y > v3.Y) Swap(ref v1, ref v3);
        if (v2.Y > v3.Y) Swap(ref v2, ref v3);

        int y0 = (int)MathF.Round(v1.Y);
        int y1 = (int)MathF.Round(v2.Y);
        int y2 = (int)MathF.Round(v3.Y);

        int totalHeight = y2 - y0;
        if (totalHeight == 0) return;

        for (int i = 0; i <= totalHeight; i++)
        {
            int y = y0 + i;
            if (y < 0 || y >= height) continue;

            bool secondHalf = i > y1 - y0 || y1 == y0;
            int segmentHeight = secondHalf ? y2 - y1 : y1 - y0;
            if (segmentHeight == 0) continue;

            float alpha = (float)i / totalHeight;
            float beta = (float)(i - (secondHalf ? y1 - y0 : 0)) / segmentHeight;

            Vector4 A = v1 + (v3 - v1) * alpha;
            Vector4 B = secondHalf ? v2 + (v3 - v2) * beta : v1 + (v2 - v1) * beta;

            if (A.X > B.X) Swap(ref A, ref B);

            int minX = (int)MathF.Max(0, MathF.Ceiling(A.X));
            int maxX = (int)MathF.Min(width - 1, MathF.Floor(B.X));

            for (int x = minX; x <= maxX; x++)
            {
                float phi = A.X == B.X ? 1f : (x - A.X) / (B.X - A.X);
                float z = A.Z + (B.Z - A.Z) * phi;
                
                int idx = y * width + x;

                if (z < zBuffer[idx])
                {
                    zBuffer[idx] = z;
                    ptr[idx] = color; 
                }
            }
        }
    }
}
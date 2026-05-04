using System;
using Lab4.Math;

namespace Lab4.Graphics;

public static class Renderer
{
    public static unsafe void Clear(uint* ptr, int width, int height, float[] zBuffer, uint color = 0xFF000000)
    {
        int size = width * height;
        new Span<uint>(ptr, size).Fill(color);
        Array.Fill(zBuffer, float.MaxValue);
    }

    private static void Swap(ref Vector4 a, ref Vector4 b) { var temp = a; a = b; b = temp; }
    private static void SwapN(ref Vector4 a, ref Vector4 b) { var temp = a; a = b; b = temp; }

 
    private static uint PhongLighting(
        Vector4 normal,
        Vector4 fragPos,
        Vector4 viewPos,
        Vector4 lightDir,
        uint baseColor)
    {
        normal = Vector4.Normalize(normal);

        
        const float ka = 0.15f;   
        const float kd = 0.75f;   
        const float ks = 0.5f;    
        const float shininess = 32f; 
        
        float ambient = ka;
        
        float diff = MathF.Max(0f, Vector4.Dot(normal, lightDir));
        float diffuse = kd * diff;
        
        Vector4 viewDir = Vector4.Normalize(viewPos - fragPos);
        float ln = Vector4.Dot(lightDir, normal);
        Vector4 reflectDir = Vector4.Normalize(
            new Vector4(
                2f * ln * normal.X - lightDir.X,
                2f * ln * normal.Y - lightDir.Y,
                2f * ln * normal.Z - lightDir.Z,
                0f));
        float spec = MathF.Pow(MathF.Max(0f, Vector4.Dot(reflectDir, viewDir)), shininess);
        float specular = ks * spec;

        float intensity = MathF.Min(1f, ambient + diffuse);
        
        uint a = (baseColor >> 24) & 0xFF;
        float r = ((baseColor >> 16) & 0xFF) * intensity;
        float g = ((baseColor >> 8) & 0xFF) * intensity;
        float b = (baseColor & 0xFF) * intensity;
        
        float specLight = specular * 255f;
        r = MathF.Min(255f, r + specLight);
        g = MathF.Min(255f, g + specLight);
        b = MathF.Min(255f, b + specLight);

        return (a << 24) | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
    }

    public static unsafe void DrawTrianglePhong(
        uint* ptr, int width, int height, float[] zBuffer,
        Vector4 v1, Vector4 v2, Vector4 v3,    
        Vector4 w1, Vector4 w2, Vector4 w3,     
        Vector4 n1, Vector4 n2, Vector4 n3,   
        Vector4 viewPos, Vector4 lightDir,
        uint baseColor)
    {
        
        if (v1.Y > v2.Y) { Swap(ref v1, ref v2); SwapN(ref n1, ref n2); Swap(ref w1, ref w2); }
        if (v1.Y > v3.Y) { Swap(ref v1, ref v3); SwapN(ref n1, ref n3); Swap(ref w1, ref w3); }
        if (v2.Y > v3.Y) { Swap(ref v2, ref v3); SwapN(ref n2, ref n3); Swap(ref w2, ref w3); }

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
            
            Vector4 Aw = w1 + (w3 - w1) * alpha;
            Vector4 Bw = secondHalf ? w2 + (w3 - w2) * beta : w1 + (w2 - w1) * beta;
            
            Vector4 An = n1 + (n3 - n1) * alpha;
            Vector4 Bn = secondHalf ? n2 + (n3 - n2) * beta : n1 + (n2 - n1) * beta;

            if (A.X > B.X)
            {
                Swap(ref A, ref B);
                Swap(ref Aw, ref Bw);
                SwapN(ref An, ref Bn);
            }

            int minX = (int)MathF.Max(0, MathF.Ceiling(A.X));
            int maxX = (int)MathF.Min(width - 1, MathF.Floor(B.X));

            for (int x = minX; x <= maxX; x++)
            {
                float phi = A.X == B.X ? 1f : (x - A.X) / (B.X - A.X);
                float z = A.Z + (B.Z - A.Z) * phi;

                int idx = y * width + x;
                if (z >= zBuffer[idx]) continue;

                zBuffer[idx] = z;
                
                Vector4 interpNormal = An + (Bn - An) * phi;
                Vector4 interpWorld  = Aw + (Bw - Aw) * phi;

                ptr[idx] = PhongLighting(interpNormal, interpWorld, viewPos, lightDir, baseColor);
            }
        }
    }
}

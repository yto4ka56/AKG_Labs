using System;
using Lab3.Math;

namespace Lab3.Graphics;

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

    // Модель освещения по Фонгу для одного пикселя
    // normal      — интерполированная нормаль в пикселе (мировое пространство)
    // fragPos     — позиция пикселя в мировом пространстве
    // viewPos     — позиция камеры
    // lightDir    — нормализованное направление к источнику света
    // baseColor   — базовый цвет объекта (ARGB)
    private static uint PhongLighting(
        Vector4 normal,
        Vector4 fragPos,
        Vector4 viewPos,
        Vector4 lightDir,
        uint baseColor)
    {
        normal = Vector4.Normalize(normal);

        // Коэффициенты модели освещения Фонга
        const float ka = 0.15f;   // фоновое
        const float kd = 0.75f;   // рассеянное
        const float ks = 0.5f;    // зеркальное
        const float shininess = 32f; // коэффициент блеска

        // Фоновое освещение: Ia = ka
        float ambient = ka;

        // Рассеянное освещение: Id = kd * max(N · L, 0)
        float diff = MathF.Max(0f, Vector4.Dot(normal, lightDir));
        float diffuse = kd * diff;

        // Зеркальное освещение: Is = ks * max(R · V, 0)^shininess
        // R = L - 2*(L·N)*N  (отражённый вектор)
        Vector4 viewDir = Vector4.Normalize(viewPos - fragPos);
        float ln = Vector4.Dot(lightDir, normal);
        Vector4 reflectDir = Vector4.Normalize(
            new Vector4(
                lightDir.X - 2f * ln * normal.X,
                lightDir.Y - 2f * ln * normal.Y,
                lightDir.Z - 2f * ln * normal.Z,
                0f));
        float spec = MathF.Pow(MathF.Max(0f, Vector4.Dot(reflectDir, viewDir)), shininess);
        float specular = ks * spec;

        float intensity = MathF.Min(1f, ambient + diffuse);

        // Базовый цвет объекта с diffuse + ambient
        uint a = (baseColor >> 24) & 0xFF;
        float r = ((baseColor >> 16) & 0xFF) * intensity;
        float g = ((baseColor >> 8) & 0xFF) * intensity;
        float b = (baseColor & 0xFF) * intensity;

        // Зеркальный блик — белый свет поверх
        float specLight = specular * 255f;
        r = MathF.Min(255f, r + specLight);
        g = MathF.Min(255f, g + specLight);
        b = MathF.Min(255f, b + specLight);

        return (a << 24) | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
    }

    // Рисуем треугольник с затенением по Фонгу
    // n1, n2, n3 — нормали вершин в мировом пространстве
    // w1, w2, w3 — позиции вершин в мировом пространстве (для интерполяции позиции)
    public static unsafe void DrawTrianglePhong(
        uint* ptr, int width, int height, float[] zBuffer,
        Vector4 v1, Vector4 v2, Vector4 v3,     // экранные координаты (с Z)
        Vector4 w1, Vector4 w2, Vector4 w3,     // мировые позиции
        Vector4 n1, Vector4 n2, Vector4 n3,     // вершинные нормали
        Vector4 viewPos, Vector4 lightDir,
        uint baseColor)
    {
        // Сортируем по Y (экранные координаты)
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

            // Интерполируем экранные координаты
            Vector4 A = v1 + (v3 - v1) * alpha;
            Vector4 B = secondHalf ? v2 + (v3 - v2) * beta : v1 + (v2 - v1) * beta;

            // Интерполируем мировые позиции
            Vector4 Aw = w1 + (w3 - w1) * alpha;
            Vector4 Bw = secondHalf ? w2 + (w3 - w2) * beta : w1 + (w2 - w1) * beta;

            // Интерполируем нормали (затенение по Фонгу — интерполируем нормаль, не цвет)
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

                // Интерполируем нормаль и мировую позицию для данного пикселя
                Vector4 interpNormal = An + (Bn - An) * phi;
                Vector4 interpWorld  = Aw + (Bw - Aw) * phi;

                ptr[idx] = PhongLighting(interpNormal, interpWorld, viewPos, lightDir, baseColor);
            }
        }
    }
}

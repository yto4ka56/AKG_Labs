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

    private static void Swap<T>(ref T a, ref T b) { var temp = a; a = b; b = temp; }

    // Освещение по Фонгу (с текстурами)
    private static uint PhongLighting(
        Vector4 normal, Vector4 fragPos, Vector4 viewPos, Vector4 lightDir,
        uint baseColor, float specularMapFactor)
    {
        normal = Vector4.Normalize(normal);

        const float ka = 0.15f;   
        const float kd = 0.75f;   
        const float shininess = 32f; 
        
        float ambient = ka;
        float diff = MathF.Max(0f, Vector4.Dot(normal, lightDir));
        float diffuse = kd * diff;
        
        Vector4 viewDir = Vector4.Normalize(viewPos - fragPos);
        float ln = Vector4.Dot(lightDir, normal);
        
        Vector4 reflectDir = Vector4.Normalize(new Vector4(
            2f * ln * normal.X - lightDir.X,
            2f * ln * normal.Y - lightDir.Y,
            2f * ln * normal.Z - lightDir.Z, 0f));
            
        float spec = MathF.Pow(MathF.Max(0f, Vector4.Dot(reflectDir, viewDir)), shininess);
        
        // Зеркальная карта влияет на силу блика (ks)
        float specular = specularMapFactor * spec;

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

    public static unsafe void DrawTriangleTextured(
        uint* ptr, int width, int height, float[] zBuffer,
        Vector4 p1, Vector4 p2, Vector4 p3,          // Экранные координаты (с W)
        Vector4 w1, Vector4 w2, Vector4 w3,          // Мировые координаты
        Vector2 uv1, Vector2 uv2, Vector2 uv3,       // Текстурные координаты
        Vector4 n1, Vector4 n2, Vector4 n3,          // Вершинные нормали
        Vector4 viewPos, Vector4 lightDir,
        Matrix4x4 rotationMatrix,                    // Матрица поворота для карты нормалей
        Texture diffTex, Texture normTex, Texture specTex)
    {
        // Сортировка вершин по Y
        if (p1.Y > p2.Y) { Swap(ref p1, ref p2); Swap(ref w1, ref w2); Swap(ref uv1, ref uv2); Swap(ref n1, ref n2); }
        if (p1.Y > p3.Y) { Swap(ref p1, ref p3); Swap(ref w1, ref w3); Swap(ref uv1, ref uv3); Swap(ref n1, ref n3); }
        if (p2.Y > p3.Y) { Swap(ref p2, ref p3); Swap(ref w2, ref w3); Swap(ref uv2, ref uv3); Swap(ref n2, ref n3); }

        int y0 = (int)MathF.Round(p1.Y);
        int y1 = (int)MathF.Round(p2.Y);
        int y2 = (int)MathF.Round(p3.Y);

        int totalHeight = y2 - y0;
        if (totalHeight == 0) return;

        // ПЕРСПЕКТИВНАЯ КОРРЕКЦИЯ: Подготовка (делим атрибуты на W)
        float invW1 = 1f / p1.W, invW2 = 1f / p2.W, invW3 = 1f / p3.W;
        
        Vector4 w1c = w1 * invW1, w2c = w2 * invW2, w3c = w3 * invW3;
        Vector2 uv1c = uv1 * invW1, uv2c = uv2 * invW2, uv3c = uv3 * invW3;
        Vector4 n1c = n1 * invW1, n2c = n2 * invW2, n3c = n3 * invW3;

        for (int i = 0; i <= totalHeight; i++)
        {
            int y = y0 + i;
            if (y < 0 || y >= height) continue;

            bool secondHalf = i > y1 - y0 || y1 == y0;
            int segmentHeight = secondHalf ? y2 - y1 : y1 - y0;
            if (segmentHeight == 0) continue;

            float alpha = (float)i / totalHeight;
            float beta = (float)(i - (secondHalf ? y1 - y0 : 0)) / segmentHeight;

            // Интерполяция X и Z
            Vector4 A = p1 + (p3 - p1) * alpha;
            Vector4 B = secondHalf ? p2 + (p3 - p2) * beta : p1 + (p2 - p1) * beta;

            // Интерполяция 1/W
            float invWA = invW1 + (invW3 - invW1) * alpha;
            float invWB = secondHalf ? invW2 + (invW3 - invW2) * beta : invW1 + (invW2 - invW1) * beta;

            // Интерполяция корректных атрибутов по краям треугольника
            Vector4 Aw = w1c + (w3c - w1c) * alpha;
            Vector4 Bw = secondHalf ? w2c + (w3c - w2c) * beta : w1c + (w2c - w1c) * beta;

            Vector2 Auv = uv1c + (uv3c - uv1c) * alpha;
            Vector2 Buv = secondHalf ? uv2c + (uv3c - uv2c) * beta : uv1c + (uv2c - uv1c) * beta;

            Vector4 An = n1c + (n3c - n1c) * alpha;
            Vector4 Bn = secondHalf ? n2c + (n3c - n2c) * beta : n1c + (n2c - n1c) * beta;

            if (A.X > B.X)
            {
                Swap(ref A, ref B); Swap(ref Aw, ref Bw); 
                Swap(ref Auv, ref Buv); Swap(ref An, ref Bn);
                Swap(ref invWA, ref invWB);
            }

            int minX = (int)MathF.Max(0, MathF.Ceiling(A.X));
            int maxX = (int)MathF.Min(width - 1, MathF.Floor(B.X));

            for (int x = minX; x <= maxX; x++)
            {
                float phi = A.X == B.X ? 1f : (x - A.X) / (B.X - A.X);
                float z = A.Z + (B.Z - A.Z) * phi;

                int idx = y * width + x;
                if (z >= zBuffer[idx]) continue;

                // ПЕРСПЕКТИВНАЯ КОРРЕКЦИЯ: Восстановление
                float interpInvW = invWA + (invWB - invWA) * phi;
                float w = 1f / interpInvW; // Восстанавливаем глубину для пикселя

                Vector2 uv = (Auv + (Buv - Auv) * phi) * w;
                Vector4 worldPos = (Aw + (Bw - Aw) * phi) * w;
                Vector4 normal = (An + (Bn - An) * phi) * w;

                zBuffer[idx] = z;

                // 1) Чтение Диффузной карты
                uint baseColor = diffTex != null ? diffTex.Sample(uv.X, uv.Y) : 0xFF808080;

                // 2) Чтение Карты нормалей (Из пространства модели -> мировое)
                if (normTex != null)
                {
                    uint nColor = normTex.Sample(uv.X, uv.Y);
                    float nx = (((nColor >> 16) & 0xFF) / 255f) * 2f - 1f;
                    float ny = (((nColor >> 8) & 0xFF) / 255f) * 2f - 1f;
                    float nz = ((nColor & 0xFF) / 255f) * 2f - 1f;
                    
                    Vector4 texNormal = new Vector4(nx, ny, nz, 0);
                    // Умножаем на матрицу поворота объекта, чтобы нормаль смотрела правильно
                    normal = Matrix4x4.Multiply(rotationMatrix, texNormal); 
                }

                // 3) Чтение Зеркальной карты (Берем R канал как значение specular intensity)
                float ks = 0.5f; // Дефолтное значение
                if (specTex != null)
                {
                    uint sColor = specTex.Sample(uv.X, uv.Y);
                    ks = ((sColor >> 16) & 0xFF) / 255f;
                }

                ptr[idx] = PhongLighting(normal, worldPos, viewPos, lightDir, baseColor, ks);
            }
        }
    }
}
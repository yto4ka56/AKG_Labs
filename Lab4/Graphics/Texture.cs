using System;
using Avalonia;
using Avalonia.Media.Imaging;

namespace Lab4.Graphics;

public class Texture
{
    public uint[] Pixels;
    public int Width;
    public int Height;

    // Добавили unsafe, так как мы используем указатели (pointers) для копирования
    public unsafe Texture(string path)
    {
        try
        {
            using var bitmap = new Bitmap(path);
            Width = (int)bitmap.Size.Width;
            Height = (int)bitmap.Size.Height;
            Pixels = new uint[Width * Height];

            // Фиксируем массив в памяти, чтобы сборщик мусора его не сдвинул во время копирования
            fixed (uint* ptr = Pixels)
            {
                // Задаем область копирования (вся картинка)
                var rect = new PixelRect(0, 0, Width, Height);
                
                // Stride - это количество байт в одной строке картинки (ширина * 4 байта на каждый пиксель ARGB)
                int stride = Width * 4; 
                
                // Копируем пиксели напрямую из Bitmap в наш массив
                bitmap.CopyPixels(rect, (IntPtr)ptr, Pixels.Length * 4, stride);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки текстуры {path}: {ex.Message}");
            Width = 1; Height = 1;
            // Если текстура не загрузилась (например, неверный путь), делаем её просто белой
            Pixels = new uint[] { 0xFFFFFFFF }; 
        }
    }

    public uint Sample(float u, float v)
    {
        // Ограничиваем текстурные координаты (wrap / clamp), чтобы они были от 0.0 до 1.0
        u = u - MathF.Floor(u);
        v = v - MathF.Floor(v);

        int x = (int)(u * (Width - 1));
        // В 3D графике координата V обычно идет снизу вверх, а в картинках (массивах) сверху вниз,
        // поэтому мы инвертируем V (1.0f - v)
        int y = (int)((1.0f - v) * (Height - 1)); 

        x = System.Math.Clamp(x, 0, Width - 1);
        y = System.Math.Clamp(y, 0, Height - 1);

        return Pixels[y * Width + x];
    }
}
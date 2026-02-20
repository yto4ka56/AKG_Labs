using System;
using Avalonia.Media.Imaging;

namespace Lab1.Graphics;

public static class LineRenderer
{
    public static unsafe void Clear(WriteableBitmap bmp, uint color = 0xFF000000)
    {
        using var buf = bmp.Lock();
        uint* ptr = (uint*)buf.Address;
        int size = bmp.PixelSize.Width * bmp.PixelSize.Height;
        
        new Span<uint>(ptr, size).Fill(color);
    }

    public static unsafe void DrawLineBresenham(WriteableBitmap bmp, int x1, int y1, int x2, int y2, uint color)
    {
        using var buf = bmp.Lock();
        int w = bmp.PixelSize.Width, h = bmp.PixelSize.Height;
        uint* ptr = (uint*)buf.Address;

        int dx = System.Math.Abs(x2 - x1), sx = x1 < x2 ? 1 : -1;
        int dy = -System.Math.Abs(y2 - y1), sy = y1 < y2 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            if (x1 >= 0 && x1 < w && y1 >= 0 && y1 < h) ptr[y1 * w + x1] = color;
            if (x1 == x2 && y1 == y2) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x1 += sx; }
            if (e2 <= dx) { err += dx; y1 += sy; }
        }
    }
}
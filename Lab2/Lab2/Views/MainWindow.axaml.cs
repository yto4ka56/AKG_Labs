using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Lab2.Math;
using Lab2.IO;
using Lab2.Graphics;
using System;

namespace Lab2.Views;

public partial class MainWindow : Window
{
    private WriteableBitmap _buffer;
    private float[] _zBuffer;
    private ObjParser _model = new();
    private float _rotX = 0, _rotY = 0;
    private float _scale = 300f;

    public MainWindow()
    {
        InitializeComponent();
        _model.Parse("/Users/maksimbelaev/Downloads/baby.obj"); // Укажите верный путь
        _model.CenterAndNormalizeModel();
        
        _buffer = new WriteableBitmap(new Avalonia.PixelSize(800, 600), new Avalonia.Vector(96, 96), 
            Avalonia.Platform.PixelFormat.Rgba8888, Avalonia.Platform.AlphaFormat.Premul);
        _zBuffer = new float[800 * 600];

        MyImage.Source = _buffer;
        this.KeyDown += (s, e) => {
            if (e.Key == Key.W) _rotX += 0.1f;
            if (e.Key == Key.S) _rotX -= 0.1f;
            if (e.Key == Key.A) _rotY -= 0.1f;
            if (e.Key == Key.D) _rotY += 0.1f;
            Render();
        };
        Render();
    }

    private void Render()
    {
        Renderer.Clear(_buffer, _zBuffer);

        var modelM = Matrix4x4.Multiply(Matrix4x4.CreateRotationX(_rotX), Matrix4x4.CreateRotationY(_rotY));
        var viewM = Matrix4x4.CreateLookAt(new Vector4(0, 0, 2), new Vector4(0, 0, 0), new Vector4(0, 1, 0));
        var projM = Matrix4x4.CreatePerspective(MathF.PI / 4, 800f/600f, 0.1f, 100f);
        var vpM = Matrix4x4.CreateViewport(800, 600);
        
        var transform = Matrix4x4.Multiply(vpM, Matrix4x4.Multiply(projM, viewM));
        Vector4 lightDir = Vector4.Normalize(new Vector4(0, 0, 1, 0)); // Свет "из камеры"

        foreach (var face in _model.Faces)
        {
            // 1. Трансформируем вершины в мировые координаты для расчета освещения
            Vector4 v1w = Matrix4x4.Multiply(modelM, _model.Vertices[face[0]]);
            Vector4 v2w = Matrix4x4.Multiply(modelM, _model.Vertices[face[1]]);
            Vector4 v3w = Matrix4x4.Multiply(modelM, _model.Vertices[face[2]]);

            // 2. Расчет нормали
            Vector4 normal = Vector4.Normalize(Vector4.Cross(v2w - v1w, v3w - v1w));

            // 3. Отбраковка задних граней (Back-face culling)
            // В экранном пространстве нормаль должна смотреть на нас (Z > 0)
            if (normal.Z < 0) continue;

            // 4. Освещение Ламберта
            float intensity = System.Math.Max(0.1f, Vector4.Dot(normal, lightDir));
            uint color = ApplyIntensity(0xFFFFFFFF, intensity);

            // 5. Проекция на экран
            Vector4 p1 = Project(v1w, transform);
            Vector4 p2 = Project(v2w, transform);
            Vector4 p3 = Project(v3w, transform);

            Renderer.DrawTriangle(_buffer, _zBuffer, p1, p2, p3, color);
        }
        MyImage.InvalidateVisual();
    }

    private Vector4 Project(Vector4 v, Matrix4x4 mat)
    {
        Vector4 res = Matrix4x4.Multiply(mat, v);
        if (res.W != 0) { res.X /= res.W; res.Y /= res.W; res.Z /= res.W; }
        return res;
    }

    private uint ApplyIntensity(uint color, float intensity)
    {
        uint r = (uint)((color >> 16 & 0xFF) * intensity);
        uint g = (uint)((color >> 8 & 0xFF) * intensity);
        uint b = (uint)((color & 0xFF) * intensity);
        return 0xFF000000 | (r << 16) | (g << 8) | b;
    }
}
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Lab1.Math;
using Lab1.IO;
using Lab1.Graphics;
using System;
using Avalonia;
using Avalonia.Platform;

namespace Lab1.Views;

public partial class MainWindow : Window
{
    private WriteableBitmap _buffer;
    private ObjParser _model = new();
    
   
    private float _rotX = 0, _rotY = 0, _rotZ = 0;
    private float _scale = 200f; 
    private float _offsetX = 0, _offsetY = 0;
    
    public MainWindow()
    {
        InitializeComponent();
        _model.Parse("/Users/maksimbelaev/Downloads/baby.obj");
        _model.CenterAndNormalizeModel();
        
        _buffer = new WriteableBitmap(new Avalonia.PixelSize(800, 600), new Avalonia.Vector(96, 96), 
            Avalonia.Platform.PixelFormat.Rgba8888, Avalonia.Platform.AlphaFormat.Premul);

        MyImage.Source = _buffer;

        this.KeyDown += OnKeyDown;
        Render();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        float angleStep = 0.3f;
        float moveStep = 0.08f;

        switch (e.Key)
        {
            case Key.W: _rotX += angleStep; break;
            case Key.S: _rotX -= angleStep; break;
            case Key.A: _rotY -= angleStep; break;
            case Key.D: _rotY += angleStep; break;
            case Key.Q: _rotZ += angleStep; break;
            case Key.E: _rotZ -= angleStep; break;
            case Key.Up: _offsetY += moveStep; break;
            case Key.Down: _offsetY -= moveStep; break;
            case Key.Left: _offsetX -= moveStep; break;
            case Key.Right: _offsetX += moveStep; break;
            case Key.OemPlus: case Key.Add: _scale *= 1.1f; break; 
            case Key.OemMinus: case Key.Subtract: _scale *= 0.9f; break; 
        }
        Render();
    }
    private void Render()
    {
        LineRenderer.Clear(_buffer);
        
        var rotationM = Matrix4x4.Multiply(Matrix4x4.CreateRotationX(_rotX),
            Matrix4x4.Multiply(Matrix4x4.CreateRotationY(_rotY),
                Matrix4x4.CreateRotationZ(_rotZ)));
        
        var translationM = Matrix4x4.CreateTranslation(_offsetX, _offsetY, 0);
        
        var scaleM = Matrix4x4.CreateScale(_scale);
        
        var modelM = Matrix4x4.Multiply(scaleM, Matrix4x4.Multiply(translationM, rotationM));

        var viewM = Matrix4x4.CreateLookAt(new Vector4(0, 0, 500), new Vector4(0, 0, 0), new Vector4(0, 1, 0));
        float aspect = 800f/ 600f;
        var projM = Matrix4x4.CreatePerspective(MathF.PI / 4, aspect, 1f, 1000f);
        
        var vpM = Matrix4x4.CreateViewport(800, 600);

        var finalM = Matrix4x4.Multiply(vpM, Matrix4x4.Multiply(projM, Matrix4x4.Multiply(viewM, modelM)));

        foreach (var face in _model.Faces)
        {
            for (int i = 0; i < face.Length; i++)
            {
                var v1 = Matrix4x4.Multiply(finalM, _model.Vertices[face[i]]);
                var v2 = Matrix4x4.Multiply(finalM, _model.Vertices[face[(i + 1) % face.Length]]);

              
                if (v1.W < 0.1f || v2.W < 0.1f) continue;
                
                if (v1.W != 0) { v1.X /= v1.W; v1.Y /= v1.W; }
                if (v2.W != 0) { v2.X /= v2.W; v2.Y /= v2.W; }
                
                float limit = 10000f;
                if (System.Math.Abs(v1.X) > limit || System.Math.Abs(v1.Y) > limit || 
                    System.Math.Abs(v2.X) > limit || System.Math.Abs(v2.Y) > limit) continue;

                LineRenderer.DrawLineBresenham(_buffer, (int)v1.X, (int)v1.Y, (int)v2.X, (int)v2.Y, 0xFFFFFFFF);
            }
        }
        MyImage.InvalidateVisual();
    }
}
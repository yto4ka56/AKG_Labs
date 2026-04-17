using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Lab3.Math;
using Lab3.IO;
using Lab3.Graphics;
using System;

namespace Lab3.Views;

public partial class MainWindow : Window
{
    private WriteableBitmap _buffer;
    private float[] _zBuffer;
    private ObjParser _model = new();
    
    private float _rotX = 0, _rotY = 0, _rotZ = 0;
    private float _scale = 1.0f;
    private bool _isDragging = false;
    private Avalonia.Point _lastMousePosition;
    
    private DateTime _lastRenderTime = DateTime.MinValue;
    private const double RenderIntervalMs = 16.0;

    private const uint BaseColor = 0xFF1E90FF;

    public MainWindow()
    {
        InitializeComponent();
        
        _model.Parse("/Users/maksimbelaev/Downloads/baby.obj");
        _model.CenterAndNormalizeModel();
        
        _buffer = new WriteableBitmap(new Avalonia.PixelSize(800, 600), new Avalonia.Vector(96, 96), 
            Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Premul);
        _zBuffer = new float[800 * 600];

        MyImage.Source = _buffer;

        this.PointerPressed += OnPointerPressed;
        this.PointerMoved += OnPointerMoved;
        this.PointerReleased += OnPointerReleased;
        this.PointerWheelChanged += OnPointerWheelChanged;

        this.KeyDown += (s, e) => {
            if (e.Key == Key.W) _rotX += 0.1f;
            if (e.Key == Key.S) _rotX -= 0.1f;
            if (e.Key == Key.A) _rotY -= 0.1f;
            if (e.Key == Key.D) _rotY += 0.1f;
            if (e.Key == Key.Q) _rotZ += 0.1f;
            if (e.Key == Key.E) _rotZ -= 0.1f;
            Render();
        };
        
        Render();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _lastMousePosition = point.Position;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging) return;

        var now = DateTime.UtcNow;
        if ((now - _lastRenderTime).TotalMilliseconds < RenderIntervalMs)
            return;

        var currentPoint = e.GetCurrentPoint(this);
        var currentPosition = currentPoint.Position;
        
        float deltaX = (float)(currentPosition.X - _lastMousePosition.X);
        float deltaY = (float)(currentPosition.Y - _lastMousePosition.Y);
        float sensitivity = 0.01f;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            _rotZ += deltaX * sensitivity;
        else
        {
            _rotY += deltaX * sensitivity;
            _rotX += deltaY * sensitivity;
        }

        _lastMousePosition = currentPosition;
        Render();
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e) => _isDragging = false;

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        float zoomFactor = 1.1f;
        if (e.Delta.Y > 0) _scale *= zoomFactor;
        else if (e.Delta.Y < 0) _scale /= zoomFactor;
        Render();
    }

    private unsafe void Render()
    {
        _lastRenderTime = DateTime.UtcNow;

        using (var buf = _buffer.Lock())
        {
            uint* ptr = (uint*)buf.Address;
            int width  = _buffer.PixelSize.Width;
            int height = _buffer.PixelSize.Height;

            Renderer.Clear(ptr, width, height, _zBuffer);
            
            var scaleM = Matrix4x4.CreateScale(_scale);
            var rotX_M = Matrix4x4.CreateRotationX(_rotX);
            var rotY_M = Matrix4x4.CreateRotationY(_rotY);
            var rotZ_M = Matrix4x4.CreateRotationZ(_rotZ);
            var rotM   = Matrix4x4.Multiply(rotZ_M, Matrix4x4.Multiply(rotX_M, rotY_M));
            var modelM = Matrix4x4.Multiply(rotM, scaleM);

            Vector4 cameraPos = new Vector4(0, 0, 2);
            var viewM  = Matrix4x4.CreateLookAt(cameraPos, new Vector4(0, 0, 0), new Vector4(0, 1, 0));
            var projM  = Matrix4x4.CreatePerspective(MathF.PI / 4, (float)width / height, 0.1f, 100f);
            var vpM    = Matrix4x4.CreateViewport(width, height);
            var transform = Matrix4x4.Multiply(vpM, Matrix4x4.Multiply(projM, viewM));
            
            Vector4 lightDir = Vector4.Normalize(new Vector4(0.5f, 1f, 1f, 0));

            foreach (var face in _model.Faces)
            {
                Vector4 v1w = Matrix4x4.Multiply(modelM, _model.Vertices[face[0]]);
                Vector4 v2w = Matrix4x4.Multiply(modelM, _model.Vertices[face[1]]);
                Vector4 v3w = Matrix4x4.Multiply(modelM, _model.Vertices[face[2]]);
                
                Vector4 n1 = Vector4.Normalize(Matrix4x4.Multiply(rotM, _model.VertexNormals[face[0]]));
                Vector4 n2 = Vector4.Normalize(Matrix4x4.Multiply(rotM, _model.VertexNormals[face[1]]));
                Vector4 n3 = Vector4.Normalize(Matrix4x4.Multiply(rotM, _model.VertexNormals[face[2]]));
                
                Vector4 faceNormal = Vector4.Normalize(Vector4.Cross(v2w - v1w, v3w - v1w));
                Vector4 center = new Vector4(
                    (v1w.X + v2w.X + v3w.X) / 3f,
                    (v1w.Y + v2w.Y + v3w.Y) / 3f,
                    (v1w.Z + v2w.Z + v3w.Z) / 3f);
                if (Vector4.Dot(faceNormal, Vector4.Normalize(cameraPos - center)) < 0)
                    continue;
                
                Vector4 p1 = Project(v1w, transform);
                Vector4 p2 = Project(v2w, transform);
                Vector4 p3 = Project(v3w, transform);

                Renderer.DrawTrianglePhong(
                    ptr, width, height, _zBuffer,
                    p1, p2, p3,
                    v1w, v2w, v3w,
                    n1, n2, n3,
                    cameraPos, lightDir,
                    BaseColor);
            }
        }

        MyImage.InvalidateVisual();
    }

    private Vector4 Project(Vector4 v, Matrix4x4 mat)
    {
        Vector4 res = Matrix4x4.Multiply(mat, v);
        if (res.W != 0) { res.X /= res.W; res.Y /= res.W; res.Z /= res.W; }
        return res;
    }
}

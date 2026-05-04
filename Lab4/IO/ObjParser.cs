using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Lab4.Math;

namespace Lab4.IO;

public struct FaceVertex
{
    public int V;
    public int Vt;
    public int Vn;
}

public class ObjParser
{
    public List<Vector4> Vertices = new();
    public List<Vector2> UVs = new(); // Текстурные координаты
    public List<Vector4> VertexNormals = new();
    
    public List<FaceVertex[]> Faces = new();

    public void Parse(string path)
    {
        foreach (var line in File.ReadLines(path))
        {
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            if (parts[0] == "v") 
            {
                Vertices.Add(new Vector4(
                    float.Parse(parts[1], CultureInfo.InvariantCulture),
                    float.Parse(parts[2], CultureInfo.InvariantCulture),
                    float.Parse(parts[3], CultureInfo.InvariantCulture)));
            }
            else if (parts[0] == "vt") 
            {
                UVs.Add(new Vector2(
                    float.Parse(parts[1], CultureInfo.InvariantCulture),
                    float.Parse(parts[2], CultureInfo.InvariantCulture)));
            }
            else if (parts[0] == "vn")
            {
                VertexNormals.Add(new Vector4(
                    float.Parse(parts[1], CultureInfo.InvariantCulture),
                    float.Parse(parts[2], CultureInfo.InvariantCulture),
                    float.Parse(parts[3], CultureInfo.InvariantCulture)));
            }
            else if (parts[0] == "f")
            {
                List<FaceVertex> faceVertices = new();
                for (int i = 1; i < parts.Length; i++)
                {
                    var indices = parts[i].Split('/');
                    FaceVertex fv = new FaceVertex();
                    
                    fv.V = int.Parse(indices[0]) - 1;
                    if (indices.Length > 1 && !string.IsNullOrEmpty(indices[1]))
                        fv.Vt = int.Parse(indices[1]) - 1;
                    else 
                        fv.Vt = -1; // Нет UV
                        
                    if (indices.Length > 2 && !string.IsNullOrEmpty(indices[2]))
                        fv.Vn = int.Parse(indices[2]) - 1;
                    else
                        fv.Vn = -1;
                        
                    faceVertices.Add(fv);
                }
                
                // Триангуляция многоугольников
                for (int i = 1; i < faceVertices.Count - 1; i++)
                {
                    Faces.Add(new FaceVertex[] { faceVertices[0], faceVertices[i], faceVertices[i + 1] });
                }
            }
        }
        // Если нормалей не было в файле, можно их сгенерировать здесь (как было в старом коде)
    }
    
    // Оставим метод масштабирования, он полезный
    public void CenterAndNormalizeModel()
    {
        if (Vertices.Count == 0) return;

        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
        
        foreach (var v in Vertices)
        {
            if (v.X < minX) minX = v.X; if (v.X > maxX) maxX = v.X;
            if (v.Y < minY) minY = v.Y; if (v.Y > maxY) maxY = v.Y;
            if (v.Z < minZ) minZ = v.Z; if (v.Z > maxZ) maxZ = v.Z;
        }
        
        float centerX = (minX + maxX) / 2.0f;
        float centerY = (minY + maxY) / 2.0f;
        float centerZ = (minZ + maxZ) / 2.0f;
        
        float sizeX = maxX - minX;
        float sizeY = maxY - minY;
        float sizeZ = maxZ - minZ;
        
        float maxDimension = System.Math.Max(sizeX, System.Math.Max(sizeY, sizeZ));
        float scaleFactor = maxDimension > 0 ? 2.0f / maxDimension : 1.0f;
        
        for (int i = 0; i < Vertices.Count; i++)
        {
            var v = Vertices[i];
            Vertices[i] = new Vector4(
                (v.X - centerX) * scaleFactor,
                (v.Y - centerY) * scaleFactor,
                (v.Z - centerZ) * scaleFactor,
                v.W
            );
        }
    }
}
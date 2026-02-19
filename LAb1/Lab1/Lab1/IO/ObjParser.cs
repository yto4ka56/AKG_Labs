using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Lab1.Math;


namespace Lab1.IO;

public class ObjParser
{
    public List<Vector4> Vertices = new();
    public List<int[]> Faces = new();

    public void Parse(string path)
    {
        foreach (var line in File.ReadLines(path))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            if (parts[0] == "v") 
            {
                Vertices.Add(new Vector4(
                    float.Parse(parts[1], CultureInfo.InvariantCulture),
                    float.Parse(parts[2], CultureInfo.InvariantCulture),
                    float.Parse(parts[3], CultureInfo.InvariantCulture)));
            }
            else if (parts[0] == "f")
            {
                int[] face = new int[parts.Length - 1];
                for (int i = 1; i < parts.Length; i++)
                {
                    string vIndexStr = parts[i].Split('/')[0];
                    int index = int.Parse(vIndexStr);
                    face[i - 1] = index > 0 ? index - 1 : Vertices.Count + index;
                }
                Faces.Add(face);
            }
        }
    }
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
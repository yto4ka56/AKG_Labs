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
}
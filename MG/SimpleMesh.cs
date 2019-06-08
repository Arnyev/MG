using System.Collections.Generic;
using System.Numerics;

namespace MG
{
    public class SimpleMesh
    {
        public readonly List<TriangleIndices> Indices;
        public readonly List<Vector3> Points;

        public readonly float MinX = float.MaxValue;
        public readonly float MinY = float.MaxValue;
        public readonly float MinZ = float.MaxValue;

        public readonly float MaxX = float.MinValue;
        public readonly float MaxY = float.MinValue;
        public readonly float MaxZ = float.MinValue;

        public Vector3 MinValues => new Vector3(MinX, MinY, MinZ);
        public Vector3 MaxValues => new Vector3(MaxX, MaxY, MaxZ);

        public SimpleMesh(List<TriangleIndices> indices, List<Vector3> points)
        {
            Indices = indices;
            Points = points;

            foreach (var pos in points)
            {
                if (pos.X > MaxX)
                    MaxX = pos.X;

                if (pos.X < MinX)
                    MinX = pos.X;

                if (pos.Y > MaxY)
                    MaxY = pos.Y;

                if (pos.Y < MinY)
                    MinY = pos.Y;

                if (pos.Z > MaxZ)
                    MaxZ = pos.Z;

                if (pos.Z < MinZ)
                    MinZ = pos.Z;
            }
        }
    }

    public struct TriangleIndices
    {
        public readonly int A, B, C;

        public TriangleIndices(int a, int b, int c)
        {
            A = a;
            B = b;
            C = c;
        }
    }
}

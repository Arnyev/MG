using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MG
{
    public struct Intersection
    {
        public readonly Vector2 A;
        public readonly Vector2 B;

        public Intersection(Vector2 a, Vector2 b)
        {
            A = a;
            B = b;
        }

        public Intersection(float au, float av, float bu, float bv)
        {
            A = new Vector2(au, av);
            B = new Vector2(bu, bv);
        }
    }

    public struct IntersectionData
    {
        public readonly float P1;
        public readonly float P2;
        public readonly float P3;
        public readonly float D;

        public IntersectionData(float p1, float p2, float p3, float d)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
            D = d;
        }
    }

    public struct TriangleParameters
    {
        public readonly Vector2 P1;
        public readonly Vector2 P2;
        public readonly Vector2 P3;

        public TriangleParameters(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }
    }

    public class BoundingBox
    {
        public readonly Triangle[] Triangles;
        public readonly float MinU;
        public readonly float MaxU;
        public readonly float MinV;
        public readonly float MaxV;

        public readonly float MinX;
        public readonly float MinY;
        public readonly float MinZ;

        public readonly float MaxX;
        public readonly float MaxY;
        public readonly float MaxZ;

        public BoundingBox(Triangle[] triangles, ParameterRange range)
        {
            Triangles = triangles;
            MinU = range.MinU;
            MaxU = range.MaxU;
            MinV = range.MinV;
            MaxV = range.MaxV;

            MaxX = triangles.Max(t => t.MaxX);
            MaxY = triangles.Max(t => t.MaxY);
            MaxZ = triangles.Max(t => t.MaxZ);

            MinX = triangles.Min(t => t.MinX);
            MinY = triangles.Min(t => t.MinY);
            MinZ = triangles.Min(t => t.MinZ);
        }

        public BoundingBox(List<TriangleIndices> triangleIndices, List<Vector3> points, ParameterRange range)
        : this(triangleIndices.Select(x => new Triangle(points[x.A], points[x.B], points[x.C])).ToArray(), range)
        {
        }

        public bool CheckBoundingCubes(BoundingBox other)
        {
            if (MaxX < other.MinX || MinX > other.MaxX ||
                MaxY < other.MinY || MinY > other.MaxY ||
                MaxZ < other.MinZ || MinZ > other.MaxZ)
                return false;

            return true;
        }
    }

    public struct Edge
    {
        public readonly Vector3 Start;
        public readonly Vector3 Direction;

        public Edge(Vector3 start, Vector3 end)
        {
            Start = start;
            Direction = end - start;
        }
    }

    public struct Triangle
    {
        public readonly Vector3 A;
        public readonly Vector3 B;
        public readonly Vector3 C;

        public Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            A = a;
            B = b;
            C = c;
        }

        public float MaxX => (A.X >= B.X && A.X >= C.X) ? A.X : (B.X >= C.X) ? B.X : C.X;
        public float MaxY => (A.Y >= B.Y && A.Y >= C.Y) ? A.Y : (B.Y >= C.Y) ? B.Y : C.Y;
        public float MaxZ => (A.Z >= B.Z && A.Z >= C.Z) ? A.Z : (B.Z >= C.Z) ? B.Z : C.Z;
        public float MinX => (A.X <= B.X && A.X <= C.X) ? A.X : (B.X <= C.X) ? B.X : C.X;
        public float MinY => (A.Y <= B.Y && A.Y <= C.Y) ? A.Y : (B.Y <= C.Y) ? B.Y : C.Y;
        public float MinZ => (A.Z <= B.Z && A.Z <= C.Z) ? A.Z : (B.Z <= C.Z) ? B.Z : C.Z;

        public bool CheckBoundingBox(Triangle other)
        {
            if (MaxX < other.MinX || MinX > other.MaxX ||
                MaxY < other.MinY || MinY > other.MaxY ||
                MaxZ < other.MinZ || MinZ > other.MaxZ)
                return false;

            return true;
        }

        public override string ToString()
        {
            return $"{A}, {B}, {C}";
        }
    }

    public struct Ray
    {
        public readonly Vector3 Start;
        public readonly Vector3 Direction;

        public Ray(Vector3 start, Vector3 direction)
        {
            Start = start;
            Direction = direction;
        }
    }

    public struct Plane
    {
        public readonly Vector3 Normal;
        public readonly float NormalLength;
        public readonly float Distance;

        public Plane(Vector3 normal, Vector3 point)
        {
            Normal = normal;
            Distance = Vector3.Dot(-Normal, point);
            NormalLength = normal.LengthSquared();
        }

        public bool IsPointOnPositiveSide(Vector3 point) => Vector3.Dot(point, Normal) >= -Distance;
    }

    public struct HalfEdge
    {
        public readonly int EndVertex;
        public readonly int Opp;
        public readonly int Face;
        public readonly int Next;

        public HalfEdge(int endVertex, int opp, int face, int next)
        {
            EndVertex = endVertex;
            Opp = opp;
            Face = face;
            Next = next;
        }
    }

    public struct FaceData
    {
        public readonly int FaceIndex;
        public readonly int EnteredFromHalfEdge;
        // If the face turns out not to be visible, this half edge will be marked as horizon edge

        public FaceData(int fi, int he)
        {
            FaceIndex = fi;
            EnteredFromHalfEdge = he;
        }
    }

    public class Face
    {
        public int HalfEdge = int.MaxValue;
        public Plane Plane;
        public float MostDistantPointDist;
        public int MostDistantPoint;
        public int VisibilityCheckedOnIteration;
        public bool IsVisibleFaceOnCurrentIteration;
        public bool InFaceStack;
        public int HorizonEdgesOnCurrentIteration;

        public List<int> PointsOnPositiveSide;
        public void Disable() => HalfEdge = int.MaxValue;
        public bool IsDisabled() => HalfEdge == int.MaxValue;
    }
}
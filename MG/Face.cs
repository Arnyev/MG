using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MG
{
    public class IntersectionRange
    {
        public readonly float AMinU;
        public readonly float AMaxU;
        public readonly float AMinV;
        public readonly float AMaxV;

        public readonly float BMinU;
        public readonly float BMaxU;
        public readonly float BMinV;
        public readonly float BMaxV;
        public readonly bool ParametersPeriodic;
        public readonly float ParameterMax;

        public IntersectionRange(BoundingBox bA, BoundingBox bB, bool parametersPeriodic, float parameterMax)
        {
            AMinU = bA.MinU;
            AMinV = bA.MinV;
            AMaxU = bA.MaxU;
            AMaxV = bA.MaxV;

            BMinU = bB.MinU;
            BMinV = bB.MinV;
            BMaxU = bB.MaxU;
            BMaxV = bB.MaxV;
            ParametersPeriodic = parametersPeriodic;
            ParameterMax = parameterMax;
        }

        public bool IsAdjacent(IntersectionRange other)
        {
            const float e = 1e-4f;
            var adjacentAU = Math.Abs(AMinU - other.AMinU) < e || Math.Abs(AMinU - other.AMaxU) < e || Math.Abs(AMaxU - other.AMinU) < e;
            var adjacentAV = Math.Abs(AMinV - other.AMinV) < e || Math.Abs(AMinV - other.AMaxV) < e || Math.Abs(AMaxV - other.AMinV) < e;
            var adjacentBU = Math.Abs(BMinU - other.BMinU) < e || Math.Abs(BMinU - other.BMaxU) < e || Math.Abs(BMaxU - other.BMinU) < e;
            var adjacentBV = Math.Abs(BMinV - other.BMinV) < e || Math.Abs(BMinV - other.BMaxV) < e || Math.Abs(BMaxV - other.BMinV) < e;

            if (ParametersPeriodic)
            {
                adjacentAU |= Math.Abs(AMinU) < e && Math.Abs(other.AMaxU - ParameterMax) < e || Math.Abs(AMaxU - ParameterMax) < e && Math.Abs(other.AMinU) < e;
                adjacentAV |= Math.Abs(AMinV) < e && Math.Abs(other.AMaxV - ParameterMax) < e || Math.Abs(AMaxV - ParameterMax) < e && Math.Abs(other.AMinV) < e;
            }

            return adjacentAU && adjacentAV;
            //&& adjacentBU && adjacentBV;
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

        public BoundingBox(Triangle[] triangles, AngleRange range)
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

        public BoundingBox(List<TriangleIndices> triangleIndices, List<Vector3> points, AngleRange range)
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
using System.Collections.Generic;
using System.Numerics;

namespace MG
{
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
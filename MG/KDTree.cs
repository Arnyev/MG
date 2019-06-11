using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MG
{
    public class KDTree
    {
        public readonly KDTreeNode StartingNode;

        public KDTree(SimpleMesh meshA, SimpleMesh meshB, bool additionalSplit)
        {
            var indicesA = Enumerable.Range(0, meshA.Indices.Count).ToList();
            var indicesB = Enumerable.Range(0, meshB.Indices.Count).ToList();

            var minValues = Vector3.Min(meshA.MinValues, meshB.MinValues);
            var maxValues = Vector3.Max(meshA.MaxValues, meshB.MaxValues);
            StartingNode = new KDTreeNode(indicesA, indicesB, meshA, meshB, 0, Dimension.X, minValues, maxValues, additionalSplit);
        }

        public List<Vector4> GetIntersectionPoints(List<Intersection> ranges, float minimumDifference = 0)
        {
            var intersections = new List<Vector4>();
            StartingNode.GetIntersectionPoints(intersections, ranges, minimumDifference);
            return intersections;
        }
    }

    public class KDTreeNode
    {
        private readonly bool _additionalSplit;
        private const int ExpectedCount = 30;
        private const int ExpectedSmallCount = 6;
        private const int MaxLevel = 9;
        public List<int> MyIndicesA;
        public List<int> MyIndicesB;
        public readonly SimpleMesh MeshA;
        public readonly SimpleMesh MeshB;
        public readonly int _level;
        public readonly Dimension _dimension;
        public readonly Vector3 MinValues;
        public readonly Vector3 MaxValues;
        public readonly List<int> NodeTriangles;
        public KDTreeNode Smaller;
        public KDTreeNode Bigger;
        public readonly int Level;
        public readonly float DivisionValue;
        public readonly Dimension Dimension;
        private long _possibleIntersections;

        private List<int>[] _dividedTrianglesA;
        private List<int>[] _dividedTrianglesB;

        public void GetIntersectionPoints(List<Vector4> points, List<Intersection> ranges, float minimumDifference)
        {
            if (Smaller != null)
                Smaller.GetIntersectionPoints(points, ranges, minimumDifference);
            if (Bigger != null)
                Bigger.GetIntersectionPoints(points, ranges, minimumDifference);

            if (_possibleIntersections == 0)
                return;

            if (_dividedTrianglesA != null)
            {
                for (int i = 0; i < _dividedTrianglesA.Length; i++)
                {
                    var triangleIndicesB = _dividedTrianglesB[i];
                    var triangleIndicesA = _dividedTrianglesA[i];

                    if (triangleIndicesA.Count > 0 && triangleIndicesB.Count > 0)
                        GetIntersectionsBetweenTriangles(points, triangleIndicesB, triangleIndicesA, ranges, minimumDifference);
                }
            }

            else
            {
                var triangleIndicesA = MyIndicesA;
                var triangleIndicesB = MyIndicesB;

                GetIntersectionsBetweenTriangles(points, triangleIndicesB, triangleIndicesA, ranges, minimumDifference);
            }
        }

        private void GetIntersectionsBetweenTriangles(List<Vector4> points, List<int> triangleIndicesB,
            List<int> triangleIndicesA, List<Intersection> intersections, float minimumDifference)
        {
            var trianglesB = new List<Triangle>();

            var triIndicesB = MeshB.Indices;
            var triVerticesB = MeshB.Points;

            var triIndicesA = MeshA.Indices;
            var triVerticesA = MeshA.Points;

            foreach (var i in triangleIndicesB)
            {
                var triIndices = triIndicesB[i];

                trianglesB.Add(new Triangle(
                    triVerticesB[triIndices.A],
                    triVerticesB[triIndices.B],
                    triVerticesB[triIndices.C]));
            }

            foreach (var i in triangleIndicesA)
            {
                var triIndices = triIndicesA[i];
                var p1 = triVerticesA[triIndices.A];
                var p2 = triVerticesA[triIndices.B];
                var p3 = triVerticesA[triIndices.C];

                var triangleA = new Triangle(p1, p2, p3);

                for (var j = 0; j < trianglesB.Count; j++)
                {
                    var triangleB = trianglesB[j];
                    if (!triangleA.CheckBoundingBox(triangleB))
                        continue;

                    var aParam = MeshA.ParameterValues[i];
                    var bParam = MeshB.ParameterValues[triangleIndicesB[j]];

                    if (minimumDifference > 0)
                    {
                        var diff = aParam.P1 - bParam.P1;

                        if (diff.LengthSquared() < minimumDifference)
                            continue;
                    }

                    if (!CheckIntersection(points, intersections, triangleB, p1, p2, triIndices, aParam.P1, aParam.P2,
                        bParam))
                    {
                        if (!CheckIntersection(points, intersections, triangleB, p2, p3, triIndices, aParam.P2,
                            aParam.P3, bParam))
                        {
                            if (CheckIntersection(points, intersections, triangleB, p3, p1, triIndices, aParam.P3,
                                aParam.P1, bParam))
                                break;
                        }
                        else
                            break;
                    }
                    else
                        break;
                }
            }
        }

        private bool CheckIntersection(List<Vector4> points, List<Intersection> intersections, Triangle triangleB, Vector3 p1, Vector3 p2,
            TriangleIndices triIndices, Vector2 myParameterP1, Vector2 myParameterP2, TriangleParameters otherParameter)
        {
            if (!IntersectionCurve.CheckIntersection(triangleB.A, triangleB.B, triangleB.C, p1, p2 - p1, out var id))
                return false;

            var myParams = myParameterP1 * (1 - id.D) + myParameterP2 * id.D;
            var otherParams = otherParameter.P1 * id.P1 + otherParameter.P2 * id.P2 +
                              otherParameter.P3 * id.P3;

            var worldPoint = new Vector4(p1 * (1 - id.D) + p2 * id.D, 1);
            points.Add(worldPoint);
            intersections.Add(new Intersection(myParams, otherParams));

            return true;
        }

        public KDTreeNode(List<int> triangleIndicesA, List<int> triangleIndicesB, SimpleMesh meshA, SimpleMesh meshB, int level, Dimension dimension, Vector3 minValues, Vector3 maxValues, bool additionalSplit)
        {
            MeshA = meshA;
            MeshB = meshB;
            _level = level;
            _dimension = dimension;
            MinValues = minValues;
            MaxValues = maxValues;
            _additionalSplit = additionalSplit;

            MyIndicesA = triangleIndicesA;
            MyIndicesB = triangleIndicesB;

            if (triangleIndicesA.LongCount() * triangleIndicesB.Count >= ExpectedCount)
                PrepareChildNodes(triangleIndicesA, triangleIndicesB, meshA, meshB, level, dimension, minValues,
                    maxValues);

            _possibleIntersections = MyIndicesA.Count * MyIndicesB.Count;

            if (_possibleIntersections > ExpectedCount * ExpectedCount && _additionalSplit)
            {
                MyIndicesA = new List<int>();
                MyIndicesB = new List<int>();
                PrepareAdditionalSplit(meshA, meshB, dimension, minValues, maxValues);
            }
        }

        private void PrepareAdditionalSplit(SimpleMesh meshA, SimpleMesh meshB, Dimension dimension, Vector3 minValues,
            Vector3 maxValues)
        {
            var dimDivisor =
                (MyIndicesA.Count > MyIndicesB.Count ? MyIndicesA.Count : MyIndicesB.Count) / ExpectedSmallCount;

            _dividedTrianglesA = new List<int>[dimDivisor];
            _dividedTrianglesB = new List<int>[dimDivisor];

            var ranges = new float[dimDivisor + 1];

            for (int i = 0; i < dimDivisor; i++)
            {
                _dividedTrianglesA[i] = new List<int>();
                _dividedTrianglesB[i] = new List<int>();
            }

            switch (dimension)
            {
                case Dimension.X:
                    for (int i = 0; i <= dimDivisor; i++)
                        ranges[i] = minValues.Y + (maxValues.Y - minValues.Y) * i / dimDivisor;

                    AdditionallySplitByY(meshA, dimDivisor, ranges, MyIndicesA, _dividedTrianglesA);
                    AdditionallySplitByY(meshB, dimDivisor, ranges, MyIndicesB, _dividedTrianglesB);
                    break;

                case Dimension.Y:
                    for (int i = 0; i <= dimDivisor; i++)
                        ranges[i] = minValues.Z + (maxValues.Z - minValues.Z) * i / dimDivisor;

                    AdditionallySplitByZ(meshA, dimDivisor, ranges, MyIndicesA, _dividedTrianglesA);
                    AdditionallySplitByZ(meshB, dimDivisor, ranges, MyIndicesB, _dividedTrianglesB);
                    break;
                default:

                case Dimension.Z:
                    for (int i = 0; i <= dimDivisor; i++)
                        ranges[i] = minValues.X + (maxValues.X - minValues.X) * i / dimDivisor;

                    AdditionallySplitByX(meshA, dimDivisor, ranges, MyIndicesA, _dividedTrianglesA);
                    AdditionallySplitByX(meshB, dimDivisor, ranges, MyIndicesB, _dividedTrianglesB);
                    break;
            }
        }

        private static void AdditionallySplitByX(SimpleMesh mesh, long dimDivisor, float[] ranges, List<int> triangleIndices, List<int>[] dividedTriangles)
        {
            foreach (var triIndex in triangleIndices)
            {
                var triangle = mesh.Indices[triIndex];
                var pA = mesh.Points[triangle.A];
                var pB = mesh.Points[triangle.B];
                var pC = mesh.Points[triangle.C];

                float minX;
                if (pA.X < pB.X)
                    minX = pA.X < pC.X ? pA.X : pC.X;
                else
                    minX = pB.X < pC.X ? pB.X : pC.X;

                float maxX;
                if (pA.X > pB.X)
                    maxX = pA.X > pC.X ? pA.X : pC.X;
                else
                    maxX = pB.X > pC.X ? pB.X : pC.X;

                for (int i = 0; i < dimDivisor; i++)
                    if (minX < ranges[i + 1] && maxX > ranges[i])
                        dividedTriangles[i].Add(triIndex);
            }
        }

        private static void AdditionallySplitByY(SimpleMesh meshA, long dimDivisor, float[] ranges, List<int> indices, List<int>[] dividedTriangles)
        {
            foreach (var triIndex in indices)
            {
                var triangle = meshA.Indices[triIndex];
                var pA = meshA.Points[triangle.A];
                var pB = meshA.Points[triangle.B];
                var pC = meshA.Points[triangle.C];

                float minY;
                if (pA.Y < pB.Y)
                    minY = pA.Y < pC.Y ? pA.Y : pC.Y;
                else
                    minY = pB.Y < pC.Y ? pB.Y : pC.Y;

                float maxY;
                if (pA.Y > pB.Y)
                    maxY = pA.Y > pC.Y ? pA.Y : pC.Y;
                else
                    maxY = pB.Y > pC.Y ? pB.Y : pC.Y;

                for (int i = 0; i < dimDivisor; i++)
                    if (minY < ranges[i + 1] && maxY > ranges[i])
                        dividedTriangles[i].Add(triIndex);
            }
        }

        private static void AdditionallySplitByZ(SimpleMesh meshA, long dimDivisor, float[] ranges, List<int> triangleIndices, List<int>[] dividedTriangles)
        {
            foreach (var triIndex in triangleIndices)
            {
                var triangle = meshA.Indices[triIndex];
                var pA = meshA.Points[triangle.A];
                var pB = meshA.Points[triangle.B];
                var pC = meshA.Points[triangle.C];

                float minZ;
                if (pA.Z < pB.Z)
                    minZ = pA.Z < pC.Z ? pA.Z : pC.Z;
                else
                    minZ = pB.Z < pC.Z ? pB.Z : pC.Z;

                float maxZ;
                if (pA.Z > pB.Z)
                    maxZ = pA.Z > pC.Z ? pA.Z : pC.Z;
                else
                    maxZ = pB.Z > pC.Z ? pB.Z : pC.Z;

                for (int i = 0; i < dimDivisor; i++)
                    if (minZ <= ranges[i + 1] && maxZ >= ranges[i])
                        dividedTriangles[i].Add(triIndex);
            }
        }

        private void PrepareChildNodes(List<int> triangleIndicesA, List<int> triangleIndicesB, SimpleMesh meshA,
            SimpleMesh meshB, int level,
            Dimension dimension, Vector3 minValues, Vector3 maxValues)
        {
            MyIndicesA = new List<int>();
            var smallerTriangleIndicesA = new List<int>();
            var biggerTriangleIndicesA = new List<int>();

            MyIndicesB = new List<int>();
            var smallerTriangleIndicesB = new List<int>();
            var biggerTriangleIndicesB = new List<int>();

            var nextDim = NextDimension(dimension);
            Vector3 mid1;
            Vector3 mid2;

            switch (dimension)
            {
                case Dimension.X:
                    var midX = (MinValues.X + MaxValues.X) / 2;
                    SplitByX(triangleIndicesA, MyIndicesA, meshA, biggerTriangleIndicesA, smallerTriangleIndicesA,
                        midX);
                    SplitByX(triangleIndicesB, MyIndicesB, meshB, biggerTriangleIndicesB, smallerTriangleIndicesB,
                        midX);
                    mid1 = new Vector3(midX, maxValues.Y, maxValues.Z);
                    mid2 = new Vector3(midX, minValues.Y, minValues.Z);
                    break;
                case Dimension.Y:
                    var midY = (MinValues.Y + MaxValues.Y) / 2;
                    SplitByY(triangleIndicesA, MyIndicesA, meshA, biggerTriangleIndicesA, smallerTriangleIndicesA,
                        midY);
                    SplitByY(triangleIndicesB, MyIndicesB, meshB, biggerTriangleIndicesB, smallerTriangleIndicesB,
                        midY);
                    mid1 = new Vector3(maxValues.X, midY, maxValues.Z);
                    mid2 = new Vector3(minValues.X, midY, minValues.Z);
                    break;
                default:
                case Dimension.Z:
                    var midZ = (MinValues.Z + MaxValues.Z) / 2;
                    SplitByZ(triangleIndicesA, MyIndicesA, meshA, biggerTriangleIndicesA, smallerTriangleIndicesA,
                        midZ);
                    SplitByZ(triangleIndicesB, MyIndicesB, meshB, biggerTriangleIndicesB, smallerTriangleIndicesB,
                        midZ);
                    mid1 = new Vector3(maxValues.X, maxValues.Y, midZ);
                    mid2 = new Vector3(minValues.X, minValues.Y, midZ);
                    break;
            }

            if (triangleIndicesA.Count * triangleIndicesB.Count > MyIndicesA.Count * MyIndicesB.Count * 2)
            {
                Smaller = new KDTreeNode(smallerTriangleIndicesA, smallerTriangleIndicesB, meshA, meshB, level + 1,
                    nextDim, minValues, mid1, _additionalSplit);

                Bigger = new KDTreeNode(biggerTriangleIndicesA, biggerTriangleIndicesB, meshA, meshB, level + 1,
                    nextDim, mid2, maxValues, _additionalSplit);
            }
            else
            {
                MyIndicesA = triangleIndicesA;
                MyIndicesB = triangleIndicesB;
            }
        }


        private static Dimension NextDimension(Dimension dim) => dim == Dimension.X ? Dimension.Y : dim == Dimension.Y ? Dimension.Z : Dimension.X;

        private static void SplitByX(List<int> inputIndices, List<int> myTriangleIndices, SimpleMesh mesh, List<int> biggerTriangleIndices, List<int> smallerTriangleIndices, float midX)
        {
            foreach (var i in inputIndices)
            {
                var triangle = mesh.Indices[i];
                var pA = mesh.Points[triangle.A];
                var pB = mesh.Points[triangle.B];
                var pC = mesh.Points[triangle.C];

                if (pA.X > midX && pB.X > midX && pC.X > midX)
                    biggerTriangleIndices.Add(i);

                else if (pA.X < midX && pB.X < midX && pC.X < midX)
                    smallerTriangleIndices.Add(i);

                else
                {
                    smallerTriangleIndices.Add(i);
                    myTriangleIndices.Add(i);
                    biggerTriangleIndices.Add(i);
                }
            }
        }

        private static void SplitByY(List<int> inputIndices, List<int> myTriangleIndices, SimpleMesh mesh, List<int> biggerTriangleIndices, List<int> smallerTriangleIndices, float midY)
        {
            foreach (var i in inputIndices)
            {
                var triangle = mesh.Indices[i];
                var pA = mesh.Points[triangle.A];
                var pB = mesh.Points[triangle.B];
                var pC = mesh.Points[triangle.C];

                if (pA.Y > midY && pB.Y > midY && pC.Y > midY)
                    biggerTriangleIndices.Add(i);

                else if (pA.Y < midY && pB.Y < midY && pC.Y < midY)
                    smallerTriangleIndices.Add(i);

                else
                {
                    smallerTriangleIndices.Add(i);
                    myTriangleIndices.Add(i);
                    biggerTriangleIndices.Add(i);
                }
            }
        }

        private static void SplitByZ(List<int> inputIndices, List<int> myTriangleIndices, SimpleMesh mesh, List<int> biggerTriangleIndices, List<int> smallerTriangleIndices, float midZ)
        {
            foreach (var i in inputIndices)
            {
                var triangle = mesh.Indices[i];
                var pA = mesh.Points[triangle.A];
                var pB = mesh.Points[triangle.B];
                var pC = mesh.Points[triangle.C];

                if (pA.Z > midZ && pB.Z > midZ && pC.Z > midZ)
                    biggerTriangleIndices.Add(i);

                else if (pA.Z < midZ && pB.Z < midZ && pC.Z < midZ)
                    smallerTriangleIndices.Add(i);

                else
                {
                    smallerTriangleIndices.Add(i);
                    myTriangleIndices.Add(i);
                    biggerTriangleIndices.Add(i);
                }
            }
        }
    }

    public enum Dimension
    {
        X, Y, Z
    }
}

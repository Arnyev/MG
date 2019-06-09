using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace MG
{
    class IntersectionCurve : IDrawableObject
    {
        public readonly IIntersecting A;
        public readonly IIntersecting B;
        private readonly Vector4 cursor;

        public float Division { get; set; } = 0.1f;
        public int TriangleCount { get; set; } = 10;

        public float MinimumDifference
        {
            get => A == B ? _minimumDifference : 0;
            set => _minimumDifference = value;
        }

        private float _minimumDifference = 0.5f;

        public IntersectionCurve(IIntersecting a, IIntersecting b, Cursor3D cursor)
        {
            this.A = a;
            this.B = b;
            this.cursor = cursor.Position;
        }

        private static List<Vector2> ComputeApproximatingPoints(List<IntersectionRange> ranges, IIntersecting a, out List<Vector4> drawingPoints)
        {
            drawingPoints = new List<Vector4>();

            var sw = new Stopwatch();
            sw.Start();
            var points = GetPointsFromTriangles(ranges);

            var closestArrays = GetClosestArrays(points);

            var minIndex = GetStartingIndex(points);
            var neighbourCount = 12;
            if (points.Count < neighbourCount)
                return new List<Vector2>();

            var averageSize = 20;

            var newPointList = FindPointsBFS(points, minIndex, neighbourCount, closestArrays);
            if (newPointList.Count < averageSize * 2)
                return new List<Vector2>();

            var finalPoints = new List<Vector2>();

            for (int i = 0; i < averageSize - 1; i++)
            {
                var point = new Vector2();

                int j = 0;
                for (; j <= i; j++)
                    point += newPointList[j];

                point /= j;
                finalPoints.Add(point);
                drawingPoints.Add(a.GetWorldPoint(point.X, point.Y));
            }

            for (int i = 0; i < newPointList.Count; i++)
            {
                var point = new Vector2();
                int j = 0;
                for (; j < averageSize && i + j < newPointList.Count; j++)
                    point += newPointList[i + j];

                point /= j;
                finalPoints.Add(point);
                drawingPoints.Add(a.GetWorldPoint(point.X, point.Y));
            }
            var sa = sw.ElapsedMilliseconds;

            return points;
        }

        private static List<Vector2> FindPointsBFS(List<Vector2> points, int minIndex, int neighbourCount, int[][] closestArrays)
        {
            var newPointList = new List<Vector2>();

            newPointList.Add(points[minIndex]);
            var onlyBiggerU = points.Count / 10;
            var visited = new bool[points.Count];
            var queue = new Queue<int>();
            queue.Enqueue(minIndex);
            visited[minIndex] = true;
            while (queue.Count != 0)
            {
                var added = 0;
                var v = queue.Dequeue();
                if (newPointList.Count < onlyBiggerU)
                {
                    for (int i = 0; i < neighbourCount; i++)
                    {
                        var neighbour = closestArrays[v][i];
                        if (visited[neighbour])
                            continue;

                        var myPoint = points[v];
                        var neighbourPoint = points[neighbour];
                        if (neighbourPoint.Y > myPoint.Y)
                        {
                            visited[neighbour] = true;
                            queue.Enqueue(neighbour);
                            newPointList.Add(points[neighbour]);
                        }
                    }
                }

                if (added == 0)
                {
                    for (int i = 0; i < neighbourCount; i++)
                    {
                        var neighbour = closestArrays[v][i];
                        if (visited[neighbour])
                            continue;

                        visited[neighbour] = true;
                        queue.Enqueue(neighbour);
                        newPointList.Add(points[neighbour]);
                    }
                }
            }

            return newPointList;
        }

        private static int GetStartingIndex(List<Vector2> points)
        {
            int minIndex = 0;
            float minV = float.MaxValue;
            for (var i = 0; i < points.Count; i++)
                if (points[i].Y < minV)
                {
                    minIndex = i;
                    minV = points[i].Y;
                }

            return minIndex;
        }

        private static int[][] GetClosestArrays(List<Vector2> points)
        {
            var closestArrays = new int[points.Count][];
            var dists = new float[points.Count][];

            for (var i = 0; i < points.Count; i++)
                dists[i] = new float[points.Count];

            for (var i = 0; i < points.Count; i++)
                for (var j = i + 1; j < points.Count; j++)
                {
                    var xDiff = Math.Abs(points[i].X - points[j].X);
                    var yDiff = Math.Abs(points[i].Y - points[j].Y);

                    dists[i][j] = dists[j][i] = xDiff > yDiff ? xDiff : yDiff;
                }

            for (var i = 0; i < points.Count; i++)
            {
                closestArrays[i] = new int[points.Count];
                for (int j = 0; j < points.Count; j++)
                    closestArrays[i][j] = j;
                Array.Sort(dists[i], closestArrays[i]);
            }

            return closestArrays;
        }

        private static List<Vector2> GetPointsFromTriangles(List<IntersectionRange> ranges)
        {
            var pointsSet = new HashSet<Vector2>();

            foreach (var range in ranges)
            {
                pointsSet.Add(new Vector2(range.AMinU, range.AMinV));
                pointsSet.Add(new Vector2(range.AMinU, range.AMaxV));
                pointsSet.Add(new Vector2(range.AMaxU, range.AMinV));
                pointsSet.Add(new Vector2(range.AMaxU, range.AMaxV));
            }

            var points = pointsSet.ToList();
            return points;
        }

        public List<Vector4> GetPoints(out List<Vector2> parameterValues)
        {
            var minimumDifference = MinimumDifference;
            parameterValues = new List<Vector2>();

            GetIntersections(minimumDifference, out List<IntersectionRange> ranges, out var intersectionPoints);

            if (ranges.Count == 0)
                return new List<Vector4>();

            var closestIndex = -1;
            if (minimumDifference > 0)
                closestIndex = SelectBestSelfIntersection(intersectionPoints, ranges, closestIndex);
            else
                closestIndex = SelectClosestToCursor(intersectionPoints, closestIndex);

            var range = ranges[closestIndex];

            var intersection = new Vector4(range.AMinU, range.AMinV, range.BMinU, range.BMinV);

            var intersections = new List<Vector4>();

            var list = new List<Vector4>();
            int firstPointsCount = 7;

            var standardOk = TryStandardFirstPoints(minimumDifference, firstPointsCount, list, intersections, ranges, ref intersection);

            if (!standardOk)
                return TriangulationPoints(ref parameterValues, ranges);

            var expectedSuccedingDifference = Division / 4;
            var midIndex = AddRestOfPoints(list, expectedSuccedingDifference, intersection, minimumDifference, 1, intersections);

            parameterValues = intersections.Select(x => new Vector2(x.X, x.Y)).ToList();
            intersection = intersections.First();

            if ((list.Last() - list.First()).Length() > Division)
            {
                if (GetFirstPoints(A, B, Division, minimumDifference, firstPointsCount, list, -1, intersections,
                    ref intersection))
                {
                    parameterValues = intersections.Select(x => new Vector2(x.X, x.Y)).ToList();
                    return list;
                }

                AddRestOfPoints(list, expectedSuccedingDifference, intersection, minimumDifference, -1, intersections);
                parameterValues = intersections.Select(x => new Vector2(x.X, x.Y)).ToList();

                ReorderList(list, midIndex, parameterValues);
            }

            if (minimumDifference > 0)
                if (list.Any(x => Math.Abs(x.X - x.Z) < minimumDifference && Math.Abs(x.Y - x.W) < minimumDifference))
                {
                    parameterValues = new List<Vector2>();
                    return new List<Vector4>();
                }

            return list;
        }

        private List<Vector4> TriangulationPoints(ref List<Vector2> parameterValues, List<IntersectionRange> ranges)
        {
            if (A != B)
            {
                parameterValues = ComputeApproximatingPoints(ranges, A, out var drawingPoints);
                return drawingPoints;
            }
            else
                return new List<Vector4>();
        }

        private bool TryStandardFirstPoints(float minimumDifference, int firstPointsCount, List<Vector4> list, List<Vector4> intersections,
            List<IntersectionRange> ranges, ref Vector4 intersection)
        {
            IntersectionRange range;
            var failed = true;
            int tryCounts = 10;
            if (GetFirstPoints(A, B, Division, minimumDifference, firstPointsCount, list, 1, intersections,
                ref intersection))
            {
                var rand = new Random(firstPointsCount);
                for (int i = 0; i < tryCounts; i++)
                {
                    var ind = rand.Next(ranges.Count);
                    range = ranges[ind];
                    intersection = new Vector4(range.AMinU, range.AMinV, range.BMinU, range.BMinV);

                    if (!GetFirstPoints(A, B, Division, minimumDifference, firstPointsCount, list, 1,
                        intersections, ref intersection))
                    {
                        failed = false;
                        break;
                    }
                }
            }
            else
                failed = false;

            return !failed;
        }

        private int AddRestOfPoints(List<Vector4> list, float expectedSuccedingDifference, Vector4 intersection, float minimumDifference,
            int direction, List<Vector4> intersections)
        {
            int midIndex = 0;

            while ((list.Last() - list.First()).Length() > Division / 2 &&
                   (list.Last() - list[list.Count - 2]).Length() > expectedSuccedingDifference && list.Count < 10000)
            {
                try
                {
                    intersection = GetIntersectionPoint(A, B, Division, intersection, minimumDifference, direction);
                }
                catch
                {
                    break;
                }

                var worldSpacePoint = A.GetWorldPoint(intersection.X, intersection.Y);
                intersections.Add(intersection);
                list.Add(worldSpacePoint);
                midIndex = list.Count;
            }

            return midIndex;
        }

        private static void ReorderList(List<Vector4> list, int midIndex, List<Vector2> parameterValues)
        {
            var copy = list.ToList();
            var parameterCopy = parameterValues.ToList();
            for (int i = copy.Count - 1; i >= midIndex; i--)
            {
                list[copy.Count - 1 - i] = copy[i];
                parameterValues[copy.Count - 1 - i] = parameterCopy[i];
            }

            for (int i = 0; i < midIndex; i++)
            {
                list[i + copy.Count - midIndex] = copy[i];
                parameterValues[i + copy.Count - midIndex] = parameterCopy[i];
            }
        }

        private static bool GetFirstPoints(IIntersecting a, IIntersecting b, float division, float minimumDifference,
            int firstPointsCount, List<Vector4> list, float direction, List<Vector4> intersections, ref Vector4 intersection)
        {
            try
            {
                for (int i = 0; i < firstPointsCount; i++)
                {
                    intersection = GetIntersectionPoint(a, b, division, intersection, minimumDifference, direction);
                    var worldSpacePoint = a.GetWorldPoint(intersection.X, intersection.Y);
                    intersections.Add(intersection);
                    list.Add(worldSpacePoint);
                }

                return false;
            }
            catch { }
            return true;
        }

        public static Vector4 GetIntersectionPoint(IIntersecting a, IIntersecting b, float division, Vector4 prevIntersection, float minimumDifference, float direction)
        {
            var normal = a.GetNormalizedWorldNormal(prevIntersection.X, prevIntersection.Y);
            var otherNormal = b.GetNormalizedWorldNormal(prevIntersection.Z, prevIntersection.W);

            var worldSpacePoint = a.GetWorldPoint(prevIntersection.X, prevIntersection.Y);

            var surfacePoint = new Vector3(worldSpacePoint.X, worldSpacePoint.Y, worldSpacePoint.Z) +
                             direction * division * Vector3.Cross(normal, otherNormal);

            Func<float, float, Vector4> f1 = a.GetWorldPoint;

            Func<float, float, Vector4> f2 = b.GetWorldPoint;

            Func<float, float, Vector4> f3 = (u, v) => new Vector4(surfacePoint + u * normal + v * otherNormal, 1);

            var system = new EquationSystem(f1, f2, f3, minimumDifference != 0);
            var startPoints = new float[]
                {prevIntersection.X, prevIntersection.Y, prevIntersection.Z, prevIntersection.W, 0, 0};

            NewtonRaphsonEquationSolver.Solve(system, startPoints, out var solutions, 10, division * division, out int iterationsUsed);

            return new Vector4(solutions[0], solutions[1], solutions[2], solutions[3]);
        }


        public static bool CheckIntersection(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 edgeStart, Vector3 edgeDirection)
        {
            var normal = Vector3.Cross(p1 - p2, p3 - p2);

            var d = Vector3.Dot(p1 - edgeStart, normal) / Vector3.Dot(edgeDirection, normal);
            if (float.IsNaN(d) || d < 0 || d > 1)
                return false;

            var newPoint = d * edgeDirection + edgeStart;

            var v0 = p2 - p1;
            var v1 = p3 - p1;
            var v2 = newPoint - p1;
            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;
            var v = (d11 * d20 - d01 * d21) / denom;
            var w = (d00 * d21 - d01 * d20) / denom;
            var u = 1.0f - v - w;

            if (v >= 0 && v <= 1 && u >= 0 && u <= 1 && w >= 0 && w <= 1)
                return true;

            return false;
        }

        private int SelectClosestToCursor(List<Vector4> intersectionPoints, int closestIndex)
        {
            var point = cursor;

            var closestDist = float.MaxValue;
            for (int i = 0; i < intersectionPoints.Count; i++)
            {
                var dist = (intersectionPoints[i] - point).Length();
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private static int SelectBestSelfIntersection(List<Vector4> intersectionPoints, List<IntersectionRange> ranges, int closestIndex)
        {
            float maxDiff = 0;
            for (int i = 0; i < intersectionPoints.Count; i++)
            {
                var paramRange = ranges[i];
                var diff = Math.Abs(paramRange.AMinU - paramRange.BMinU) +
                           Math.Abs(paramRange.AMinV - paramRange.BMinV);

                if (diff > maxDiff)
                {
                    maxDiff = diff;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private void GetIntersections(float minimumDifference, out List<IntersectionRange> ranges, out List<Vector4> intersectionPoints)
        {
            var vertices = new List<Vector3>();
            var indices = new List<TriangleIndices>();
            var parameters = new List<Vector4>();

            A.GetTriangles(TriangleCount, TriangleCount, parameters, indices, vertices);

            var verticesB = new List<Vector3>();
            var indicesB = new List<TriangleIndices>();
            var parametersB = new List<Vector4>();

            B.GetTriangles(TriangleCount, TriangleCount, parametersB, indicesB, verticesB);

            var simpleMesh = new SimpleMesh(indices, vertices, parameters, true, 2 * (float)Math.PI);
            var meshB = new SimpleMesh(indicesB, verticesB, parametersB, true, 2 * (float)Math.PI);
            var tree = new KDTree(simpleMesh, meshB, true);

            ranges = new List<IntersectionRange>();
            intersectionPoints = tree.GetIntersectionPoints(ranges, minimumDifference);
        }

        public bool DrawLines { get; }

        public bool Selected { get; set; }
        public void AddPoint(DrawablePoint point)
        {
        }

        public Matrix4x4 GetModelMatrix()
        {
            return Matrix4x4.Identity;
        }

        public Tuple<Line[], Vector4[]> GetLines()
        {
            var points = GetPoints(out var parameters).ToArray();
            if (points.Length == 0)
                return Tuple.Create(new Line[0], points);

            var addLast = (points[0] - points[points.Length - 1]).Length() < 2 * Division;
            var lines = new Line[points.Length - (addLast ? 0 : 1)];

            for (int i = 0; i < lines.Length; i++)
                lines[i] = new Line(i, i + 1);

            if (addLast)
                lines[lines.Length - 1] = new Line(0, lines.Length - 1);

            return Tuple.Create(lines, points);
        }
    }
}

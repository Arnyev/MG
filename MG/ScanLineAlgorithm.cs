using System;
using System.Collections.Generic;
using System.Drawing;

namespace MG
{
    class ScanLineAlgorithm
    {
        public static void FillPolygon(bool[,] bitmap, Point[] points)
        {
            if (points.Length < 3)
                return;

            GetIndexesArray(out int[] indexes, points);
            CreateEdgeDataArrays(out EdgeData[] EdgeDatasFrom, out EdgeData[] EdgeDatasTo, points);

            var AET = new List<EdgeData>();

            int yMin = points[indexes[0]].Y;
            int yMax = points[indexes[points.Length - 1]].Y;

            var lastActiveIndex = 0;

            for (int y = yMin + 1; y < yMax; y++)
            {
                for (int i = lastActiveIndex; i < points.Length; i++)
                {
                    int pointY = points[indexes[i]].Y;
                    if (pointY >= y)
                    {
                        lastActiveIndex = i;
                        break;
                    }

                    if (pointY == y - 1)
                        UpdateActiveLines(indexes[i], y, EdgeDatasTo, EdgeDatasFrom, AET);
                }

                FillScanLine(AET, bitmap, y);
            }
        }

        private static void GetIndexesArray(out int[] indexes, Point[] points)
        {
            indexes = new int[points.Length];
            for (int i = 0; i < points.Length; i++)
                indexes[i] = i;

            var pointsCopy = (Point[]) points.Clone();

            Array.Sort(pointsCopy, indexes, new PointYComparer());
        }

        private static void UpdateActiveLines(int index, int y, EdgeData[] EdgeDatasTo, EdgeData[] EdgeDatasFrom,
            List<EdgeData> AET)
        {
            if (EdgeDatasTo[index].YMax > y - 1)
                AET.Add(EdgeDatasTo[index]);
            else
                AET.Remove(EdgeDatasTo[index]);

            if (EdgeDatasFrom[index].YMax > y - 1)
                AET.Add(EdgeDatasFrom[index]);
            else
                AET.Remove(EdgeDatasFrom[index]);
        }

        private static void CreateEdgeDataArrays(out EdgeData[] EdgeDatasFrom, out EdgeData[] EdgeDatasTo,
            Point[] points)
        {
            int n = points.Length;
            EdgeDatasFrom = new EdgeData[n];
            EdgeDatasTo = new EdgeData[n];

            for (int i = 0; i < n; i++)
            {
                int nextIndex = (i + 1) % n;
                if (points[i].Y < points[nextIndex].Y)
                {
                    double inverseM = (double) (points[nextIndex].X - points[i].X) /
                                      (points[nextIndex].Y - points[i].Y);
                    EdgeDatasTo[nextIndex] =
                        EdgeDatasFrom[i] = new EdgeData(points[i].Y, points[nextIndex].Y, inverseM, points[i].X);
                }
                else
                {
                    double inverseM = (double) (points[i].X - points[nextIndex].X) /
                                      (points[i].Y - points[nextIndex].Y);
                    EdgeDatasTo[nextIndex] =
                        EdgeDatasFrom[i] =
                            new EdgeData(points[nextIndex].Y, points[i].Y, inverseM, points[nextIndex].X);
                }
            }
        }

        private static void FillScanLine(List<EdgeData> AET, bool[,] bitmap, int y)
        {
            var intersections = new int[AET.Count];
            for (int j = 0; j < AET.Count; j++)
            {
                AET[j].X += +AET[j].InverseM;
                intersections[j] = (int) AET[j].X;
            }

            Array.Sort(intersections);
            for (int i = 0; i < intersections.Length; i += 2)
            {
                var startPoint = intersections[i] > 0 ? intersections[i] : 0;
                var endPoint = intersections[(i + 1) % intersections.Length] < bitmap.GetLength(1)
                    ? intersections[(i + 1) % intersections.Length]
                    : bitmap.GetLength(1) - 1;

                for (int j = startPoint; j < endPoint; j++)
                    bitmap[j, y] = false;
            }
        }

        public class EdgeData
        {
            public readonly int YMin;
            public readonly int YMax;
            public readonly double InverseM;
            public double X;

            public EdgeData(int yMin, int yMax, double inverseM, double x)
            {
                YMin = yMin;
                YMax = yMax;
                InverseM = inverseM;
                X = x;
            }
        }

        public class PointXComparer : IComparer<Point>
        {
            public int Compare(Point p1, Point p2)
            {
                return p1.X.CompareTo(p2.X);
            }
        }

        public class PointYComparer : IComparer<Point>
        {
            public int Compare(Point p1, Point p2)
            {
                return p1.Y.CompareTo(p2.Y);
            }
        }
    }
}

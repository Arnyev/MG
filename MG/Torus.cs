using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace MG
{
    public interface IIntersecting
    {
        void GetTriangles(int divisionsU, int divisionsV, List<TriangleParameters> parameterValues, List<TriangleIndices> indices, List<Vector3> points);
        Vector4 GetWorldPoint(float u, float v);
        Vector3 GetNormalizedWorldNormal(float u, float v);
        bool Selected { get; }
        void Trim(List<Vector2> parameters);
        void DrawCurve(List<Vector2> parameters, DirectBitmap bitmap);
        void InverseTrimming();
    }

    public class Torus : IDrawableObject, IIntersecting
    {
        private static int _torusNumber = 1;
        public float TubeRadius { get; set; } = 2;
        public float TorusRadius { get; set; } = 10;
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float Alpha { get; set; }
        public float Beta { get; set; }
        public float Gamma { get; set; }

        public string Name { get; set; } = "Torus " + _torusNumber++;
        public int PointCountH { get; set; } = 30;
        public int PointCountV { get; set; } = 30;
        private const int ParameterRangePrecision = 1000;
        public bool Trimmed { get; set; }
        private readonly bool[,] ParameterRange;

        public Torus()
        {
            ParameterRange = new bool[ParameterRangePrecision + 1, ParameterRangePrecision + 1];
            for (int i = 0; i < ParameterRangePrecision + 1; i++)
                for (int j = 0; j < ParameterRangePrecision + 1; j++)
                    ParameterRange[i, j] = true;
        }


        public Matrix4x4 GetModelMatrix()
        {
            var rotate = GetXRotationMatrix(Alpha) * GetYRotationMatrix(Beta) * GetZRotationMatrix(Gamma);

            rotate.M41 = PositionX;
            rotate.M42 = PositionY;
            rotate.M43 = PositionZ;
            return rotate;
        }

        private static Matrix4x4 GetXRotationMatrix(float alpha)
        {
            var result = Matrix4x4.Identity;
            var sina = (float)Math.Sin(alpha);
            var cosa = (float)Math.Cos(alpha);

            result.M22 = cosa;
            result.M23 = sina;
            result.M32 = -sina;
            result.M33 = cosa;

            return result;
        }

        private static Matrix4x4 GetYRotationMatrix(float alpha)
        {
            var result = Matrix4x4.Identity;
            var sina = (float)Math.Sin(alpha);
            var cosa = (float)Math.Cos(alpha);

            result.M11 = cosa;
            result.M13 = -sina;
            result.M31 = sina;
            result.M33 = cosa;

            return result;
        }

        private static Matrix4x4 GetZRotationMatrix(float alpha)
        {
            var result = Matrix4x4.Identity;
            var sina = (float)Math.Sin(alpha);
            var cosa = (float)Math.Cos(alpha);

            result.M11 = cosa;
            result.M12 = sina;
            result.M21 = -sina;
            result.M22 = cosa;

            return result;
        }

        public Vector4 GetNormal(Vector4 point)
        {
            var mult = 2 - 2 * TorusRadius / (float)Math.Sqrt(point.X * point.X + point.Y * point.Y);
            return new Vector4(point.X * mult, point.Y * mult, 2 * point.Z, 0);
        }

        public Vector4 GetNormal(float alpha, float beta) =>
            GetNormal(new Vector4(GetPoint(alpha, beta, TubeRadius, TorusRadius), 1));

        public Tuple<Line[], Vector4[]> GetLines()
        {
            var points2Dim = GetPoints();
            var lines = new Line[PointCountH * PointCountV * 2];
            var points = new Vector4[PointCountH * PointCountV];
            var multInd = (int)(ParameterRangePrecision / (Math.PI * 2));

            var ind = 0;

            var diffa = 2 * (float)Math.PI / points2Dim.GetLength(0);
            var diffb = 2 * (float)Math.PI / points2Dim.GetLength(1);

            for (int i = 0; i < points2Dim.GetLength(0); i++)
                for (int j = 0; j < points2Dim.GetLength(1); j++)
                {
                    points[ind] = new Vector4(points2Dim[i, j], 1);
                    var nextIndY = ind;
                    var nextIndX = ind;

                    if (i != PointCountH - 1)
                        nextIndX += PointCountV;
                    else
                        nextIndX -= PointCountV * (PointCountH - 1);

                    if (j != PointCountV - 1)
                        nextIndY += 1;
                    else
                        nextIndY -= PointCountV - 1;

                    var alpha = i * diffa;
                    var beta = j * diffb;

                    var indX = (int)(alpha * multInd);
                    var indY = (int)(beta * multInd);
                    if (!Trimmed || ParameterRange[indX, indY])
                    {

                        lines[2 * ind] = new Line(ind, nextIndX);
                        lines[2 * ind + 1] = new Line(ind, nextIndY);
                    }

                    ind++;
                }

            return Tuple.Create(lines, points);
        }



        public BoundingBox[] GetBoxes(int divisionsU, int divisionsV)
        {
            var angleRange = new ParameterRange(0, 2 * (float)Math.PI, 0, 2 * (float)Math.PI);

            GetBoundingBoxes(divisionsU, divisionsV, angleRange, out var triangleIndices, out var triangleVertices, out var angleRanges);

            var boundingBoxes = new BoundingBox[divisionsU * divisionsV];

            for (int i = 0; i < triangleIndices.Length; i++)
                boundingBoxes[i] = new BoundingBox(triangleIndices[i], triangleVertices[i], angleRanges[i]);

            return boundingBoxes;
        }

        public void GetTriangles(int divisionsU, int divisionsV, List<TriangleParameters> parameterValues, List<TriangleIndices> indices, List<Vector3> points)
        {
            var modelMatrix = GetModelMatrix();

            var pointCount = divisionsU * divisionsV;
            points.Capacity = pointCount;
            indices.Capacity = 2 * pointCount;
            parameterValues.Capacity = pointCount;

            var torusRadius = TorusRadius;
            var tubeRadius = TubeRadius;

            var diffa = 2 * (float)Math.PI / divisionsU;
            var diffb = 2 * (float)Math.PI / divisionsV;

            for (int x = 0; x < divisionsU; x++)
            {
                var alpha = x * diffa;
                var sina = Math.Sin(alpha);
                var cosa = Math.Cos(alpha);
                var zp = (float)(tubeRadius * sina);
                var mult = torusRadius + tubeRadius * cosa;

                for (int y = 0; y < divisionsV; y++)
                {
                    var beta = y * diffb;
                    var cosb = Math.Cos(beta);
                    var sinb = Math.Sin(beta);

                    var xp = (float)(mult * cosb);
                    var yp = (float)(mult * sinb);

                    points.Add(Vector3.Transform(new Vector3(xp, yp, zp), modelMatrix));

                    var i = x * divisionsV + y;

                    var indRight = (i + 1) % pointCount;
                    var indUp = (i + divisionsV) % pointCount;
                    var indCross = (i + 1 + divisionsV) % pointCount;

                    parameterValues.Add(new TriangleParameters(
                        new Vector2(alpha, beta),
                        new Vector2(alpha, beta + diffb),
                        new Vector2(alpha + diffa, beta)));

                    indices.Add(new TriangleIndices(i, indRight, indUp));

                    parameterValues.Add(new TriangleParameters(
                        new Vector2(alpha, beta + diffb),
                        new Vector2(alpha + diffa, beta + diffb),
                        new Vector2(alpha + diffa, beta)));

                    indices.Add(new TriangleIndices(indRight, indCross, indUp));
                }
            }
        }

        private void GetBoundingBoxes(int divisionsU, int divisionsV, ParameterRange parameterRange, out List<TriangleIndices>[] triangleIndices,
                out List<Vector3>[] triangleVertices, out ParameterRange[] parameterRanges)
        {
            var angleForMultiplyRadiusU =
                2 * (float)Math.PI / (divisionsU * 2 * (float)Math.PI / (parameterRange.MaxU - parameterRange.MinU));
            var angleForMultiplyRadiusV =
                2 * (float)Math.PI / (divisionsV * 2 * (float)Math.PI / (parameterRange.MaxV - parameterRange.MinV));

            var torusRadius = TorusRadius;
            var torusRadiusDiff = TorusRadius / (float)Math.Cos(angleForMultiplyRadiusU / 2) - TorusRadius;
            var tubeRadius = torusRadiusDiff + TubeRadius / (float)Math.Cos(angleForMultiplyRadiusV / 2);

            triangleIndices = new List<TriangleIndices>[divisionsU * divisionsV];
            triangleVertices = new List<Vector3>[divisionsU * divisionsV];
            parameterRanges = new ParameterRange[divisionsU * divisionsV];

            var modelMatrix = GetModelMatrix();

            for (int i = 0; i < divisionsU; i++)
                for (int j = 0; j < divisionsV; j++)
                {
                    var startAlpha = parameterRange.MinU + angleForMultiplyRadiusU * i;
                    var endAlpha = parameterRange.MinU + angleForMultiplyRadiusU * ((i + 1) % divisionsU);
                    var startBeta = parameterRange.MinV + angleForMultiplyRadiusV * j;
                    var endBeta = parameterRange.MinV + angleForMultiplyRadiusV * ((j + 1) % divisionsV);

                    parameterRanges[i * divisionsV + j] = new ParameterRange(startAlpha, endAlpha, startBeta, endBeta);

                    var points = new List<Vector3>();

                    for (float u = 0; u <= 1.0f; u += 0.5f)
                    {
                        for (float v = 0; v <= 1.0f; v += 0.5f)
                        {
                            var alpha = startAlpha + u * angleForMultiplyRadiusU;
                            var beta = startBeta + v * angleForMultiplyRadiusV;
                            var point = GetPoint(alpha, beta, tubeRadius, torusRadius);
                            points.Add(Vector3.Transform(point, modelMatrix));
                        }
                    }

                    triangleIndices[i * divisionsV + j] = new QuickHull(points).GetMeshIndices();
                    triangleVertices[i * divisionsV + j] = points;
                }
        }

        public Vector3 GetNormalizedWorldNormal(float alpha, float beta)
        {
            var modelMatrix = GetModelMatrix();
            var normal = GetNormal(alpha, beta);
            var normalWorld = Vector4.Transform(normal, modelMatrix);
            return Vector3.Normalize(new Vector3(normalWorld.X, normalWorld.Y, normalWorld.Z));
        }

        public bool Selected { get; set; }

        public Vector4 GetWorldPoint(float u, float v) => Vector4.Transform(new Vector4(GetPoint(u, v, TubeRadius, TorusRadius), 1), GetModelMatrix());

        private Vector3[,] GetPoints()
        {
            var diffa = (float)Math.PI * 2 / PointCountH;
            var diffb = (float)Math.PI * 2 / PointCountV;
            var ret = new Vector3[PointCountH, PointCountV];
            for (int i = 0; i < ret.GetLength(0); i++)
                for (int j = 0; j < ret.GetLength(1); j++)
                    ret[i, j] = GetPoint(i * diffa, j * diffb, TubeRadius, TorusRadius);

            return ret;
        }

        public static Vector3 GetPoint(float alpha, float beta, float TubeRadius, float TorusRadius)
        {
            var cosa = Math.Cos(alpha);
            var cosb = Math.Cos(beta);
            var sina = Math.Sin(alpha);
            var sinb = Math.Sin(beta);

            var mult = TorusRadius + TubeRadius * cosa;
            var x = (float)(mult * cosb);
            var y = (float)(mult * sinb);
            var z = (float)(TubeRadius * sina);
            return new Vector3(x, y, z);
        }


        public void Trim(List<Vector2> parameters)
        {
            Trimmed = true;
            var m = ParameterRangePrecision;
            var mult = (int)(m / (Math.PI * 2));
            var points = parameters.Select(x => new Point((int)(x.X * mult), (int)(x.Y * mult))).ToList();
            var pointsClamped = points.Select(x => new Point(((x.X % m) + m) % m, ((x.Y % m) + m) % m)).ToList();
            var lines = pointsClamped.Zip(pointsClamped.Skip(1), (x, y) => Tuple.Create(x, y))
                    .Where(x => (x.Item1.X - x.Item2.X) * (x.Item1.X - x.Item2.X) + (x.Item1.Y - x.Item2.Y) * (x.Item1.Y - x.Item2.Y) < 10).ToList();

            lines.ForEach(x => DrawLine(x.Item1, x.Item2));

            var stack = new Stack<Point>();
            var rand = new Random();
            int xi = 0;
            int yi = 0;
            while (!ParameterRange[xi, yi])
            {
                xi = rand.Next(m);
                yi = rand.Next(m);
            }
            stack.Push(new Point(xi, yi));
            ParameterRange[xi, yi] = false;
            int pushed = 0;
            while (stack.Count != 0)
            {
                var p = stack.Pop();
                var right = (p.X + 1) % m;
                var left = (p.X - 1 + m) % m;
                var top = (p.Y + 1) % m;
                var bot = (p.Y - 1 + m) % m;

                pushed += 1;
                if (ParameterRange[right, p.Y])
                {
                    ParameterRange[right, p.Y] = false;
                    stack.Push(new Point(right, p.Y));
                }
                if (ParameterRange[left, p.Y])
                {
                    ParameterRange[left, p.Y] = false;
                    stack.Push(new Point(left, p.Y));
                }
                if (ParameterRange[p.X, top])
                {
                    ParameterRange[p.X, top] = false;
                    stack.Push(new Point(p.X, top));
                }
                if (ParameterRange[p.X, bot])
                {
                    ParameterRange[p.X, bot] = false;
                    stack.Push(new Point(p.X, bot));
                }
            }
        }

        public void DrawLine(Point p1, Point p2)
        {
            var differencePoint = new Point(p2.X - p1.X, p2.Y - p1.Y);
            var octant = DirectBitmap.FindOctant(differencePoint);

            var mappedDifference = DirectBitmap.MapInput(octant, differencePoint.X, differencePoint.Y);

            var dx = mappedDifference.X;
            var dy = mappedDifference.Y;
            var d = 2 * dy - dx;
            var y = 0;

            for (int x = 0; x <= mappedDifference.X; x++)
            {
                var p = DirectBitmap.MapOutput(octant, x, y);
                var newPointX = p.X + p1.X;
                var newPointY = p.Y + p1.Y;

                if (newPointX >= 0 && newPointX < ParameterRangePrecision && newPointY >= 0 && newPointY < ParameterRangePrecision)
                    ParameterRange[newPointX, newPointY] = false;

                if (d > 0)
                {
                    y = y + 1;
                    d = d - 2 * dx;
                }

                d = d + 2 * dy;
            }
        }

        public void DrawCurve(List<Vector2> parameters, DirectBitmap bitmap)
        {
            var m = bitmap.Width;
            var mult = (int)(m / (Math.PI * 2));
            var points = parameters.Select(x => new Point((int)(x.X * mult), (int)(x.Y * mult))).ToList();
            var pointsClamped = points.Select(x => new Point(((x.X % m) + m) % m, ((x.Y % m) + m) % m)).ToList();

            var myColor = new MyColor(255, 255, 255);

            var lines = pointsClamped.Zip(pointsClamped.Skip(1), (x, y) => Tuple.Create(x, y))
                .Where(x => (x.Item1.X - x.Item2.X) * (x.Item1.X - x.Item2.X) + (x.Item1.Y - x.Item2.Y) * (x.Item1.Y - x.Item2.Y) < 30).ToList();

            lines.ForEach(x => bitmap.DrawLine(x.Item1, x.Item2, myColor, false));
        }

        public void InverseTrimming()
        {
            for (int i = 0; i < ParameterRangePrecision + 1; i++)
                for (int j = 0; j < ParameterRangePrecision + 1; j++)
                    ParameterRange[i, j] = !ParameterRange[i, j];
        }
    }

    public struct ParameterRange
    {
        public readonly float MinU;
        public readonly float MaxU;
        public readonly float MinV;
        public readonly float MaxV;

        public ParameterRange(float minU, float maxU, float minV, float maxV)
        {
            MinU = minU;
            MaxU = maxU;
            MinV = minV;
            MaxV = maxV;
        }
    }
}

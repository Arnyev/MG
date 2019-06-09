using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace MG
{
    public interface IIntersecting
    {
        void GetTriangles(int divisionsU, int divisionsV, List<Vector4> parameterValues, List<TriangleIndices> indices,
            List<Vector3> points);

        Vector4 GetWorldPoint(float u, float v);
        Vector3 GetNormalizedWorldNormal(float u, float v);
        bool Selected { get; }
        void Trim(List<Vector2> parameters);
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

            var ind = 0;
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

                    lines[2 * ind] = new Line(ind, nextIndX);
                    lines[2 * ind + 1] = new Line(ind, nextIndY);

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

        public void GetTriangles(int divisionsU, int divisionsV, List<Vector4> parameterValues, List<TriangleIndices> indices, List<Vector3> points)
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

                    parameterValues.Add(new Vector4(alpha, beta, alpha + diffa, beta + diffb));

                    points.Add(Vector3.Transform(new Vector3(xp, yp, zp), modelMatrix));

                    var i = x * divisionsV + y;

                    var indRight = (i + 1) % pointCount;
                    var indUp = (i + divisionsV) % pointCount;
                    var indCross = (i + 1 + divisionsV) % pointCount;

                    indices.Add(new TriangleIndices(i, indRight, indUp, parameterValues.Count - 1));
                    indices.Add(new TriangleIndices(indRight, indCross, indUp, parameterValues.Count - 1));
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

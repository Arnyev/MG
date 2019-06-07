using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MG
{
    class Torus : IDrawableObject
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

        private Vector3[,] GetPoints()
        {
            var diffa = Math.PI * 2 / PointCountH;
            var diffb = Math.PI * 2 / PointCountV;
            var ret = new Vector3[PointCountH, PointCountV];
            for (int i = 0; i < ret.GetLength(0); i++)
                for (int j = 0; j < ret.GetLength(1); j++)
                    ret[i, j] = GetPoint(i * diffa, j * diffb);

            return ret;
        }

        public List<Tuple<Vector3, Vector3>> GetBoundingBox()
        {
            var pointsMine = new List<Vector3>
            {
                new Vector3(-TorusRadius - TubeRadius, -TorusRadius - TubeRadius, -TubeRadius), // - - -
                new Vector3(-TorusRadius - TubeRadius, -TorusRadius - TubeRadius, TubeRadius),  // - - +
                new Vector3(-TorusRadius - TubeRadius, TorusRadius + TubeRadius, -TubeRadius),  // - + -
                new Vector3(-TorusRadius - TubeRadius, TorusRadius + TubeRadius, TubeRadius),   // - + +
                new Vector3(TorusRadius + TubeRadius, -TorusRadius - TubeRadius, -TubeRadius),  // + - -
                new Vector3(TorusRadius + TubeRadius, -TorusRadius - TubeRadius, TubeRadius),   // + = +
                new Vector3(TorusRadius + TubeRadius, TorusRadius + TubeRadius, -TubeRadius),   // + + -
                new Vector3(TorusRadius + TubeRadius, TorusRadius + TubeRadius, TubeRadius),    // + + +
            };

            var myMatrix = GetModelMatrix();
            pointsMine = pointsMine.Select(x => Vector3.Transform(x, myMatrix)).ToList();

            var result = new List<Tuple<Vector3, Vector3>>
            {
                Tuple.Create(pointsMine[0], pointsMine[1]),
                Tuple.Create(pointsMine[0], pointsMine[2]),
                Tuple.Create(pointsMine[0], pointsMine[4]),
                Tuple.Create(pointsMine[1], pointsMine[3]),
                Tuple.Create(pointsMine[1], pointsMine[5]),
                Tuple.Create(pointsMine[2], pointsMine[3]),
                Tuple.Create(pointsMine[2], pointsMine[6]),
                Tuple.Create(pointsMine[3], pointsMine[7]),
                Tuple.Create(pointsMine[4], pointsMine[5]),
                Tuple.Create(pointsMine[4], pointsMine[6]),
                Tuple.Create(pointsMine[5], pointsMine[7]),
                Tuple.Create(pointsMine[6], pointsMine[7]),
            };

            return result;
        }

        public List<Tuple<Vector3, Vector3, Vector3>> GetBoxFaces()
        {
            var pointsMine = new List<Vector3>
            {
                new Vector3(-TorusRadius - TubeRadius, -TorusRadius - TubeRadius, -TubeRadius), // - - -
                new Vector3(-TorusRadius - TubeRadius, -TorusRadius - TubeRadius, TubeRadius),  // - - +
                new Vector3(-TorusRadius - TubeRadius, TorusRadius + TubeRadius, -TubeRadius),  // - + -
                new Vector3(-TorusRadius - TubeRadius, TorusRadius + TubeRadius, TubeRadius),   // - + +
                new Vector3(TorusRadius + TubeRadius, -TorusRadius - TubeRadius, -TubeRadius),  // + - -
                new Vector3(TorusRadius + TubeRadius, -TorusRadius - TubeRadius, TubeRadius),   // + - +
                new Vector3(TorusRadius + TubeRadius, TorusRadius + TubeRadius, -TubeRadius),   // + + -
                new Vector3(TorusRadius + TubeRadius, TorusRadius + TubeRadius, TubeRadius),    // + + +
            };

            var myMatrix = GetModelMatrix();
            pointsMine = pointsMine.Select(x => Vector3.Transform(x, myMatrix)).ToList();

            var result = new List<Tuple<Vector3, Vector3, Vector3>>
            {
                Tuple.Create(pointsMine[0], pointsMine[1], pointsMine[3]),  // -x
                Tuple.Create(pointsMine[0], pointsMine[1], pointsMine[5]),  // -y
                Tuple.Create(pointsMine[0], pointsMine[4], pointsMine[6]),  // -z
                Tuple.Create(pointsMine[1], pointsMine[3], pointsMine[7]),  // +z
                Tuple.Create(pointsMine[2], pointsMine[3], pointsMine[7]),  // + y
                Tuple.Create(pointsMine[4], pointsMine[6], pointsMine[7]),  // + x 
            };

            return result;
        }

        public List<Vector4> Intersection(Torus other)
        {
            var myEdges = GetBoundingBox();
            var otherEdges = other.GetBoundingBox();

            var faces = GetBoxFaces();
            var otherFaces = other.GetBoxFaces();

            var intersecting = CheckEdgeSquareIntersections(myEdges, otherFaces);
            if (!intersecting)
                intersecting = CheckEdgeSquareIntersections(otherEdges, faces);

            return null;
        }

        public static bool CheckEdgeTriangleIntersections(List<Tuple<Vector3, Vector3>> edges, List<Tuple<Vector3, Vector3, Vector3>> triangles)
        {
            bool intersecting = false;
            foreach (var edge in edges)
            {
                foreach (var face in triangles)
                {
                    var p1 = face.Item1;
                    var p2 = face.Item2;
                    var p3 = face.Item3;
                    var normal = Vector3.Cross(p1 - p2, p3 - p2);
                    var l = edge.Item2 - edge.Item1;
                    var l0 = edge.Item1;

                    var d = Vector3.Dot(p1 - l0, normal) / Vector3.Dot(l, normal);
                    if (float.IsNaN(d) || d < 0 || d > 1)
                        continue;

                    var newPoint = d * l + l0;

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
                    {
                        intersecting = true;
                        break;
                    }
                }

                if (intersecting)
                    break;
            }

            return intersecting;
        }

        public static bool CheckEdgeSquareIntersections(List<Tuple<Vector3, Vector3>> edges, List<Tuple<Vector3, Vector3, Vector3>> squares)
        {
            bool intersecting = false;
            foreach (var edge in edges)
            {
                foreach (var face in squares)
                {
                    var p1 = face.Item1;
                    var p2 = face.Item2;
                    var p3 = face.Item3;
                    var normal = Vector3.Cross(p1 - p2, p3 - p2);
                    var l = edge.Item2 - edge.Item1;
                    var l0 = edge.Item1;

                    var d = Vector3.Dot(p1 - l0, normal) / Vector3.Dot(l, normal);
                    if (float.IsNaN(d) || d < 0 || d > 1)
                        continue;

                    var newPoint = d * l + l0;
                    var dir1 = p1 - p2;
                    var dir2 = p3 - p2;

                    var v1 = newPoint - p2;
                    var d1 = Vector3.Dot(v1, dir1) / dir1.LengthSquared();
                    var d2 = Vector3.Dot(v1, dir2) / dir2.LengthSquared();

                    if (d1 >= 0 && d1 <= 1 && d2 >= 0 && d2 <= 1)
                    {
                        intersecting = true;
                        break;
                    }
                }

                if (intersecting)
                    break;
            }

            return intersecting;
        }

        private Vector3 GetPoint(double alpha, double beta)
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
    }
}

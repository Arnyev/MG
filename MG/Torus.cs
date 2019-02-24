using System;
using System.Numerics;

namespace MG
{
    class Torus : IDrawableObject
    {
        private static int _torusNumber = 1;
        public double TubeRadius { get; set; } = 2;
        public double TorusRadius { get; set; } = 10;
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float Alpha { get; set; }
        public float Beta { get; set; }
        public string Name { get; set; } = "Torus " + _torusNumber++;
        public int PointCountH { get; set; } = 100;
        public int PointCountV { get; set; } = 100;

        public Matrix4x4 GetModelMatrix()
        {
            var rotate = Matrix4x4.CreateFromYawPitchRoll(Alpha, Beta, 0.0f);
            var translation = Matrix4x4.CreateTranslation(PositionX, PositionY, PositionZ);
            return rotate * translation;
            //return Matrix4x4.Identity;
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

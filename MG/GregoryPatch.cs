using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MG
{
    public class GregoryPatch : IDrawableObject, ICurve
    {
        private readonly Func<float, Vector4> _valueFuncU0;
        private readonly Func<float, Vector4> _valueFuncU1;
        private readonly Func<float, Vector4> _valueFuncV0;
        private readonly Func<float, Vector4> _valueFuncV1;

        private readonly Func<float, Vector4> _derivativeFuncU0;
        private readonly Func<float, Vector4> _derivativeFuncU1;
        private readonly Func<float, Vector4> _derivativeFuncV0;
        private readonly Func<float, Vector4> _derivativeFuncV1;

        private readonly Func<Vector4> _duv00;
        private readonly Func<Vector4> _dvu00;

        private readonly Func<Vector4> _duv01;
        private readonly Func<Vector4> _dvu01;

        private readonly Func<Vector4> _duv10;
        private readonly Func<Vector4> _dvu10;

        private readonly Func<Vector4> _duv11;
        private readonly Func<Vector4> _dvu11;

        public GregoryPatch(Func<float, Vector4> valueFuncU0, Func<float, Vector4> valueFuncU1,
            Func<float, Vector4> valueFuncV0, Func<float, Vector4> valueFuncV1, Func<float, Vector4> derivativeFuncU0,
            Func<float, Vector4> derivativeFuncU1, Func<float, Vector4> derivativeFuncV0,
            Func<float, Vector4> derivativeFuncV1, Func<Vector4> duv00, Func<Vector4> dvu00, Func<Vector4> duv01,
            Func<Vector4> dvu01, Func<Vector4> duv10, Func<Vector4> dvu10, Func<Vector4> duv11, Func<Vector4> dvu11)
        {
            _valueFuncU0 = valueFuncU0;
            _valueFuncU1 = valueFuncU1;
            _valueFuncV0 = valueFuncV0;
            _valueFuncV1 = valueFuncV1;
            _derivativeFuncU0 = derivativeFuncU0;
            _derivativeFuncU1 = derivativeFuncU1;
            _derivativeFuncV0 = derivativeFuncV0;
            _derivativeFuncV1 = derivativeFuncV1;
            _duv00 = duv00;
            _dvu00 = dvu00;
            _duv01 = duv01;
            _dvu01 = dvu01;
            _duv10 = duv10;
            _dvu10 = dvu10;
            _duv11 = duv11;
            _dvu11 = dvu11;
        }

        private Matrix4x4 ComputeMatrixX()
        {
            var m = new Matrix4x4
            {
                M11 = _valueFuncU0(0).X,
                M14 = _valueFuncU0(1).X,
                M41 = _valueFuncU1(0).X,
                M44 = _valueFuncU1(1).X,

                M12 = _derivativeFuncV0(0).X,
                M13 = _derivativeFuncV1(0).X,
                M42 = _derivativeFuncV0(1).X,
                M43 = _derivativeFuncV1(1).X,

                M21 = _derivativeFuncU0(0).X,
                M31 = _derivativeFuncU1(0).X,
                M24 = _derivativeFuncU0(1).X,
                M34 = _derivativeFuncU1(1).X
            };

            return m;
        }

        private Matrix4x4 ComputeMatrixY()
        {
            var m = new Matrix4x4
            {
                M11 = _valueFuncU0(0).Y,
                M14 = _valueFuncU0(1).Y,
                M41 = _valueFuncU1(0).Y,
                M44 = _valueFuncU1(1).Y,

                M12 = _derivativeFuncV0(0).Y,
                M13 = _derivativeFuncV1(0).Y,
                M42 = _derivativeFuncV0(1).Y,
                M43 = _derivativeFuncV1(1).Y,

                M21 = _derivativeFuncU0(0).Y,
                M31 = _derivativeFuncU1(0).Y,
                M24 = _derivativeFuncU0(1).Y,
                M34 = _derivativeFuncU1(1).Y,
            };

            return m;
        }

        private Matrix4x4 ComputeMatrixZ()
        {
            var m = new Matrix4x4
            {
                M11 = _valueFuncU0(0).Z,
                M14 = _valueFuncU0(1).Z,
                M41 = _valueFuncU1(0).Z,
                M44 = _valueFuncU1(1).Z,

                M12 = _derivativeFuncV0(0).Z,
                M13 = _derivativeFuncV1(0).Z,
                M42 = _derivativeFuncV0(1).Z,
                M43 = _derivativeFuncV1(1).Z,

                M21 = _derivativeFuncU0(0).Z,
                M31 = _derivativeFuncU1(0).Z,
                M24 = _derivativeFuncU0(1).Z,
                M34 = _derivativeFuncU1(1).Z,
            };

            return m;
        }

        private float Hermite(float t, int number)
        {
            switch (number)
            {
                case 0:
                    return (1 + 2 * t) * (1 - t) * (1 - t);
                case 1:
                    return t * (1 - t) * (1 - t);
                case 2:
                    return t * t * (t - 1);
                case 3:
                    return t * t * (3 - 2 * t);

            }

            return 0;
        }

        public Matrix4x4 GetModelMatrix()
        {
            return Matrix4x4.Identity;
        }

        public Tuple<Line[], Vector4[]> GetLines()
        {
            var p1 = _valueFuncU0(0);
            var p2 = _valueFuncU0(1);
            var p3 = _valueFuncU1(0);
            var p4 = _valueFuncU1(1);

            var points = new[] { p1, p2, p4, p3 };
            var lines = new[] { new Line(0, 1), new Line(1, 2), new Line(2, 3), new Line(3, 0) };
            return Tuple.Create(lines, points);
        }

        public bool DrawLines { get; set; }
        public int DivisionsU { get; set; } = 4;
        public int DivisionsV { get; set; } = 4;

        public List<Vector4> GetPoints(int count)
        {
            count /= 8;
            var mX = ComputeMatrixX();
            var mY = ComputeMatrixY();
            var mZ = ComputeMatrixZ();

            var points = new List<Vector4>();

            var valuesU0 = Enumerable.Range(0, count).Select(x => _valueFuncU0(x * (1.0f / count))).ToArray();
            var valuesU1 = Enumerable.Range(0, count).Select(x => _valueFuncU1(x * (1.0f / count))).ToArray();
            var valuesV0 = Enumerable.Range(0, count).Select(x => _valueFuncV0(x * (1.0f / count))).ToArray();
            var valuesV1 = Enumerable.Range(0, count).Select(x => _valueFuncV1(x * (1.0f / count))).ToArray();

            var derivativesU0 = Enumerable.Range(0, count).Select(x => _derivativeFuncU0(x * (1.0f / count))).ToArray();
            var derivativesU1 = Enumerable.Range(0, count).Select(x => _derivativeFuncU1(x * (1.0f / count))).ToArray();
            var derivativesV0 = Enumerable.Range(0, count).Select(x => _derivativeFuncV0(x * (1.0f / count))).ToArray();
            var derivativesV1 = Enumerable.Range(0, count).Select(x => _derivativeFuncV1(x * (1.0f / count))).ToArray();

            var uvaluesV0 = Enumerable.Range(0, DivisionsU).Select(x => _valueFuncV0(x * (1.0f / (DivisionsU - 1)))).ToArray();
            var uvaluesV1 = Enumerable.Range(0, DivisionsU).Select(x => _valueFuncV1(x * (1.0f / (DivisionsU - 1)))).ToArray();
            var uderivativesV0 = Enumerable.Range(0, DivisionsU).Select(x => _derivativeFuncV0(x * (1.0f / (DivisionsU - 1)))).ToArray();
            var uderivativesV1 = Enumerable.Range(0, DivisionsU).Select(x => _derivativeFuncV1(x * (1.0f / (DivisionsU - 1)))).ToArray();

            var vvaluesU0 = Enumerable.Range(0, DivisionsV).Select(x => _valueFuncU0(x * (1.0f / (DivisionsV - 1)))).ToArray();
            var vvaluesU1 = Enumerable.Range(0, DivisionsV).Select(x => _valueFuncU1(x * (1.0f / (DivisionsV - 1)))).ToArray();
            var vderivativesU0 = Enumerable.Range(0, DivisionsV).Select(x => _derivativeFuncU0(x * (1.0f / (DivisionsV - 1)))).ToArray();
            var vderivativesU1 = Enumerable.Range(0, DivisionsV).Select(x => _derivativeFuncU1(x * (1.0f / (DivisionsV - 1)))).ToArray();

            var duv00 = _duv00();
            var dvu00 = _dvu00();
            var duv01 = _duv01();
            var dvu01 = _dvu01();
            var duv10 = _duv10();
            var dvu10 = _dvu10();
            var duv11 = _duv11();
            var dvu11 = _dvu11();

            for (int j = 0; j < DivisionsU; j++)
            {
                float curveU = j * (1.0f / (DivisionsU - 1));
                var vu0 = Hermite(curveU, 0);
                var vu1 = Hermite(curveU, 1);
                var vu2 = Hermite(curveU, 2);
                var vu3 = Hermite(curveU, 3);

                var vecU = new Vector4(vu0, vu1, vu2, vu3);
                for (int i = 0; i < count; i++)
                {
                    float t = i * (1.0f / count);
                    var vv0 = Hermite(t, 0);
                    var vv1 = Hermite(t, 1);
                    var vv2 = Hermite(t, 2);
                    var vv3 = Hermite(t, 3);

                    var vecV = new Vector4(vv0, vv1, vv2, vv3);

                    UpdateMatrix(curveU, duv00, t, dvu00, duv01, dvu01, duv10, dvu10, duv11, dvu11, ref mX, ref mY, ref mZ);

                    var transform = Vector4.Transform(vecU, mX);
                    var x = Vector4.Dot(transform, vecV);
                    var vector1 = Vector4.Transform(vecU, mY);
                    var y = Vector4.Dot(vector1, vecV);
                    var vector4 = Vector4.Transform(vecU, mZ);
                    var z = Vector4.Dot(vector4, vecV);

                    var hc = vu0 * valuesU0[i] + vu1 * derivativesU0[i] + vu2 * derivativesU1[i] + vu3 * valuesU1[i];
                    var hd = vv0 * uvaluesV0[j] + vv1 * uderivativesV0[j] + vv2 * uderivativesV1[j] + vv3 * uvaluesV1[j];
                    var point = hc + hd;
                    var newPoint = new Vector4(point.X - x, point.Y - y, point.Z - z, 1.0f);

                    points.Add(newPoint);
                }
            }

            mX = ComputeMatrixX();
            mY = ComputeMatrixY();
            mZ = ComputeMatrixZ();

            for (int j = 0; j < DivisionsV; j++)
            {
                float curveV = j * (1.0f / (DivisionsV - 1));
                var vv0 = Hermite(curveV, 0);
                var vv1 = Hermite(curveV, 1);
                var vv2 = Hermite(curveV, 2);
                var vv3 = Hermite(curveV, 3);

                var vecV = new Vector4(vv0, vv1, vv2, vv3);
                for (int i = 0; i < count; i++)
                {
                    float t = i * (1.0f / count);
                    var vu0 = Hermite(t, 0);
                    var vu1 = Hermite(t, 1);
                    var vu2 = Hermite(t, 2);
                    var vu3 = Hermite(t, 3);

                    var vecU = new Vector4(vu0, vu1, vu2, vu3);
                    UpdateMatrix(t, duv00, curveV, dvu00, duv01, dvu01, duv10, dvu10, duv11, dvu11, ref mX, ref mY, ref mZ);

                    var x = Vector4.Dot(Vector4.Transform(vecU, mX), vecV);
                    var y = Vector4.Dot(Vector4.Transform(vecU, mY), vecV);
                    var z = Vector4.Dot(Vector4.Transform(vecU, mZ), vecV);

                    var hc = vu0 * vvaluesU0[j] + vu1 * vderivativesU0[j] + vu2 * vderivativesU1[j] + vu3 * vvaluesU1[j];
                    var hd = vv0 * valuesV0[i] + vv1 * derivativesV0[i] + vv2 * derivativesV1[i] + vv3 * valuesV1[i];
                    var point = hc + hd;
                    var newPoint = new Vector4(point.X - x, point.Y - y, point.Z - z, 1.0f);

                    points.Add(newPoint);
                }
            }

            return points;
        }

        private static void UpdateMatrix(float u, Vector4 duv00, float v, Vector4 dvu00, Vector4 duv01, Vector4 dvu01,
            Vector4 duv10, Vector4 dvu10, Vector4 duv11, Vector4 dvu11, ref Matrix4x4 mX, ref Matrix4x4 mY, ref Matrix4x4 mZ)
        {
            if (u == 0 || v == 0 || u == 1 || v == 1)
                return;

            mX.M22 = (u * duv00.X + v * dvu00.X) / (u + v);
            mX.M23 = (-u * duv01.X + (v - 1) * dvu01.X) / (-u + v - 1);
            mX.M32 = ((1 - u) * duv10.X + v * dvu10.X) / (1 - u + v);
            mX.M33 = ((u - 1) * duv11.X + (v - 1) * dvu11.X) / (u + v - 2);

            mY.M22 = (u * duv00.Y + v * dvu00.Y) / (u + v);
            mY.M23 = (-u * duv01.Y + (v - 1) * dvu01.Y) / (-u + v - 1);
            mY.M32 = ((1 - u) * duv10.Y + v * dvu10.Y) / (1 - u + v);
            mY.M33 = ((u - 1) * duv11.Y + (v - 1) * dvu11.Y) / (u + v - 2);

            mZ.M22 = (u * duv00.Z + v * dvu00.Z) / (u + v);
            mZ.M23 = (-u * duv01.Z + (v - 1) * dvu01.Z) / (-u + v - 1);
            mZ.M32 = ((1 - u) * duv10.Z + v * dvu10.Z) / (1 - u + v);
            mZ.M33 = ((u - 1) * duv11.Z + (v - 1) * dvu11.Z) / (u + v - 2);
        }

        public bool Selected { get; set; }
        public void AddPoint(DrawablePoint point)
        {
        }
    }
}



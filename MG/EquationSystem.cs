using System;
using System.Numerics;

namespace MG
{
    public class EquationSystem
    {
        private readonly Func<float, float, Vector4> F1;
        private readonly Func<float, float, Vector4> F2;
        private readonly Func<float, float, Vector4> F3;
        private readonly bool _differentParams;

        public EquationSystem(Func<float, float, Vector4> f1, Func<float, float, Vector4> f2, Func<float, float, Vector4> f3, bool differentParams)
        {
            F1 = f1;
            F2 = f2;
            F3 = f3;
            _differentParams = differentParams;
        }

        public void Jacobian(float[] x, float[][] jacobian)
        {
            var diff = 0.00001f;

            var f1Point1 = F1(x[0], x[1]);
            var f1Point2 = F1(x[0] + diff, x[1]);
            var f1Point3 = F1(x[0], x[1] + diff);

            var f2Point1 = F2(x[2], x[3]);
            var f2Point2 = F2(x[2] + diff, x[3]);
            var f2Point3 = F2(x[2], x[3] + diff);

            var f3Point1 = F3(x[4], x[5]);
            var f3Point2 = F3(x[4] + diff, x[5]);
            var f3Point3 = F3(x[4], x[5] + diff);

            var d1a = (f1Point2 - f1Point1) / diff;
            var d1b = (f1Point3 - f1Point1) / diff;

            var d2a = (f2Point2 - f2Point1) / diff;
            var d2b = (f2Point3 - f2Point1) / diff;

            var d3a = (f3Point2 - f3Point1) / diff;
            var d3b = (f3Point3 - f3Point1) / diff;

            jacobian[0][0] = d1a.X;
            jacobian[1][0] = d1a.Y;
            jacobian[2][0] = d1a.Z;

            jacobian[0][1] = d1b.X;
            jacobian[1][1] = d1b.Y;
            jacobian[2][1] = d1b.Z;

            jacobian[3][0] = d1a.X;
            jacobian[4][0] = d1a.Y;
            jacobian[5][0] = d1a.Z;

            jacobian[3][1] = d1b.X;
            jacobian[4][1] = d1b.Y;
            jacobian[5][1] = d1b.Z;

            jacobian[0][2] = -d2a.X;
            jacobian[1][2] = -d2a.Y;
            jacobian[2][2] = -d2a.Z;

            jacobian[0][3] = -d2b.X;
            jacobian[1][3] = -d2b.Y;
            jacobian[2][3] = -d2b.Z;

            jacobian[3][4] = -d3a.X;
            jacobian[4][4] = -d3a.Y;
            jacobian[5][4] = -d3a.Z;

            jacobian[3][5] = -d3b.X;
            jacobian[4][5] = -d3b.Y;
            jacobian[5][5] = -d3b.Z;
        }

        public void Calculate(float[] x, float[] y)
        {
            var t1Point = F1(x[0], x[1]);
            var t2Point = F2(x[2], x[3]);
            var t3Point = F3(x[4], x[5]);

            y[0] = t1Point.X - t2Point.X;
            y[1] = t1Point.Y - t2Point.Y;
            y[2] = t1Point.Z - t2Point.Z;

            y[3] = t1Point.X - t3Point.X;
            y[4] = t1Point.Y - t3Point.Y;
            y[5] = t1Point.Z - t3Point.Z;

            if (_differentParams)
            {
                var val1 = (x[0] - x[2]) * (x[0] - x[2]) + 0.01f;
                var val2 = (x[1] - x[3]) * (x[1] - x[3]) + 0.01f;

                var diff1 = 0.0001f / val1;
                var diff2 = 0.0001f / val2;
                y[0] += diff1 * diff2;
            }
        }
    }
}

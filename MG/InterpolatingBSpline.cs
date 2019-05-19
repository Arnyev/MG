using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MG
{
    class InterpolatingBSpline : ICurve, ISublistContaining, IDrawableObject
    {
        private static int _index;
        public string Name { get; set; } = "Interpolating_curve_" + ++_index;

        private readonly List<DrawablePoint> _points = new List<DrawablePoint>();
        public List<DrawablePoint> Points => _points;

        public static Vector3[] Interpolate(Vector3[] points, float[] knots)
        {
            var n = points.Length;

            var h1 = ComputeSplineValue(knots, knots[3], 1, 3);
            var h2 = ComputeSplineValue(knots, knots[n], n, 3);
            var rs = points[1] - points[0] * h1;
            var re = points[points.Length - 2] - points[points.Length - 1] * h2;

            var a = new float[n - 2];
            var b = new float[n - 2];
            var c = new float[n - 2];
            var d = new Vector3[n - 2];

            for (int i = 1; i < n - 2; i++)
            {
                var knot = knots[i + 3];
                a[i] = ComputeSplineValue(knots, knot, i + 1, 3);
                b[i] = ComputeSplineValue(knots, knot, i + 2, 3);
                c[i] = ComputeSplineValue(knots, knot, i + 3, 3);
                d[i] = points[i + 1];
            }
            b[0] = ComputeSplineValue(knots, knots[3], 2, 3);
            c[0] = ComputeSplineValue(knots, knots[3], 3, 3);

            d[0] = rs;
            d[d.Length - 1] = re;
            c[c.Length - 1] = 0.0f;

            var result = SolveTridiagonal(a, b, c, d);

            var list = result.ToList();
            list.Insert(0, points[0]);
            list.Insert(1, points[0]);

            list.Add(points[points.Length - 1]);
            list.Add(points[points.Length - 1]);

            return list.ToArray();
        }

        public bool ChordParametrization { get; set; }

        public static float ComputeSplineValue(float[] knots, float value, int index, int degree)
        {
            if (degree == 0)
                return value >= knots[index - 1] && value < knots[index] ? 1 : 0;

            var firstPart = Math.Abs(knots[index - 1] - knots[index + degree - 1]) < 1e-6
                ? 0
                : (value - knots[index - 1]) / (knots[index + degree - 1] - knots[index - 1]) *
                  ComputeSplineValue(knots, value, index, degree - 1);

            var secondPart = Math.Abs(knots[index] - knots[index + degree]) < 1e-6
                ? 0
                : (knots[index + degree] - value) / (knots[index + degree] - knots[index]) *
                  ComputeSplineValue(knots, value, index + 1, degree - 1);

            return firstPart + secondPart;
        }

        public static Vector3[] SolveTridiagonal(float[] a, float[] b, float[] c, Vector3[] d)
        {
            var n = d.Length;
            c[0] = c[0] / b[0];
            d[0] = d[0] / b[0];
            for (int i = 1; i < n; i++)
            {
                c[i] = c[i] / (b[i] - a[i] * c[i - 1]);
                d[i] = (d[i] - a[i] * d[i - 1]) / (b[i] - a[i] * c[i - 1]);
            }

            var x = new Vector3[n];
            x[n - 1] = d[n - 1];
            for (int i = n - 2; i >= 0; i--)
                x[i] = d[i] - c[i] * x[i + 1];

            return x;
        }

        public bool DrawLines { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Name);
            _points.ForEach(p => sb.Append(" " + p.ToString()));
            return sb.ToString();
        }

        public InterpolatingBSpline(string s)
        {
            var l = s.Split(' ');
            Name = l[0];
            _points.AddRange(l.Skip(1).Select(str => new DrawablePoint(str)));
        }
        public InterpolatingBSpline()
        { }
        public List<Vector4> GetPoints(int count)
        {
            if (_points.Count < 2)
                return new List<Vector4>();

            if (_points.Count == 2)
                return Enumerable.Range(0, count).Select(x => (float)x / count)
                    .Select(a => _points[0].Point * a + _points[1].Point * (1 - a)).ToList();
            float[] knots;

            if (ChordParametrization)
            {
                var distances = _points.Skip(1)
                    .Zip(_points, (second, first) => Vector4.Distance(second.Point, first.Point))
                    .ToArray();
                var sum = 0.0f;
                distances = distances.Select(x => sum += x).ToArray();

                knots = Enumerable.Repeat(0.0f, 3).Concat(distances.Take(distances.Length - 1).Select(x => x / sum))
                    .Concat(Enumerable.Repeat(1.0f, 3)).ToArray();
            }
            else
                knots = Enumerable.Repeat(0.0f, 2)
                    .Concat(Enumerable.Range(0, _points.Count).Select(x => (float)x / (_points.Count - 1)))
                    .Concat(Enumerable.Repeat(1.0f, 2)).ToArray();

            var points = _points.Select(x => new Vector3(x.X, x.Y, x.Z)).ToArray();

            var interpolating = _points.Count == 3 ? Interpolate3Points(points, knots) : Interpolate(points, knots);
            return GetPointsDeBoor(knots, interpolating, count);
        }

        public static List<Vector4> GetPointsDeBoor(float[] knots, Vector3[] controlPoints, int count)
        {
            var start = knots[0];
            var end = knots[knots.Length - 1];

            int intervalIndex = 0;
            while (start >= knots[intervalIndex + 1])
                intervalIndex++;

            var result = new List<Vector4>(count);
            var helper = new Vector3[4];

            for (float val = start; val < end; val += (end - start) / count)
            {
                if (val >= knots[intervalIndex + 1])
                    intervalIndex++;

                for (int index = 0; index < 4; index++)
                    helper[index] = controlPoints[index + intervalIndex - 2];

                for (int level = 1; level < 4; level++)
                    for (int index = 3; index > level - 1; index--)
                    {
                        float alpha = (val - knots[index + intervalIndex - 3]) /
                                      (knots[index + 1 + intervalIndex - level] - knots[index + intervalIndex - 3]);
                        helper[index] = (1.0f - alpha) * helper[index - 1] + alpha * helper[index];
                    }

                result.Add(new Vector4(helper[3].X, helper[3].Y, helper[3].Z, 1.0f));
            }

            return result;
        }

        private static Vector3[] Interpolate3Points(Vector3[] points, float[] knots)
        {
            var interpolating = new Vector3[5];
            interpolating[0] = interpolating[1] = points[0];
            interpolating[3] = interpolating[4] = points[2];

            var n1 = ComputeSplineValue(knots, knots[3], 1, 3);
            var n2 = ComputeSplineValue(knots, knots[3], 2, 3);
            var n3 = ComputeSplineValue(knots, knots[3], 3, 3);
            interpolating[2] = (points[1] - interpolating[1] * n1 - interpolating[3] * n3) / n2;
            return interpolating;
        }

        public bool Selected { get; set; }
        public void AddPoint(DrawablePoint point)
        {
            _points.Add(point);
        }

        public IReadOnlyList<object> List => _points;

        public void RemoveObject(object o)
        {
            _points.Remove(o as DrawablePoint);
        }

        public Matrix4x4 GetModelMatrix()
        {
            return Matrix4x4.Identity;
        }

        public Tuple<Line[], Vector4[]> GetLines()
        {
            if (_points.Count < 2)
                return Tuple.Create(new Line[0], new Vector4[0]);

            var points = _points.Select(x => x.Point).ToArray();
            var lines = Enumerable.Range(0, _points.Count - 1).Select(x => new Line(x, x + 1)).ToArray();
            return Tuple.Create(lines, points);
        }
    }
}

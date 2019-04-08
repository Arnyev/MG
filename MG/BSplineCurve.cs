using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;

namespace MG
{
    class BSplineCurve : IDrawableObject, ISublistContaining, ICurve
    {
        private static int _index;

        private List<DrawablePoint> _points = new List<DrawablePoint>();
        private List<DrawablePoint> _bernsteinPoints = new List<DrawablePoint>();

        public void AddPoint(DrawablePoint point)
        {
            if (IsBernstein)
            {
                MessageBox.Show("Can't add points in Bernstein mode.");
                return;
            }
            if (point != null && !_points.Contains(point))
                _points.Add(point);
        }

        public bool Selected { get; set; }
        public bool DrawLines { get; set; }
        public string Name { get; set; } = "BSpline curve " + ++_index;

        private bool _isBernstein;
        public bool IsBernstein
        {
            get => _isBernstein;
            set
            {
                if (value == _isBernstein)
                    return;

                _isBernstein = value;
                if (value)
                    ConvertToBernstein();
                else
                    ConvertFromBernstein();
            }
        }

        private void ConvertToBernstein()
        {
            var bernsteinPoints = new List<Vector4>();
            var newPoints = _points.ToList();

            for (int i = 0; i < newPoints.Count - 3; i++)
            {
                var s1 = newPoints[i].Point;
                var s2 = newPoints[i + 1].Point;
                var s3 = newPoints[i + 2].Point;
                var s4 = newPoints[i + 3].Point;

                var h1 = s1 * 1.0f / 3 + s2 * 2.0f / 3;
                var h2 = s4 * 1.0f / 3 + s3 * 2.0f / 3;

                var b2 = s2 * 2.0f / 3 + s3 * 1.0f / 3;
                var b3 = s2 * 1.0f / 3 + s3 * 2.0f / 3;
                var b1 = (h1 + b2) / 2;
                var b4 = (h2 + b3) / 2;

                bernsteinPoints.Add(b1);
                bernsteinPoints.Add(b2);
                bernsteinPoints.Add(b3);
                if (i == newPoints.Count - 4)
                    bernsteinPoints.Add(b4);
            }

            _bernsteinPoints = bernsteinPoints.Select(x => new DrawablePoint(x.X, x.Y, x.Z)).ToList();
        }

        private void ConvertFromBernstein()
        {
            for (int i = 0; i < _bernsteinPoints.Count - 3; i += 3)
            {
                var b1 = _bernsteinPoints[i].Point;
                var b2 = _bernsteinPoints[i + 1].Point;
                var b3 = _bernsteinPoints[i + 2].Point;
                var b4 = _bernsteinPoints[i + 3].Point;

                var h1 = 2 * b1 - b2;
                var h2 = 2 * b4 - b3;

                var s2 = 2 * b2 - b3;
                var s3 = 2 * b3 - b2;
                var s1 = 3 * h1 - 2 * s2;
                var s4 = 3 * h2 - 2 * s3;

                if (i == 0)
                {
                    _points[0].Point = s1;
                    _points[1].Point = s2;
                    _points[2].Point = s3;
                    _points[3].Point = s4;
                }
                else
                {
                    _points[3 + i / 3].Point = s4;
                }
            }
        }

        private void CorrectBernsteinPoints()
        {
            for (int i = 4; i < _bernsteinPoints.Count; i += 3)
            {
                var last = _bernsteinPoints[i - 1].Point;
                var prev = _bernsteinPoints[i - 2].Point;
                var evenPrev = _bernsteinPoints[i - 3].Point;
                var bsplinePoint = 2 * prev - evenPrev;

                var nextPoint = 2 * last - prev;
                _bernsteinPoints[i].Point = nextPoint;
                _bernsteinPoints[i + 1].Point = 2 * nextPoint - bsplinePoint;
            }
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


        public List<Vector4> GetPoints(int count)
        {
            if (IsBernstein)
                return ComputeInBernsteinBasis(count);

            List<Vector4> points = new List<Vector4>();
            var newPoints = _points.ToList();

            for (int i = 3; i < newPoints.Count; i++)
            {
                for (float j = 0.0f; j <= 1.0f; j += 1.0f / count)
                {
                    var a1 = GetFirstPart(j);
                    var a2 = GetSecondPart(j);
                    var a3 = GetThirdPart(j);
                    var a4 = GetFourthPart(j);

                    var f1 = a1 * newPoints[i].Point;
                    var f2 = a2 * newPoints[i - 1].Point;
                    var f3 = a3 * newPoints[i - 2].Point;
                    var f4 = a4 * newPoints[i - 3].Point;
                    points.Add(f1 + f2 + f3 + f4);
                }
            }

            return points;
        }

        private List<Vector4> ComputeInBernsteinBasis(int count)
        {
            var list = new List<Vector4>();
            var diff = 1.0f / (count - 1);

            CorrectBernsteinPoints();
            var coordinates = _bernsteinPoints.Select(x => new Vector3(x.X, x.Y, x.Z)).ToArray();

            var currentPointIndex = 0;
            while (coordinates.Length - currentPointIndex >= 4)
            {
                list.AddRange(Enumerable.Range(0, count).Select(i =>
                    BezierCurve.Point4C(coordinates[currentPointIndex], coordinates[currentPointIndex + 1],
                        coordinates[currentPointIndex + 2], coordinates[currentPointIndex + 3], i * diff)));
                currentPointIndex += 3;
            }

            return list;
        }

        private static float GetFourthPart(float val)
        {
            val = 1 - val;
            return val * val * val / 6;
        }

        private static float GetThirdPart(float val)
        {
            return (4 - 6 * val * val + 3 * val * val * val) / 6;
        }

        private static float GetSecondPart(float val)
        {
            return (1 + 3 * val + 3 * val * val - 3 * val * val * val) / 6;
        }

        private static float GetFirstPart(float val)
        {
            return val * val * val / 6;
        }

        public IReadOnlyList<object> List => _points;
        public void RemoveObject(object o)
        {
            if (IsBernstein)
            {
                MessageBox.Show("Can't remove points in Bernstein mode.");
                return;
            }
            _points.Remove(o as DrawablePoint);
        }

        public List<DrawablePoint> BernsteinPoints => _isBernstein ? _bernsteinPoints.ToList() : new List<DrawablePoint>();
    }

    public interface ICurve
    {
        bool DrawLines { get; }
        List<Vector4> GetPoints(int count);
        bool Selected { get; set; }
        void AddPoint(DrawablePoint point);
    }
}

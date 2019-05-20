using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MG
{
    public class SurfaceProperties
    {
        public bool IsTube { get; set; }
        public int CountU { get; set; } = 1;
        public int CountV { get; set; } = 1;
        public float SizeU { get; set; } = 2;
        public float SizeV { get; set; } = 2;
    }

    public class BasicSurface : IDrawableObject, ICurve
    {
        private readonly int _countU;
        private readonly int _countV;
        private readonly bool _isTube;
        private readonly List<DrawablePoint> _points = new List<DrawablePoint>();
        public IReadOnlyList<DrawablePoint> Points => _points;

        public bool IsTube => _isTube;
        public BasicSurface(SurfaceProperties properties, Vector4 translation = new Vector4())
        {
            _countU = properties.CountU;
            _countV = properties.CountV;
            _isTube = properties.IsTube;

            if (_countU < 1 || _countV < 1)
                return;

            if (!_isTube)
            {
                var scaleU = properties.SizeU / (_countU * 3);
                var scaleV = properties.SizeV / (_countV * 3);

                for (int i = 0; i < _countU * 3 + 1; i++)
                    for (int j = 0; j < _countV * 3 + 1; j++)
                        _points.Add(new DrawablePoint(i * scaleU, 0, j * scaleV));

                if (_countV == 1 && _countV == 1)
                {
                    _points[0].IsCorner = true;
                    _points[3].IsCorner = true;
                    _points[12].IsCorner = true;
                    _points[15].IsCorner = true;
                }
            }
            else
            {
                var scaleU = properties.SizeU / (_countU * 3);
                var scaleV = (float)Math.PI * 2 / (_countV * 3);

                for (int i = 0; i < _countU * 3 + 1; i++)
                    for (int j = 0; j < _countV * 3; j++)
                        _points.Add(new DrawablePoint((float)Math.Cos(j * scaleV) * properties.SizeV, (float)Math.Sin(j * scaleV) * properties.SizeV, i * scaleU));
            }

            _points.ForEach(x => x.Point = x.Point + translation);
        }

        public Vector4 Dv(float u, float v, int depth = 0)
        {
            var lcPoints = _points.Select(x => x.Point).ToArray();
            var point = new Vector4();
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 3; j++)
                {
                    var diff = lcPoints[i * 4 + j + 1] - lcPoints[i * 4 + j];
                    var b1 = GetBernsteinValue(i, u);
                    var b2 = GetBernsteinValue2(j, v);
                    point += diff * b1 * b2;
                }

            return 3 * point;
        }

        public Vector4 Du(float u, float v, int depth = 0)
        {
            var lcPoints = _points.Select(x => x.Point).ToArray();
            var point = new Vector4();
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 4; j++)
                {
                    var diff = lcPoints[i * 4 + j + 4] - lcPoints[i * 4 + j];
                    var b1 = GetBernsteinValue2(i, u);
                    var b2 = GetBernsteinValue(j, v);
                    point += diff * b1 * b2;
                }

            return 3 * point;
        }

        public Vector4 DuDv(float u, float v)
        {
            var lcPoints = _points.Select(x => x.Point).ToArray();
            var point = new Vector4();
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    var diff = (lcPoints[i * 4 + j + 5] - lcPoints[i * 4 + j + 1]) - (lcPoints[i * 4 + j + 4] - lcPoints[i * 4 + j]);
                    var b1 = GetBernsteinValue2(i, u);
                    var b2 = GetBernsteinValue2(j, v);
                    point += diff * b1 * b2;
                }

            var derivative = 9 * point;

            return derivative;
        }

        public Vector4 GetPoint(float u, float v)
        {
            var bezierPoints = _points.Select(x => x.Point).ToArray();
            var point = new Vector4();
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    point += bezierPoints[i * 4 + j] * GetBernsteinValue(i, u) *
                             GetBernsteinValue(j, v);

            return point;
        }

        private float GetBernsteinValue2(int index, float t)
        {
            switch (index)
            {
                case 0:
                    return (1.0f - t) * (1.0f - t);
                case 1:
                    return 2 * t * (1.0f - t);
                case 2:
                    return t * t;
            }

            return 0;
        }
        public bool AreLine(DrawablePoint p1, DrawablePoint p2)
        {
            if (!p1.IsCorner || !p2.IsCorner)
                return false;

            var i1 = _points.IndexOf(p1);
            var i2 = _points.IndexOf(p2);

            if (i1 < 0 || i2 < 0)
                return false;

            if (i1 == i2)
                return false;

            var sm = i1 < i2 ? i1 : i2;
            var bi = i1 > i2 ? i1 : i2;

            if (sm == 0 && bi == 15)
                return false;

            if (sm == 3 && bi == 12)
                return false;

            return true;
        }

        public void ReplacePoint(DrawablePoint old, DrawablePoint newPoint)
        {
            var index = _points.IndexOf(old);
            if (index < 0 || index >= _points.Count)
                return;

            if (!old.IsCorner)
                return;

            _points[index] = newPoint;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Name);
            sb.Append(" " + _countU);
            sb.Append(" " + _countV);
            _points.ForEach(p => sb.Append(" " + p.ToString()));
            return sb.ToString();
        }

        public BasicSurface(string s, bool isTube)
        {
            var l = s.Split(' ');
            Name = l[0];
            _countU = int.Parse(l[1]);
            _countV = int.Parse(l[2]);
            _points = l.Skip(3).Select(x => new DrawablePoint(x)).ToList();
            _isTube = isTube;
        }

        public Matrix4x4 GetModelMatrix()
        {
            return Matrix4x4.Identity;
        }

        public Tuple<Line[], Vector4[]> GetLines()
        {
            var points = _points.Select(x => x.Point).ToArray();
            var lines = new List<Line>();
            var add = _isTube ? 0 : 1;

            for (int i = 0; i < _countU * 3 + 1; i++)
            {
                for (int j = 0; j < _countV * 3 - 1; j++)
                {
                    var startIndex = i * (_countV * 3 + add) + j;
                    lines.Add(new Line(startIndex, startIndex + 1));
                }

                var indexU = i * (_countV * 3 + add);
                lines.Add(_isTube
                    ? new Line(indexU, indexU + _countV * 3 - 1)
                    : new Line(indexU + _countV * 3 - 1, indexU + _countV * 3));
            }

            for (int i = 0; i < _countU * 3; i++)
                for (int j = 0; j < _countV * 3 + add; j++)
                {
                    var startIndex = i * (_countV * 3 + add) + j;
                    lines.Add(new Line(startIndex, startIndex + _countV * 3 + add));
                }

            return Tuple.Create(lines.ToArray(), points);
        }

        public bool DrawLines { get; set; }
        public int DivisionsU { get; set; } = 4;
        public int DivisionsV { get; set; } = 4;

        public bool AllPointsSelected
        {
            get => _points.All(x => x.Selected);
            set => _points.ForEach(x => x.Selected = value);
        }

        public List<Vector4> GetPoints(int count)
        {
            var list = new List<Vector4>();
            count /= 8;

            for (int patchU = 0; patchU < _countU; patchU++)
            {
                for (int patchV = 0; patchV < _countV; patchV++)
                {
                    var bezierPoints = GetBezierPoints(patchU, patchV);

                    for (float curveU = 0; curveU <= 1.0; curveU += 1.0f / (DivisionsU - 1))
                    {
                        for (float t = 0; t < 1.0f; t += 1.0f / count)
                        {
                            var point = new Vector4();
                            for (int i = 0; i < 4; i++)
                                for (int j = 0; j < 4; j++)
                                    point += bezierPoints[i * 4 + j] * GetBernsteinValue(i, curveU) *
                                             GetBernsteinValue(j, t);
                            list.Add(point);
                        }
                    }

                    for (float curveV = 0; curveV <= 1.0; curveV += 1.0f / (DivisionsV - 1))
                    {
                        for (float t = 0; t < 1.0f; t += 1.0f / count)
                        {
                            var point = new Vector4();
                            for (int i = 0; i < 4; i++)
                                for (int j = 0; j < 4; j++)
                                    point += bezierPoints[i * 4 + j] * GetBernsteinValue(j, curveV) *
                                             GetBernsteinValue(i, t);
                            list.Add(point);
                        }
                    }
                }
            }

            return list;
        }

        private Vector4[] GetBezierPoints(int patchU, int patchV)
        {
            var bezierPoints = new Vector4[16];
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 3; j++)
                {
                    var indexU = (3 * patchU + i) * (3 * _countV + (_isTube ? 0 : 1));
                    bezierPoints[4 * i + j] = _points[indexU + 3 * patchV + j].Point;
                }

            if (_isTube && patchV == _countV - 1)
            {
                for (int i = 0; i < 4; i++)
                {
                    var indexU = (3 * patchU + i) * 3 * _countV;
                    bezierPoints[4 * i + 3] = _points[indexU].Point;
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    var indexU = (3 * patchU + i) * (3 * _countV + (_isTube ? 0 : 1));
                    bezierPoints[4 * i + 3] = _points[indexU + 3 * patchV + 3].Point;
                }
            }

            return bezierPoints;
        }

        private float GetBernsteinValue(int index, float t)
        {
            switch (index)
            {
                case 0:
                    return (1.0f - t) * (1.0f - t) * (1.0f - t);
                case 1:
                    return 3 * t * (1.0f - t) * (1.0f - t);
                case 2:
                    return 3 * t * t * (1.0f - t);
                case 3:
                    return t * t * t;
            }

            return 0;
        }

        public bool Selected { get; set; }
        public void AddPoint(DrawablePoint point)
        {
        }

        static int Nr = 1;
        public string Name { get; set; } = "Basic_surface" + Nr++;
    }
}

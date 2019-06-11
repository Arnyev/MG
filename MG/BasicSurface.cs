using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MG
{
    public enum UVDirection
    {
        U0V01,
        U0V10,
        U1V01,
        U1V10,
        V0U01,
        V0U10,
        V1U01,
        V1U10,
    }
    public class SurfaceProperties
    {
        public bool IsTube { get; set; }
        public int CountU { get; set; } = 1;
        public int CountV { get; set; } = 1;
        public float SizeU { get; set; } = 2;
        public float SizeV { get; set; } = 2;
    }

    public class BasicSurface : IDrawableObject, ICurve, IIntersecting
    {
        private readonly int _countU;
        private readonly int _countV;
        private readonly bool _isTube;
        private const int ParameterRangePrecision = 1000;
        private bool Trimmed;
        private bool[,] ParameterRange;

        private readonly List<DrawablePoint> _points = new List<DrawablePoint>();

        public IReadOnlyList<DrawablePoint> Points => _points;

        public bool IsTube => _isTube;
        public BasicSurface(SurfaceProperties properties, Vector4 translation = new Vector4())
        {
            _countU = properties.CountU;
            _countV = properties.CountV;
            _isTube = properties.IsTube;

            ParameterRange = new bool[ParameterRangePrecision + 1, ParameterRangePrecision + 1];
            for (int i = 0; i < ParameterRangePrecision + 1; i++)
                for (int j = 0; j < ParameterRangePrecision + 1; j++)
                    ParameterRange[i, j] = true;

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

        public UVDirection GetDirection(DrawablePoint p1, DrawablePoint p2)
        {
            var i1 = _points.IndexOf(p1);
            var i2 = _points.IndexOf(p2);

            switch (i1)
            {
                case 0 when i2 == 3:
                    return UVDirection.U0V01;
                case 3 when i2 == 0:
                    return UVDirection.U0V10;
                case 0 when i2 == 12:
                    return UVDirection.V0U01;
                case 12 when i2 == 0:
                    return UVDirection.V0U10;
                case 3 when i2 == 15:
                    return UVDirection.V1U01;
                case 15 when i2 == 3:
                    return UVDirection.V1U10;
                case 12 when i2 == 15:
                    return UVDirection.U1V01;
                case 15 when i2 == 12:
                    return UVDirection.U1V10;
                default:
                    return UVDirection.U0V01;
            }
        }

        public Func<float, Vector4> GetValueFunc(UVDirection direction, bool firstHalf)
        {
            switch (direction)
            {
                case UVDirection.U0V01:
                    if (firstHalf)
                        return f => GetPoint(0, f / 2);
                    else
                        return f => GetPoint(0, f / 2 + 0.5f);
                case UVDirection.U0V10:
                    if (firstHalf)
                        return f => GetPoint(0, 1 - f / 2);
                    else
                        return f => GetPoint(0, 0.5f - f / 2);
                case UVDirection.U1V01:
                    if (firstHalf)
                        return f => GetPoint(1, f / 2);
                    else
                        return f => GetPoint(1, f / 2 + 0.5f);
                case UVDirection.U1V10:
                    if (firstHalf)
                        return f => GetPoint(1, 1 - f / 2);
                    else
                        return f => GetPoint(1, 0.5f - f / 2);
                case UVDirection.V0U01:
                    if (firstHalf)
                        return f => GetPoint(f / 2, 0);
                    else
                        return f => GetPoint(0.5f + f / 2, 0);
                case UVDirection.V0U10:
                    if (firstHalf)
                        return f => GetPoint(1 - f / 2, 0);
                    else
                        return f => GetPoint(0.5f - f / 2, 0);
                case UVDirection.V1U01:
                    if (firstHalf)
                        return f => GetPoint(f / 2, 1);
                    else
                        return f => GetPoint(0.5f + f / 2, 1);
                case UVDirection.V1U10:
                    if (firstHalf)
                        return f => GetPoint(1 - f / 2, 1);
                    else
                        return f => GetPoint(0.5f - f / 2, 1);
            }

            return f => new Vector4();
        }

        public Func<float, Vector4> GetDerivativeFunc(UVDirection direction, bool firstHalf)
        {
            switch (direction)
            {
                case UVDirection.U0V01:
                    if (firstHalf)
                        return f => Du(0, f / 2);
                    else
                        return f => Du(0, f / 2 + 0.5f);
                case UVDirection.U0V10:
                    if (firstHalf)
                        return f => Du(0, 1 - f / 2);
                    else
                        return f => Du(0, 0.5f - f / 2);
                case UVDirection.U1V01:
                    if (firstHalf)
                        return f => Du(1, f / 2);
                    else
                        return f => Du(1, f / 2 + 0.5f);
                case UVDirection.U1V10:
                    if (firstHalf)
                        return f => Du(1, 1 - f / 2);
                    else
                        return f => Du(1, 0.5f - f / 2);
                case UVDirection.V0U01:
                    if (firstHalf)
                        return f => Dv(f / 2, 0);
                    else
                        return f => Dv(0.5f + f / 2, 0);
                case UVDirection.V0U10:
                    if (firstHalf)
                        return f => Dv(1 - f / 2, 0);
                    else
                        return f => Dv(0.5f - f / 2, 0);
                case UVDirection.V1U01:
                    if (firstHalf)
                        return f => Dv(f / 2, 1);
                    else
                        return f => Dv(0.5f + f / 2, 1);
                case UVDirection.V1U10:
                    if (firstHalf)
                        return f => Dv(1 - f / 2, 1);
                    else
                        return f => Dv(0.5f - f / 2, 1);
            }

            return f => new Vector4();
        }

        public Func<Vector4> DerivativeMiddle(UVDirection direction)
        {
            switch (direction)
            {
                case UVDirection.U0V01:
                    return () => Dv(0, 0.5f);
                case UVDirection.U0V10:
                    return () => Dv(0, 0.5f);
                case UVDirection.U1V01:
                    return () => Dv(1, 0.5f);
                case UVDirection.U1V10:
                    return () => Dv(1, 0.5f);
                case UVDirection.V0U01:
                    return () => Du(0.5f, 0);
                case UVDirection.V0U10:
                    return () => Du(0.5f, 0);
                case UVDirection.V1U01:
                    return () => Du(0.5f, 1);
                case UVDirection.V1U10:
                    return () => Du(0.5f, 1);

            }

            return () => new Vector4();
        }

        public Tuple<Func<Vector4>, Func<Vector4>> GetSecondDerivativeFuncs
            (UVDirection direction, bool firstHalf)
        {
            switch (direction)
            {
                case UVDirection.U0V01:
                    if (firstHalf)
                        return new Tuple<Func<Vector4>, Func<Vector4>>(() => DuDv(0, 0), () => DuDv(0, 0.5f));
                    else
                        return new Tuple<Func<Vector4>, Func<Vector4>>(() => DuDv(0, 0.5f), () => DuDv(0, 1));
                case UVDirection.U0V10:
                    if (firstHalf)
                        return new Tuple<Func<Vector4>, Func<Vector4>>(() => DuDv(0, 1), () => DuDv(0, 0.5f));
                    else
                        return new Tuple<Func<Vector4>, Func<Vector4>>(() => DuDv(0, 0.5f), () => DuDv(0, 1));
                case UVDirection.U1V01:
                    if (firstHalf)
                        return new Tuple<Func<Vector4>, Func<Vector4>>(() => DuDv(1, 0), () => DuDv(0, 0.5f));
                    else
                        return new Tuple<Func<Vector4>, Func<Vector4>>(() => DuDv(1, 0.5f), () => DuDv(1, 1));
                case UVDirection.U1V10:
                    if (firstHalf)
                        return new Tuple<Func<Vector4>, Func<Vector4>>(() => DuDv(1, 1), () => DuDv(1, 0.5f));
                    else
                        return new Tuple<Func<Vector4>, Func<Vector4>>(() => DuDv(1, 0.5f), () => DuDv(1, 0));
                case UVDirection.V0U01:
                    if (firstHalf)
                        return new Tuple<Func<Vector4>, Func<Vector4>>(() => DuDv(0, 0), () => DuDv(0.5f, 0));
                    else
                        return new Tuple<Func<Vector4>, Func<Vector4>>(() => DuDv(0.5f, 0), () => DuDv(1, 0));
                case UVDirection.V0U10:
                    if (firstHalf)
                        return new Tuple<Func<Vector4>, Func<Vector4>>(() => DuDv(1, 0), () => DuDv(0.5f, 0));
                    else
                        return new Tuple<Func<Vector4>, Func<Vector4>>(() => DuDv(0.5f, 0), () => DuDv(0, 0));
                case UVDirection.V1U01:
                    if (firstHalf)
                        return new Tuple<Func<Vector4>, Func<Vector4>>(() => DuDv(0, 1), () => DuDv(0.5f, 1));
                    else
                        return new Tuple<Func<Vector4>, Func<Vector4>>(() => DuDv(0.5f, 1), () => DuDv(1, 1));
                case UVDirection.V1U10:
                    if (firstHalf)
                        return new Tuple<Func<Vector4>, Func<Vector4>>(() => DuDv(1, 1), () => DuDv(0.5f, 1));
                    else
                        return new Tuple<Func<Vector4>, Func<Vector4>>(() => DuDv(0.5f, 1), () => DuDv(0, 1));
            }

            return new Tuple<Func<Vector4>, Func<Vector4>>(() => new Vector4(), () => new Vector4());
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
                        var paramU = (int)((patchU + curveU) * ParameterRangePrecision / _countU);

                        for (float t = 0; t < 1.0f; t += 1.0f / count)
                        {
                            if (Trimmed)
                            {
                                var paramV = (int)((patchV + t) * ParameterRangePrecision / _countV);
                                if (!ParameterRange[paramU, paramV])
                                    continue;
                            }

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
                        var paramV = (int)((patchV + curveV) * ParameterRangePrecision / _countV);

                        for (float t = 0; t < 1.0f; t += 1.0f / count)
                        {
                            if (Trimmed)
                            {
                                var paramU = (int)((patchU + t) * ParameterRangePrecision / _countU);
                                if (!ParameterRange[paramU, paramV])
                                    continue;
                            }

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
        public void GetTriangles(int divisionsU, int divisionsV, List<TriangleParameters> parameterValues, List<TriangleIndices> indices, List<Vector3> points)
        {
            var pointCount = divisionsU * divisionsV;
            var pointsPerPathU = (int)Math.Ceiling((float)divisionsU / _countU);
            var pointsPerPathV = (int)Math.Ceiling((float)divisionsV / _countV);

            var bernsteinValuesU = new float[pointsPerPathU, 4];
            var bernsteinValuesV = new float[pointsPerPathV, 4];

            for (int i = 0; i < pointsPerPathU; i++)
            {
                var u = i * 1.0f / (pointsPerPathU - 1);
                for (int j = 0; j < 4; j++)
                    bernsteinValuesU[i, j] = GetBernsteinValue(j, u);
            }

            for (int i = 0; i < pointsPerPathV; i++)
            {
                var v = i * 1.0f / (pointsPerPathV - 1);
                for (int j = 0; j < 4; j++)
                    bernsteinValuesV[i, j] = GetBernsteinValue(j, v);
            }

            var diffU = 1.0f / (pointsPerPathU - 1);
            var diffV = 1.0f / (pointsPerPathV - 1);

            for (int indexU = 0; indexU < divisionsU; indexU++)
                for (int indexV = 0; indexV < divisionsV; indexV++)
                {
                    var patchU = indexU / pointsPerPathU;
                    var ui = indexU % pointsPerPathU;

                    var patchV = indexV / pointsPerPathV;
                    var vi = indexV % pointsPerPathV;

                    var bezierPoints = GetBezierPoints(patchU, patchV);

                    var point = new Vector4();
                    for (int i = 0; i < 4; i++)
                        for (int j = 0; j < 4; j++)
                            point += bezierPoints[i * 4 + j] * bernsteinValuesU[ui, i] * bernsteinValuesV[vi, j];

                    points.Add(new Vector3(point.X, point.Y, point.Z));
                    var u = patchU + (float)ui / (pointsPerPathU - 1);
                    var v = patchV + (float)vi / (pointsPerPathV - 1);

                    if (indexU != divisionsU - 1 && (_isTube || indexV != divisionsV - 1))
                    {
                        var ind = indexU * divisionsV + indexV;
                        var ind2 = (ind + 1) % pointCount;
                        var ind3 = (ind + divisionsV) % pointCount;
                        var ind4 = (ind + divisionsV + 1) % pointCount;

                        parameterValues.Add(new TriangleParameters(
                            new Vector2(u, v),
                            new Vector2(u, v + diffV),
                            new Vector2(u + diffU, v)));


                        parameterValues.Add(new TriangleParameters(
                            new Vector2(u, v + diffV),
                            new Vector2(u + diffU, v + diffV),
                            new Vector2(u + diffU, v)));


                        indices.Add(new TriangleIndices(ind, ind2, ind3));
                        indices.Add(new TriangleIndices(ind2, ind4, ind3));
                    }
                }
        }

        public Vector4 GetWorldPoint(float u, float v)
        {
            int patchU;

            if (u >= _countU)
            {
                patchU = _countU - 1;
                u = 1;
            }
            else if (u <= 0)
            {
                u = 0;
                patchU = 0;
            }
            else
            {
                patchU = (int)u;
                u = u - patchU;
            }

            int patchV;
            if (v >= _countV)
            {
                patchV = _countV - 1;
                v = 1;
            }
            else if (v <= 0)
            {
                v = 0;
                patchV = 0;
            }
            else
            {
                patchV = (int)v;
                v = v - patchV;
            }

            var bezierPoints = GetBezierPoints(patchU, patchV);

            var point = new Vector4();
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    point += bezierPoints[i * 4 + j] * GetBernsteinValue(i, u) * GetBernsteinValue(j, v);

            return point;
        }

        public void Trim(List<Vector2> parameters)
        {
            if (parameters.Count < 3)
                return;

            var eps = 0.1;
            var first = parameters[0];
            var last = parameters[parameters.Count - 1];

            var circle = (first - last).Length() < eps;
            var firstBound = Math.Abs(first.X) < eps || Math.Abs(_countU - first.X) < eps || Math.Abs(first.Y) < eps ||
                             Math.Abs(_countV - first.Y) < eps;

            var lastBound = Math.Abs(last.X) < eps || Math.Abs(_countU - last.X) < eps || Math.Abs(last.Y) < eps ||
                             Math.Abs(_countV - last.Y) < eps;

            if (!circle && (!firstBound || !lastBound))
                return;

            var points = parameters.Select(x =>
                    new Point((int)(x.X * 1000 / _countU), (int)(x.Y * 1000 / _countV)))
                .ToArray();

            ScanLineAlgorithm.FillPolygon(ParameterRange, points);
            Trimmed = true;
        }

        public void DrawCurve(List<Vector2> parameters, DirectBitmap bitmap)
        {
            var multU = (int)((bitmap.Width - 5) / (float)_countU);
            var multV = (int)((bitmap.Width - 5) / (float)_countV);

            var m = bitmap.Width;
            var lines = parameters.Select(x => new Point((int)(x.X * multU), (int)(x.Y * multV))).ToList();
            var lines2 = lines.Select(x => new Point(((x.X % m) + m) % m, ((x.Y % m) + m) % m)).ToList();

            var myColor = new MyColor(255, 255, 255);

            var lines3 = lines2.Zip(lines2.Skip(1), (x, y) => Tuple.Create(x, y)).ToList();

            lines3.ForEach(x => bitmap.DrawLine(x.Item1, x.Item2, myColor, false));
        }

        public Vector3 GetNormalizedWorldNormal(float u, float v)
        {
            const float diff = 0.0001f;
            var point1 = GetWorldPoint(u, v);
            var point2 = GetWorldPoint(u + diff, v);
            var point3 = GetWorldPoint(u, v + diff);

            var d1 = point2 - point1;
            var d2 = point3 - point1;

            var v1 = new Vector3(d1.X, d1.Y, d1.Z);
            var v2 = new Vector3(d2.X, d2.Y, d2.Z);

            return Vector3.Normalize(Vector3.Cross(v1, v2));
        }
    }
}

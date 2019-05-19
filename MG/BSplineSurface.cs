﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MG
{
    class BSplineSurface : IDrawableObject, ICurve
    {
        private readonly int _countU;
        private readonly int _countV;
        private readonly bool _isTube;
        private readonly List<DrawablePoint> _points = new List<DrawablePoint>();
        public IReadOnlyList<DrawablePoint> Points => _points;

        public int DivisionsU { get; set; } = 4;
        public int DivisionsV { get; set; } = 4;
        public bool IsTube => _isTube;

        public BSplineSurface(SurfaceProperties properties)
        {
            _countU = properties.CountU + 3;
            _countV = properties.CountV + 3;
            _isTube = properties.IsTube;

            if (_countU < 4 || _countV < 4)
            {
                MessageBox.Show("Cannot make a Bspline surface with no patch.");
                return;
            }

            if (!_isTube)
            {
                var scaleU = properties.SizeU / _countU;
                var scaleV = properties.SizeV / _countV;

                for (int i = 0; i < _countU; i++)
                    for (int j = 0; j < _countV; j++)
                        _points.Add(new DrawablePoint(i * scaleU, 0, j * scaleV));
            }
            else
            {
                var scaleU = properties.SizeU / _countU;
                var scaleV = (float)Math.PI * 2 / _countV;

                for (int i = 0; i < _countU; i++)
                    for (int j = 0; j < _countV; j++)
                        _points.Add(new DrawablePoint((float)Math.Cos(j * scaleV), (float)Math.Sin(j * scaleV), i * scaleU));
            }
        }

        public Matrix4x4 GetModelMatrix()
        {
            return Matrix4x4.Identity;
        }

        public Tuple<Line[], Vector4[]> GetLines()
        {
            var points = _points.Select(x => x.Point).ToArray();
            var lines = new List<Line>();

            for (int i = 0; i < _countU - 1; i++)
                for (int j = 0; j < _countV; j++)
                    lines.Add(new Line(i * _countV + j, (i + 1) * _countV + j));

            for (int i = 0; i < _countU; i++)
                for (int j = 0; j < _countV - 1; j++)
                    lines.Add(new Line(i * _countV + j, i * _countV + j + 1));

            if (_isTube)
                for (int i = 0; i < _countU; i++)
                    lines.Add(new Line(i * _countV, (i + 1) * _countV - 1));

            return Tuple.Create(lines.ToArray(), points);
        }

        public bool DrawLines { get; set; }
        public List<Vector4> GetPoints(int count)
        {
            var list = new List<Vector4>();
            count /= 8;

            for (int patchU = 3; patchU < _countU; patchU++)
            {
                var startV = _isTube ? 0 : 3;
                for (int patchV = startV; patchV < _countV; patchV++)
                {
                    var p = GetDeBoorPoints(patchU, patchV);

                    for (float curveU = 0; curveU <= 1.0; curveU += 1.0f / (DivisionsU - 1))
                    {
                        var u1 = BSplineCurve.GetFirstPart(curveU);
                        var u2 = BSplineCurve.GetSecondPart(curveU);
                        var u3 = BSplineCurve.GetThirdPart(curveU);
                        var u4 = BSplineCurve.GetFourthPart(curveU);

                        for (float t = 0; t < 1.0f; t += 1.0f / count)
                        {
                            var t1 = t * t * t / 6;
                            var t2 = (1 + 3 * t + 3 * t * t - 3 * t * t * t) / 6;
                            var t3 = (4 - 6 * t * t + 3 * t * t * t) / 6;
                            var t4 = (1 - t) * (1 - t) * (1 - t) / 6;
                            var point =
                                u1 * t1 * p[0] + u1 * t2 * p[1] + u1 * t3 * p[2] + u1 * t4 * p[3] +
                                u2 * t1 * p[4] + u2 * t2 * p[5] + u2 * t3 * p[6] + u2 * t4 * p[7] +
                                u3 * t1 * p[8] + u3 * t2 * p[9] + u3 * t3 * p[10] + u3 * t4 * p[11] +
                                u4 * t1 * p[12] + u4 * t2 * p[13] + u4 * t3 * p[14] + u4 * t4 * p[15];

                            list.Add(point);
                        }
                    }

                    for (float curveV = 0; curveV <= 1.0; curveV += 1.0f / (DivisionsV - 1))
                    {
                        var u1 = BSplineCurve.GetFirstPart(curveV);
                        var u2 = BSplineCurve.GetSecondPart(curveV);
                        var u3 = BSplineCurve.GetThirdPart(curveV);
                        var u4 = BSplineCurve.GetFourthPart(curveV);

                        for (float t = 0; t < 1.0f; t += 1.0f / count)
                        {
                            var t1 = t * t * t / 6;
                            var t2 = (1 + 3 * t + 3 * t * t - 3 * t * t * t) / 6;
                            var t3 = (4 - 6 * t * t + 3 * t * t * t) / 6;
                            var t4 = (1 - t) * (1 - t) * (1 - t) / 6;
                            var point =
                                t1 * u1 * p[0] + t1 * u2 * p[1] + t1 * u3 * p[2] + t1 * u4 * p[3] +
                                t2 * u1 * p[4] + t2 * u2 * p[5] + t2 * u3 * p[6] + t2 * u4 * p[7] +
                                t3 * u1 * p[8] + t3 * u2 * p[9] + t3 * u3 * p[10] + t3 * u4 * p[11] +
                                t4 * u1 * p[12] + t4 * u2 * p[13] + t4 * u3 * p[14] + t4 * u4 * p[15];

                            list.Add(point);
                        }
                    }
                }
            }

            return list;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Name);
            sb.Append(" " + (_countU - 3));
            sb.Append(" " + (_countV - 3));
            _points.ForEach(p => sb.Append(" " + p.ToString()));
            return sb.ToString();
        }

        public BSplineSurface(string s, bool isTube)
        {
            var l = s.Split(' ');
            Name = l[0];
            _countU = int.Parse(l[1]) + 3;
            _countV = int.Parse(l[2]) + 3;
            _points = l.Skip(3).Select(x => new DrawablePoint(x)).ToList();
            _isTube = isTube;
        }

        private Vector4[] GetDeBoorPoints(int patchU, int patchV)
        {
            var points = new Vector4[16];
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                {
                    var indU = patchU - i;

                    var indV = patchV - j;
                    if (indV < 0)
                        if (_isTube)
                            indV = _countV + indV;

                    points[4 * i + j] = _points[indU * _countV + indV].Point;
                }

            return points;
        }

        public bool Selected { get; set; }
        public void AddPoint(DrawablePoint point)
        {
        }

        static int Nr = 1;
        public string Name { get; set; } = "Bspline_surface" + Nr++;

        public bool AllPointsSelected
        {
            get => _points.All(x => x.Selected);
            set => _points.ForEach(x => x.Selected = value);
        }

    }
}
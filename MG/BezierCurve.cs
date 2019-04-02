using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MG
{
    class BezierCurve : IDrawableObject, ISublistContaining
    {
        private static int _index;

        private readonly List<DrawablePoint> _points = new List<DrawablePoint>();

        public void AddPoint(DrawablePoint point)
        {
            if (point != null && !_points.Contains(point))
                _points.Add(point);
        }

        public bool Selected { get; set; }
        public bool DrawLines { get; set; }
        public string Name { get; set; } = "Bezier curve " + ++_index;

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
            var list = new List<Vector4>();
            var diff = 1.0f / (count - 1);
            var coordinates = _points.Select(x => new Vector3(x.X, x.Y, x.Z)).ToArray();

            var currentPointIndex = 0;
            while (coordinates.Length - currentPointIndex >= 4)
            {
                list.AddRange(Enumerable.Range(0, count).Select(i =>
                    Point4C(coordinates[currentPointIndex], coordinates[currentPointIndex + 1], coordinates[currentPointIndex + 2], coordinates[currentPointIndex + 3], i * diff)));
                currentPointIndex += 3;
            }

            if (coordinates.Length - currentPointIndex == 3)
                list.AddRange(Enumerable.Range(0, count).Select(i =>
                    Point3C(coordinates[currentPointIndex], coordinates[currentPointIndex + 1], coordinates[currentPointIndex + 2], i * diff)));

            if (coordinates.Length - currentPointIndex == 2)
                list.AddRange(Enumerable.Range(0, count).Select(i =>
                    Point2C(coordinates[currentPointIndex], coordinates[currentPointIndex + 1], i * diff)));

            return list;
        }

        private static Vector4 Point2C(Vector3 p1, Vector3 p2, float t) => new Vector4(p1 * (1 - t) + p2 * t, 1.0f);

        private static Vector4 Point3C(Vector3 p1, Vector3 p2, Vector3 p3, float t)
            => new Vector4(p1 * (1 - t) * (1 - t) + p2 * 2 * t * (1 - t) + p3 * t * t, 1.0f);

        public static Vector4 Point4C(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float t)
            => new Vector4(
                p1 * (1 - t) * (1 - t) * (1 - t) + p2 * 3 * t * (1 - t) * (1 - t) + p3 * 3 * (1 - t) * t * t +
                p4 * t * t * t, 1.0f);

        public IReadOnlyList<object> List => _points;
        public void RemoveObject(object o)
        {
            _points.Remove(o as DrawablePoint);
        }
    }
}

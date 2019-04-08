using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MG
{
    class FrenetCurve:IDrawableObject, ICurve
    {
        float r = 1.0f;
        float p = 1.0f;

        public Matrix4x4 GetModelMatrix()
        {
            return Matrix4x4.Identity;
        }

        public Tuple<Line[], Vector4[]> GetLines()
        {
            float alpha = DateTime.Now.Millisecond + DateTime.Now.Second%6 * 1000;
            alpha /= 1000;

            //alpha = 0;
            var sina = (float) Math.Sin(alpha);
            var cosa = (float) Math.Cos(alpha);
            var startPoint = new Vector4(r * cosa, r * sina, p * alpha, 1.0f);


            var lines = new[] {new Line(0, 1), new Line(0, 2), new Line(0, 3)};
            var div = (float)Math.Sqrt(r * r + p * p);
            var tangent = new Vector4(-r * sina / div, r * cosa / div, p / div, 0.0f);
            var p1 = startPoint + tangent;

            var normal = new Vector4(-cosa, -sina, 0.0f, 0.0f);
            var p2 = startPoint + normal;

            var binormal = new Vector4(p * sina / div, -p * cosa / div, r / div, 0.0f);
            var p3 = startPoint + binormal;
            var points = new[] {startPoint, p1, p2, p3};
            return Tuple.Create(lines, points);
        }

        public bool DrawLines { get; set; }
        public List<Vector4> GetPoints(int count)
        {
            var points = new List<Vector4>();
            for (var alpha = 0.0f; alpha < Math.PI * 2; alpha += 0.01f)
                points.Add(new Vector4(r * (float) Math.Cos(alpha), r * (float) Math.Sin(alpha), p * alpha, 1.0f));

            return points;
        }

        public bool Selected { get; set; }
        public void AddPoint(DrawablePoint point)
        {
        }
    }
}

using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace MG
{
    public class DrawablePoint
    {
        private static int _pointsCount = 0;

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector4 Point
        {
            get => new Vector4(X, Y, Z, 1.0f);
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }

        public Vector4 FrozenPosition { get; set; }
        public bool Selected { get; set; }
        public bool Grabbed { get; set; }

        public string Name { get; set; } = "Point" + ++_pointsCount;

        public DrawablePoint(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public DrawablePoint()
        {
        }

        public DrawablePoint(string s)
        {
            var l = s.Split(';').Where(s1 => !string.IsNullOrWhiteSpace(s1)).ToList();

            if (l.Count > 3)
                Name = l[3];

            float x = 0, y = 0, z = 0;
            if (!float.TryParse(l[0], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out x))
                float.TryParse(l[0], out x);

            if (!float.TryParse(l[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out y))
                float.TryParse(l[1], out y);

            if (!float.TryParse(l[2], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out z))
                float.TryParse(l[2], out z);
        
            X = x;
            Y = y;
            Z = z;
        }

        public Point ScreenPosition { get; set; }

        public override string ToString()
        {
            return $"{X};{Y};{Z};{Name}";
        }
    }
}
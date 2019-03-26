using System.Drawing;
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

        public string Name { get; set; }

        public DrawablePoint(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
            Name = "Point " + ++_pointsCount;
        }

        public DrawablePoint()
        {
            Name = "Point " + ++_pointsCount;
        }

        public Point ScreenPosition { get; set; }
    }
}
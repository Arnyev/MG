using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MG
{
    public class DirectBitmap : IDisposable
    {
        public Bitmap Bitmap { get; }
        public byte[] Bits { get; }
        public bool Disposed { get; private set; }
        public int Height { get; }
        public int Width { get; }

        protected GCHandle BitsHandle { get; }

        public DirectBitmap(int width, int height)
        {
            Width = width;
            Height = height;
            Bits = new byte[width * height * 4];
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, BitsHandle.AddrOfPinnedObject());
            for (int i = 0; i < width * height; i++)
                Bits[4 * i + 3] = 255;
        }

        public DirectBitmap(Bitmap bitmap) : this(bitmap.Width, bitmap.Height)
        {
            using (var graphics = Graphics.FromImage(Bitmap))
            {
                graphics.DrawImage(bitmap, new Point(0, 0));
            }
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Bitmap.Dispose();
            BitsHandle.Free();
        }

        public void SetPixel(int x, int y, MyColor color)
        {
            var index = (y * Width + x) * 4;
            if (index >= Bits.Length || index < 0)
                return;
            Bits[index] = color.B;
            Bits[index + 1] = color.G;
            Bits[index + 2] = color.R;
        }

        public MyColor GetPixel(int x, int y)
        {
            var index = (y * Width + x) * 4;
            if (index > Bits.Length || index < 0)
                return new MyColor();
            return new MyColor(Bits[index + 2], Bits[index + 1], Bits[index]);
        }

        public void DrawLine(Point p1, Point p2, MyColor color, bool addColors = false)
        {
            if (addColors)
            {
                AppendLine(p1, p2, color);
                return;
            }

            var differencePoint = new Point(p2.X - p1.X, p2.Y - p1.Y);
            var octant = FindOctant(differencePoint);

            var mappedDifference = MapInput(octant, differencePoint.X, differencePoint.Y);

            var dx = mappedDifference.X;
            var dy = mappedDifference.Y;
            var d = 2 * dy - dx;
            var y = 0;

            for (int x = 0; x <= mappedDifference.X; x++)
            {
                var p = MapOutput(octant, x, y);
                var newPointX = p.X + p1.X;
                var newPointY = p.Y + p1.Y;

                if (newPointX >= 0 && newPointX < Width && newPointY >= 0 && newPointY < Height)
                {
                    var index = (newPointY * Width + newPointX) * 4;

                    Bits[index] = color.B;
                    Bits[index + 1] = color.G;
                    Bits[index + 2] = color.R;
                }

                if (d > 0)
                {
                    y = y + 1;
                    d = d - 2 * dx;
                }

                d = d + 2 * dy;
            }
        }

        private void AppendLine(Point p1, Point p2, MyColor color)
        {
            var differencePoint = new Point(p2.X - p1.X, p2.Y - p1.Y);
            var octant = FindOctant(differencePoint);

            var mappedDifference = MapInput(octant, differencePoint.X, differencePoint.Y);

            var dx = mappedDifference.X;
            var dy = mappedDifference.Y;
            var d = 2 * dy - dx;
            var y = 0;

            for (int x = 0; x <= mappedDifference.X; x++)
            {
                var p = MapOutput(octant, x, y);
                var newPointX = p.X + p1.X;
                var newPointY = p.Y + p1.Y;

                if (newPointX >= 0 && newPointX < Width && newPointY >= 0 && newPointY < Height)
                {
                    var index = (newPointY * Width + newPointX) * 4;

                    var blue = (byte)Math.Min(255, Bits[index] + color.B);
                    var green = (byte)Math.Min(255, Bits[index + 1] + color.G);
                    var red = (byte)Math.Min(255, Bits[index + 2] + color.R);

                    Bits[index] = blue;
                    Bits[index + 1] = green;
                    Bits[index + 2] = red;
                }

                if (d > 0)
                {
                    y = y + 1;
                    d = d - 2 * dx;
                }

                d = d + 2 * dy;
            }
        }

        public static int FindOctant(Point end)
        {
            var dx = end.X;
            var dy = -end.Y;

            if (dx >= 0 && dy >= 0)
                return dy > dx ? 1 : 0;

            if (dx < 0 && dy >= 0)
                return dy > -dx ? 2 : 3;

            if (dx < 0 && dy < 0)
                return dy > dx ? 4 : 5;

            return -dy > dx ? 6 : 7;
        }

        public static Point MapInput(int octant, int x, int y)
        {
            switch (octant)
            {
                case 0: return new Point(x, -y);
                case 1: return new Point(-y, x);
                case 2: return new Point(-y, -x);
                case 3: return new Point(-x, -y);
                case 4: return new Point(-x, y);
                case 5: return new Point(y, -x);
                case 6: return new Point(y, x);
                case 7: return new Point(x, y);
            }

            return new Point();
        }

        public static Point MapOutput(int octant, int x, int y)
        {
            switch (octant)
            {
                case 0: return new Point(x, -y);
                case 1: return new Point(y, -x);
                case 2: return new Point(-y, -x);
                case 3: return new Point(-x, -y);
                case 4: return new Point(-x, y);
                case 5: return new Point(-y, x);
                case 6: return new Point(y, x);
                case 7: return new Point(x, y);
            }

            return new Point();
        }
    }

    public struct MyColor
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;

        public MyColor(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }
    }
}

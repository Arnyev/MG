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
            if (index > Bits.Length || index < 0)
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

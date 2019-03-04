using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MG
{
    public partial class MainForm : Form
    {
        private readonly Camera _camera;
        private readonly Pipeline _pipeline;
        private readonly ObjectsController _controller;
        private DirectBitmap _bitmap;
        private Graphics _graphics;
        private const double Fov = Math.PI / 3;
        private const double Near = 2;
        private const double Far = 100.0;
        private DateTime _lastTimeDrawn;

        public MainForm()
        {
            InitializeComponent();
            KeyPreview = true;
            KeyPress += MainForm_KeyPress;
            CenterToScreen();

            _bitmap = new DirectBitmap(pictureBox1.Width, pictureBox1.Height);
            pictureBox1.Image = _bitmap.Bitmap;
            _graphics = Graphics.FromImage(_bitmap.Bitmap);

            _camera = new Camera(pictureBox1, this);
            _controller = new ObjectsController(propertyGrid1, listBox1, flowLayoutPanel1);
            _pipeline = new Pipeline(_camera, Fov, Near, Far, pictureBox1, _controller);

            var timer = new Timer { Interval = 1000 };
            timer.Tick += Timer_Tick;
            timer.Start();

            pictureBox1.SizeChanged += PictureBox1_Resize;
        }

        private void PictureBox1_Resize(object sender, EventArgs e)
        {
            _bitmap = new DirectBitmap(pictureBox1.Width, pictureBox1.Height);
            pictureBox1.Image = _bitmap.Bitmap;
            _graphics = Graphics.FromImage(_bitmap.Bitmap);
        }

        private void MainForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '1')
                return;

            if (pictureBox1.Focused)
            {
                listBox1.Focus();
                Cursor.Show();
                return;
            }

            _camera.SetCursorInMiddle();
            pictureBox1.Focus();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Redraw(true);
        }

        public void Redraw(bool timered = false)
        {
            var diff = DateTime.Now - _lastTimeDrawn;
            if (diff.Milliseconds < 20)
                return;

            _lastTimeDrawn = DateTime.Now;
            _camera.UpdatePosition();
            Task.Factory.StartNew(RaycastingTask);
            //_pipeline.Redraw();
        }

        private void RaycastingTask()
        {
            var scalingFactor = _bitmap.Width / 100;
            int frameNumber;
            lock (_lockObjectFrame)
            {
                frameNumber = ++_frameNumber;
            }

            while (scalingFactor > 0)
            {
                var bitmap = new DirectBitmap(_bitmap.Width / scalingFactor, _bitmap.Height / scalingFactor);
                var rayCaster = new RayCaster(bitmap, _camera, Fov);
                rayCaster.Draw(_controller.RaycastingParameters);

                lock (_lockObject)
                {
                    lock (_lockObjectFrame)
                    {
                        if (frameNumber != _frameNumber)
                        {
                            bitmap.Dispose();
                            return;
                        }
                    }

                    pictureBox1.Invoke(new MethodInvoker(() => CopyImage(bitmap)));
                }

                bitmap.Dispose();
                scalingFactor /= 2;
            }
        }

        private readonly object _lockObject = new object();
        private readonly object _lockObjectFrame = new object();
        private int _frameNumber;


        private void CopyImage(DirectBitmap bitmap)
        {
            var width = (float)pictureBox1.Image.Width;
            var height = (float)pictureBox1.Image.Height;

            _graphics.DrawImage(bitmap.Bitmap, new RectangleF(0, 0, width, height),
                new RectangleF(0, 0, bitmap.Width, bitmap.Height), GraphicsUnit.Pixel);

            pictureBox1.Refresh();
        }
    }
}

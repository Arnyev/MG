using System;
using System.Drawing;
using System.Windows.Forms;

namespace MG
{
    public partial class MainForm : Form
    {
        private readonly Camera _camera;
        private Pipeline _pipeline;
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
            KeyDown += MainForm_KeyDown;
            CenterToScreen();

            _bitmap = new DirectBitmap(pictureBox1.Width, pictureBox1.Height);
            pictureBox1.Image = _bitmap.Bitmap;
            _graphics = Graphics.FromImage(_bitmap.Bitmap);

            _camera = new Camera(pictureBox1, this);
            _cursor = new Cursor3D(_camera, pictureBox1, (float)Near, (float)Far);
            _controller = new ObjectsController(propertyGrid1, listBox1,listBox2, flowLayoutPanel1, _cursor);
            _pipeline = new Pipeline(_camera, Fov, Near, Far, _controller, _bitmap, _cursor);

            _manipulator = new PointManipulator(_controller, (float)Fov, pictureBox1, _camera);

            var timer = new Timer { Interval = 10 };
            timer.Tick += Timer_Tick;
            timer.Start();

            pictureBox1.SizeChanged += PictureBox1_Resize;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F1:
                    if (pictureBox1.Focused)
                    {
                        listBox1.Focus();
                        Cursor.Show();
                        return;
                    }

                    _camera.SetCursorInMiddle();
                    pictureBox1.Focus();
                    break;
                case Keys.F2:
                    _pipeline.IsAnaglyphic = !_pipeline.IsAnaglyphic;
                    break;

            }
        }

        private void PictureBox1_Resize(object sender, EventArgs e)
        {
            _bitmap = new DirectBitmap(pictureBox1.Width, pictureBox1.Height);
            pictureBox1.Image = _bitmap.Bitmap;
            _graphics = Graphics.FromImage(_bitmap.Bitmap);
            _pipeline = new Pipeline(_camera, Fov, Near, Far, _controller, _bitmap, _cursor);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Redraw(true);
            if (pictureBox1.Focused)
                propertyGrid1.Refresh();
        }

        public void Redraw(bool timered = false)
        {
            //var diff = DateTime.Now - _lastTimeDrawn;
            //if (diff.Milliseconds < 10)
            //    return;

            _lastTimeDrawn = DateTime.Now;
            _camera.UpdatePosition();
            //Task.Factory.StartNew(RaycastingTask);

            _manipulator.Update();
            _pipeline.Redraw();
            pictureBox1.Refresh();
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
        private readonly Cursor3D _cursor;
        private readonly PointManipulator _manipulator;

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

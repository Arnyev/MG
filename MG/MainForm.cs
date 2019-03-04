using System;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;

namespace MG
{
    public partial class MainForm : Form
    {
        private readonly Camera _camera;
        private readonly Pipeline _pipeline;
        private RayCaster _rayCaster;
        private ObjectsController _controller;
        private const double Fov = Math.PI / 3;
        private const double Near = 2;
        private const double Far = 100.0;

        public MainForm()
        {
            InitializeComponent();
            KeyPreview = true;
            KeyPress += MainForm_KeyPress;
            CenterToScreen();

            var bitmap = new DirectBitmap(pictureBox1.Width, pictureBox1.Height);
            pictureBox1.Image = bitmap.Bitmap;

            _camera = new Camera(pictureBox1);
            _controller = new ObjectsController(propertyGrid1, listBox1, flowLayoutPanel1);
            _pipeline = new Pipeline(_camera, Fov, Near, Far, pictureBox1, _controller);
            _rayCaster = new RayCaster(bitmap, _camera, Fov);

            var timer = new Timer();
            timer.Interval = 10;
            timer.Tick += Timer_Tick;
            timer.Start();

            pictureBox1.SizeChanged += PictureBox1_Resize;

        }

        private void PictureBox1_Resize(object sender, EventArgs e)
        {
            var bitmap = new DirectBitmap(pictureBox1.Width, pictureBox1.Height);
            pictureBox1.Image = bitmap.Bitmap;
            _rayCaster = new RayCaster(bitmap, _camera, Fov);
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
            _camera.UpdatePosition();
            _rayCaster.DisplayRaycasted(_controller.RaycastingParameters);
            //_pipeline.Redraw();
            pictureBox1.Refresh();
        }
    }
}

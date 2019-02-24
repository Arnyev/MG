using System;
using System.Drawing;
using System.Windows.Forms;

namespace MG
{
    public partial class MainForm : Form
    {
        private readonly Camera _camera;
        private readonly Pipeline _pipeline;
        public MainForm()
        {
            InitializeComponent();
            KeyPreview = true;
            KeyPress += MainForm_KeyPress;
            CenterToScreen();

            var bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            pictureBox1.Image = bitmap;
            _camera = new Camera(pictureBox1);
            _pipeline = new Pipeline(_camera, 16.0 / 9, Math.PI / 3, 2, 50, pictureBox1, new DrawableObjectsController(propertyGrid1, listBox1, flowLayoutPanel1));

            var timer = new Timer();
            timer.Interval = 10;
            timer.Tick += Timer_Tick;
            timer.Start();

            pictureBox1.SizeChanged += PictureBox1_Resize;
        }

        private void PictureBox1_Resize(object sender, EventArgs e)
        {
            var bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            pictureBox1.Image = bitmap;
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
            _pipeline.Redraw();
            pictureBox1.Refresh();
        }
    }
}

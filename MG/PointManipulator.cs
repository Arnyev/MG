using System;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;

namespace MG
{
    class PointManipulator
    {
        private readonly ObjectsController _controller;
        private readonly Camera _camera;

        public PointManipulator(ObjectsController controller, float fov, PictureBox box, Camera camera)
        {
            _controller = controller;
            _camera = camera;
            box.MouseClick += _box_MouseClick;
            box.MouseDown += Box_MouseDown;
            box.MouseUp += Box_MouseUp;
        }

        private void Box_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            _controller.Points.Where(x => x.Selected).ToList().ForEach(x => x.Grabbed = false);
        }

        private void Box_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            var points = _controller.Points.Where(x => x.Selected).ToList();
            var viewMatrix = _camera.ViewMatrix;
            points.ForEach(x => x.FrozenPosition = Vector4.Transform(x.Point, viewMatrix));
            points.ForEach(x => x.Grabbed = true);
        }

        public void Update()
        {
            var invViewMatrix = _camera.InverseViewMatrix;
            _controller.Points.Where(x => x.Grabbed).ToList()
                .ForEach(x => x.Point = Vector4.Transform(x.FrozenPosition, invViewMatrix));
        }

        private void _box_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                LeftClick(e);
        }

        private void LeftClick(MouseEventArgs e)
        {
            var points = _controller.Points;
            if (points.Count == 0)
                return;

            var distances = points.Select((p, i) =>
                    Tuple.Create(
                        (p.ScreenPosition.X - e.X) * (p.ScreenPosition.X - e.X) +
                        (p.ScreenPosition.Y - e.Y) * (p.ScreenPosition.Y - e.Y), i))
                .Where(p => p.Item1 < 40)
                .OrderBy(p=>p.Item1)
                .ToList();

            if (distances.Count > 0)
                points[distances[0].Item2].Selected = !points[distances[0].Item2].Selected;
        }
    }
}

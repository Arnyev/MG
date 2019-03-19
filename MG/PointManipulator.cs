using System;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;

namespace MG
{
    class PointManipulator
    {
        private readonly ObjectsController _controller;
        private readonly int _width;
        private readonly int _height;
        private readonly float _fov;
        private readonly Camera _camera;
        private readonly float _coneWidth;
        private bool _buttonPressed;

        public PointManipulator(ObjectsController controller, int width, int height, float fov, PictureBox box, Camera camera, float coneWidth)
        {
            _controller = controller;
            _width = width;
            _height = height;
            _fov = fov;
            _camera = camera;
            _coneWidth = coneWidth;
            box.MouseClick += _box_MouseClick;
            box.MouseDown += Box_MouseDown;
            box.MouseUp += Box_MouseUp;
        }

        private void Box_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            _buttonPressed = false;
            _controller.Points.Where(x => x.Selected).ToList().ForEach(x => x.Grabbed = false);
        }

        private void Box_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            _buttonPressed = true;
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

            var direction = new Vector4(0, 0, 1, 0);
            float aspect = (float)_height / _width;
            float s = (float)(-2.0 * Math.Tan(_fov * 0.5));
            direction.Y = -((float)e.Y / _height - 0.5f) * s * aspect;
            direction.X = ((float)e.X / _width - 0.5f) * s;
            var directionWorld = Vector4.Normalize(Vector4.Transform(direction, _camera.InverseViewMatrix));
            var directionWorld3 = new Vector3(directionWorld.X, directionWorld.Y, directionWorld.Z);
            var matrix = _camera.LookAt(directionWorld3);
            var pointsLineWorld = points.Select((point, index) => Tuple.Create(index, Vector4.Transform(point.Point, matrix)))
                .Where(p => p.Item2.Z > 0)
                .ToList();
            if (pointsLineWorld.Count == 0)
                return;

            var pointsDistances = pointsLineWorld
                .Select(p => Tuple.Create(p.Item1, (p.Item2.X * p.Item2.X + p.Item2.Y * p.Item2.Y) / p.Item2.Z))
                .Where(x => x.Item2 < _coneWidth)
                .OrderBy(x => x.Item2).ToList();

            if (pointsDistances.Count > 0)
                points[pointsDistances[0].Item1].Selected = true;
        }
    }
}

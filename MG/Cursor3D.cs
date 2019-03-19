using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Forms;

namespace MG
{
    class Cursor3D : IDrawableObject
    {
        private const float ScrollSpeed = 10.0f;
        private readonly float _minDistance;
        private readonly float _maxDistance;
        private const float LineLength = 0.05f;

        private readonly Camera _camera;
        private float _distanceFromCamera;
        public Vector4 Position => Vector4.Transform(new Vector4(0, 0, -_distanceFromCamera, 1), _camera.InverseViewMatrix);
        public MyColor Color => new MyColor(255, 0, (byte)(_distanceFromCamera * (255 / _maxDistance)));

        public float X => Position.X;
        public float Y => Position.Y;
        public float Z => Position.Z;

        public Cursor3D(Camera camera, PictureBox box, float near, float far)
        {
            _camera = camera;
            _minDistance = near + 0.5f;
            _maxDistance = far - 0.5f;
            _distanceFromCamera = near;
            box.MouseWheel += Box_MouseWheel;
        }

        private void Box_MouseWheel(object sender, MouseEventArgs e)
        {
            _distanceFromCamera += e.Delta / ScrollSpeed;
            if (_distanceFromCamera > _maxDistance)
                _distanceFromCamera = _maxDistance;
            if (_distanceFromCamera < _minDistance)
                _distanceFromCamera = _minDistance;
        }

        public Matrix4x4 GetModelMatrix()
        {
            var matrix = Matrix4x4.CreateTranslation(0, 0, -_distanceFromCamera) * _camera.InverseViewMatrix;
            return matrix;
        }

        private static readonly Line[] Lines = { new Line(0, 1), new Line(2, 3), new Line(4, 5) };

        public Tuple<Line[], Vector4[]> GetLines()
        {
            var halfLen = LineLength * _distanceFromCamera / 2;
            var points = new Vector4[6];
            points[0] = new Vector4(-halfLen, 0, 0, 1);
            points[1] = new Vector4(halfLen, 0, 0, 1);
            points[2] = new Vector4(0, -halfLen, 0, 1);
            points[3] = new Vector4(0, halfLen, 0, 1);
            points[4] = new Vector4(0, 0, -halfLen, 1);
            points[5] = new Vector4(0, 0, halfLen, 1);
            return Tuple.Create(Lines, points);
        }
    }
}

using System;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;

namespace MG
{
    class Camera
    {
        private readonly PictureBox _box;
        private readonly MainForm _mainForm;
        private static readonly Vector3 Up = new Vector3(0, 1, 0);
        private const float MovementSpeed = 20.0f;
        private const float MouseSensitivity = 0.0005f;
        private Vector3 _position = new Vector3(0, 0, -10);
        private DateTime _lastComputationTime;
        private int _wPressed;
        private int _sPressed;
        private int _aPressed;
        private int _dPressed;
        private float _pitchRotation;
        private float _yawRotation;
        private Vector3 CurrentDirection
        {
            get
            {
                var z = Math.Cos(_pitchRotation) * Math.Cos(_yawRotation);
                var x = Math.Cos(_pitchRotation) * Math.Sin(_yawRotation);
                var y = Math.Sin(_pitchRotation);
                return new Vector3((float)x, (float)y, (float)z);
            }
        }

        public Camera(PictureBox box, MainForm mainForm)
        {
            _box = box;
            _mainForm = mainForm;
            box.MouseMove += Box_MouseMove;
            box.KeyDown += Box_KeyDown;
            box.KeyUp += Box_KeyUp;
            box.LostFocus += Box_LostFocus;
        }

        public Vector4 Position => new Vector4(_position, 1.0f);

        public Matrix4x4 GetViewMatrix()
        {
            var back = -CurrentDirection;
            var left = Vector3.Normalize(Vector3.Cross(Up, back));
            var up = Vector3.Cross(back, left);

            Matrix4x4 result;

            result.M11 = left.X;
            result.M12 = up.X;
            result.M13 = back.X;
            result.M14 = 0.0f;
            result.M21 = left.Y;
            result.M22 = up.Y;
            result.M23 = back.Y;
            result.M24 = 0.0f;
            result.M31 = left.Z;
            result.M32 = up.Z;
            result.M33 = back.Z;
            result.M34 = 0.0f;
            result.M41 = -Vector3.Dot(left, _position);
            result.M42 = -Vector3.Dot(up, _position);
            result.M43 = -Vector3.Dot(back, _position);
            result.M44 = 1.0f;

            return result;
        }

        private void UpdateMovementDirection(Keys keyPressed, int valueUpDown)
        {
            switch (keyPressed)
            {
                case Keys.A:
                    _aPressed = valueUpDown;
                    break;
                case Keys.D:
                    _dPressed = valueUpDown;
                    break;
                case Keys.S:
                    _sPressed = valueUpDown;
                    break;
                case Keys.W:
                    _wPressed = valueUpDown;
                    break;
            }
        }

        public void UpdatePosition()
        {
            var now = DateTime.Now;
            var span = now - _lastComputationTime;
            _lastComputationTime = now;

            var duration = (float)(span.Hours * 3600.0 + span.Minutes * 60.0 + span.Seconds + span.Milliseconds / 1000.0);
            var durationScaled = duration * MovementSpeed;
            var zaxis = CurrentDirection;
            var xaxis = Vector3.Normalize(Vector3.Cross(Up, zaxis));

            _position += durationScaled * zaxis * (_wPressed - _sPressed);
            _position += durationScaled * xaxis * (_aPressed - _dPressed);
        }

        private void Box_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_box.Focused)
                return;

            var centerX = _box.Width / 2;
            var centerY = _box.Height / 2;
            var diffX = e.X - centerX;
            var diffY = e.Y - centerY;
            if (diffY == 0 && diffX == 0)
                return;

            SetCursorInMiddle();

            _yawRotation -= diffX * MouseSensitivity;
            _pitchRotation += diffY * MouseSensitivity;
            _mainForm.Redraw();
        }

        public void SetCursorInMiddle()
        {
            Cursor.Position = _box.PointToScreen(new Point(_box.Width / 2, _box.Height / 2));
        }

        private void Box_LostFocus(object sender, EventArgs e)
        {
            _wPressed = _sPressed = _aPressed = _dPressed = 0;
            _mainForm.Redraw();
        }

        private void Box_KeyDown(object sender, KeyEventArgs e)
        {
            UpdateMovementDirection(e.KeyCode, 1);
            _mainForm.Redraw();
        }

        private void Box_KeyUp(object sender, KeyEventArgs e)
        {
            UpdateMovementDirection(e.KeyCode, 0);
            _mainForm.Redraw();
        }
    }
}

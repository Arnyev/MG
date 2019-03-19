using System;
using System.Drawing;
using System.Numerics;

namespace MG
{
    class Pipeline
    {
        private readonly Camera _camera;
        private readonly float _fov;
        private readonly float _near;
        private readonly float _far;
        private readonly ObjectsController _objectsController;
        private readonly DirectBitmap _bitmap;
        private readonly Cursor3D _cursor;

        public bool IsAnaglyphic { get; set; }

        public Pipeline(Camera camera, double fov, double near, double far, ObjectsController objectsController, DirectBitmap bitmap, Cursor3D cursor)
        {
            _camera = camera;
            _fov = (float)fov;
            _near = (float)near;
            _far = (float)far;
            _objectsController = objectsController;
            _bitmap = bitmap;
            _cursor = cursor;
        }

        public void Redraw()
        {
            const float eyesep = 0.15f;
            const float viewDist = 3f;

            using (var g = Graphics.FromImage(_bitmap.Bitmap))
            {
                g.Clear(Color.Black);
            }

            if (IsAnaglyphic)
            {
                var left = StereoscopicMatrixL((float)_bitmap.Width / _bitmap.Height, _fov, _near, _far, eyesep,
                    viewDist);

                DrawSinglePass(left, new MyColor(255, 0, 0), true);

                var right = StereoscopicMatrixR((float)_bitmap.Width / _bitmap.Height, _fov, _near, _far, eyesep,
                    viewDist);

                DrawSinglePass(right, new MyColor(0, 255, 255), true);
            }
            else
            {
                var perspectiveMatrix = PerspectiveMatrix((float)_bitmap.Width / _bitmap.Height, _fov, _near, _far);
                DrawSinglePass(perspectiveMatrix, new MyColor(255, 255, 255), false);
                var middleX = _bitmap.Width / 2;
                var middleY = _bitmap.Height / 2;

                _bitmap.DrawLine(new Point(middleX - 10, middleY), new Point(middleX + 10, middleY), _cursor.Color);
                _bitmap.DrawLine(new Point(middleX, middleY - 10), new Point(middleX, middleY + 10), _cursor.Color);
            }
        }

        private void DrawSinglePass(Matrix4x4 perspectiveMatrix, MyColor color, bool shouldAppend)
        {
            var viewportMatrix = GetViewportMatrix(_bitmap.Width, _bitmap.Height);
            var viewMatrix = _camera.ViewMatrix;
            var matrixViewProj = viewMatrix * perspectiveMatrix;

            foreach (var torus in _objectsController.DrawableObjects)
            {
                var mvp = torus.GetModelMatrix() * matrixViewProj;
                var k = torus.GetModelMatrix() * _camera.ViewMatrix;
                var tuple = torus.GetLines();
                var pointsLines = tuple.Item2;

                var newPoints = new Vector4[pointsLines.Length];
                for (int i = 0; i < pointsLines.Length; i++)
                {
                    var transformed = Vector4.Transform(pointsLines[i], mvp);
                    newPoints[i] = transformed / transformed.W;
                }

                foreach (var line in tuple.Item1)
                {
                    var p1 = newPoints[line.Start];
                    var p2 = newPoints[line.End];
                    if (!(p1.X > -1) || !(p1.X < 1) || !(p1.Y > -1) || !(p1.Y < 1) || !(p1.Z > -1) || !(p1.Z < 1) ||
                        !(p2.X > -1) || !(p2.X < 1) || !(p2.Y > -1) || !(p2.Y < 1) || !(p2.Z > -1) ||
                        !(p2.Z < 1)) continue;

                    p1 = Vector4.Transform(p1, viewportMatrix);
                    p2 = Vector4.Transform(p2, viewportMatrix);
                    _bitmap.DrawLine(new Point((int)p1.X, (int)p1.Y), new Point((int)p2.X, (int)p2.Y), color, shouldAppend);
                }
            }

            foreach (var point in _objectsController.Points)
            {
                var transformed = Vector4.Transform(point.Point, matrixViewProj);
                var p = transformed / transformed.W;
                if (!(p.X > -1) || !(p.X < 1) || !(p.Y > -1) || !(p.Y < 1) || !(p.Z > -1) || !(p.Z < 1))
                    continue;

                var screenPoint = Vector4.Transform(p, viewportMatrix);
                if (screenPoint.X < 2 || screenPoint.X > _bitmap.Width - 2 || screenPoint.Y < 2 ||
                    screenPoint.Y > _bitmap.Height - 2)
                    continue;

                _bitmap.SetPixel((int)screenPoint.X - 1, (int)screenPoint.Y, color);
                _bitmap.SetPixel((int)screenPoint.X, (int)screenPoint.Y + 1, color);
                _bitmap.SetPixel((int)screenPoint.X + 1, (int)screenPoint.Y, color);
                _bitmap.SetPixel((int)screenPoint.X, (int)screenPoint.Y - 1, color);
                _bitmap.SetPixel((int)screenPoint.X, (int)screenPoint.Y, color);
            }
        }

        private static Matrix4x4 GetViewportMatrix(int width, int height)
        {
            Matrix4x4 viewportMatrix = new Matrix4x4
            {
                M11 = width / 2.0f,
                M22 = height / 2.0f,
                M33 = 0.5f,
                M44 = 1,
                M41 = width / 2.0f,
                M42 = height / 2.0f,
                M43 = 0.5f
            };

            return viewportMatrix;
        }

        private static Matrix4x4 Frustrum(float left, float right, float bottom, float top, float near, float far)
        {
            var m = new Matrix4x4();
            m.M11 = 2 * near / (right - left);
            m.M22 = 2 * near / (top - bottom);
            m.M31 = (right + left) / (right - left);
            m.M32 = (top + bottom) / (top - bottom);
            m.M33 = -(far + near) / (far - near);
            m.M43 = -2 * far * near / (far - near);
            m.M34 = -1;
            return m;
        }

        private static Matrix4x4 StereoscopicMatrixL(float aspectRatio, float fov, float near, float far, float eye, float screen)
        {
            float tan = (float)Math.Tan(fov / 2);
            var top = near * tan;
            var bottom = -top;
            var a = aspectRatio * tan * screen;
            var b = a - eye / 2;
            var c = a + eye / 2;
            var left = -b * near / screen;
            var right = c * near / screen;
            var translateMatrix = Matrix4x4.CreateTranslation(eye / 2, 0, 0);
            var projectionMatrix = Frustrum(left, right, bottom, top, near, far);
            return translateMatrix * projectionMatrix;
        }

        private static Matrix4x4 StereoscopicMatrixR(float aspectRatio, float fov, float near, float far, float eye, float screen)
        {
            float tan = (float)Math.Tan(fov / 2);
            var top = near * tan;
            var bottom = -top;
            var a = aspectRatio * tan * screen;
            var b = a - eye / 2;
            var c = a + eye / 2;
            var left = -c * near / screen;
            var right = b * near / screen;
            var translateMatrix = Matrix4x4.CreateTranslation(-eye / 2, 0, 0);
            var projectionMatrix = Frustrum(left, right, bottom, top, near, far);
            return translateMatrix * projectionMatrix;
        }

        private static Matrix4x4 PerspectiveMatrix(double aspectRatio, double fov, double near, double far)
        {
            var perspectiveMatrix = new Matrix4x4();
            var ctan = 1 / Math.Tan(fov / 2);
            perspectiveMatrix.M11 = (float)(ctan / aspectRatio);
            perspectiveMatrix.M22 = (float)ctan;
            perspectiveMatrix.M33 = (float)(far / (near - far));
            perspectiveMatrix.M34 = -1;
            perspectiveMatrix.M43 = (float)(far * near / (near - far));
            return perspectiveMatrix;
        }
    }
}

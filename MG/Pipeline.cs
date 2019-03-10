using System;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;

namespace MG
{
    class Pipeline
    {
        private readonly Camera _camera;
        private readonly float _fov;
        private float _near;
        private float _far;
        private readonly PictureBox _pictureBox;
        private readonly ObjectsController _objectsController;
        private readonly DirectBitmap _bitmap;

        public Pipeline(Camera camera, double fov, double near, double far, PictureBox pictureBox, ObjectsController objectsController, DirectBitmap bitmap)
        {
            _camera = camera;
            _fov = (float)fov;
            _near = (float)near;
            _far = (float)far;
            _pictureBox = pictureBox;
            _objectsController = objectsController;
            _bitmap = bitmap;
        }

        public void Redraw()
        {
            var perspectiveMatrix = PerspectiveMatrix((float)_pictureBox.Width / _pictureBox.Height, _fov, _near, _far);

            using (var g = Graphics.FromImage(_pictureBox.Image))
            {
                g.Clear(Color.Black);
            }

            var eyesep = 0.15f;
            var t = 0.1f;
            var viewDist = 3f;
            _near = 1.0f;
            var pen = Pens.Red;
            var p1 = StereoscopicMatrixL((float)_pictureBox.Width / _pictureBox.Height, _fov, _near, _far, eyesep);
            var p3 = StereoscopicMatrixL2((float)_pictureBox.Width / _pictureBox.Height, _fov, _near, _far, eyesep, viewDist, t);
            var p5 = StereoscopicMatrixL3((float)_pictureBox.Width / _pictureBox.Height, _fov, _near, _far, eyesep, viewDist);

            DrawSinglePass(p5, new MyColor(255, 0, 0));

            var pen3 = new Pen(Color.FromArgb(0, 200, 255));
            var pen2 = Pens.Blue;
            var p2 = StereoscopicMatrixR((float)_pictureBox.Width / _pictureBox.Height, _fov, _near, _far, eyesep);
            var p4 = StereoscopicMatrixR2((float)_pictureBox.Width / _pictureBox.Height, _fov, _near, _far, eyesep, viewDist, t);
            var p6 = StereoscopicMatrixR3((float)_pictureBox.Width / _pictureBox.Height, _fov, _near, _far, eyesep, viewDist);

            DrawSinglePass(p6, new MyColor(0, 255, 255));
        }

        private void DrawSinglePass(Matrix4x4 perspectiveMatrix, MyColor color)
        {
            var viewportMatrix = GetViewportMatrix(_pictureBox.Width, _pictureBox.Height);
            var viewMatrix = _camera.GetViewMatrix();
            var matrixViewProj = viewMatrix * perspectiveMatrix;

            using (var g = Graphics.FromImage(_pictureBox.Image))
            {
                foreach (var torus in _objectsController.DrawableObjects)
                {
                    var modelMatrix = torus.GetModelMatrix();
                    var mvp = modelMatrix * matrixViewProj;
                    var tuple = torus.GetLines();
                    var lines = tuple.Item1;
                    var points = tuple.Item2;

                    var newPoints = new Vector4[points.Length];
                    for (int i = 0; i < points.Length; i++)
                    {
                        var transformed = Vector4.Transform(points[i], mvp);
                        newPoints[i] = transformed / transformed.W;
                    }

                    foreach (var line in lines)
                    {
                        var p1 = newPoints[line.Start];
                        var p2 = newPoints[line.End];
                        if (!(p1.X > -1) || !(p1.X < 1) || !(p1.Y > -1) || !(p1.Y < 1) || !(p1.Z > -1) || !(p1.Z < 1) ||
                            !(p2.X > -1) || !(p2.X < 1) || !(p2.Y > -1) || !(p2.Y < 1) || !(p2.Z > -1) ||
                            !(p2.Z < 1)) continue;

                        p1 = Vector4.Transform(p1, viewportMatrix);
                        p2 = Vector4.Transform(p2, viewportMatrix);
                        _bitmap.AppendLine(new Point((int)p1.X, (int)p1.Y), new Point((int)p2.X, (int)p2.Y), color);
                        //g.DrawLine(Pens.White, p1.X, p1.Y, p2.X, p2.Y);
                    }
                }
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

        private static Matrix4x4 StereoscopicMatrixL(double aspectRatio, double fov, double near, double far, double eyeSeparation)
        {
            var width = 2 * near * aspectRatio * Math.Tan(fov / 2);
            var m = PerspectiveMatrix(aspectRatio, fov, near, far);
            m.M31 = (float)(eyeSeparation / width);
            m.M41 = (float)-eyeSeparation / 2;

            return m;
        }
        private static Matrix4x4 StereoscopicMatrixR(double aspectRatio, double fov, double near, double far, double eyeSeparation)
        {
            var width = 2 * near * aspectRatio * Math.Tan(fov / 2);
            var m = PerspectiveMatrix(aspectRatio, fov, near, far);
            m.M31 = (float)(eyeSeparation / width);
            m.M41 = (float)eyeSeparation / 2;

            return m;
        }

        private static Matrix4x4 StereoscopicMatrixL2(double aspectRatio, double fov, double near, double far, double eyeSeparation, double screen, double interaxialCameraSeparation)
        {
            var width = screen * aspectRatio * Math.Tan(fov / 2);
            var m = new Matrix4x4();
            m.M11 = (float)(screen / width);
            m.M22 = (float)(screen * aspectRatio / width);
            m.M31 = (float)(eyeSeparation / width);
            m.M33 = (float)(-(far + near) / (far - near));
            m.M41 = -(float)interaxialCameraSeparation / 2;
            m.M43 = (float)(-2 * far * near / (far - near));
            m.M34 = -1;
            return m;
        }

        private static Matrix4x4 StereoscopicMatrixR2(double aspectRatio, double fov, double near, double far,
            double eyeSeparation, double viewDistance, double interaxialCameraSeparation)
        {
            var m = StereoscopicMatrixL2(aspectRatio, fov, near, far, eyeSeparation, viewDistance, interaxialCameraSeparation);
            m.M41 = (float)interaxialCameraSeparation / 2;
            return m;
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

        private static Matrix4x4 StereoscopicMatrixL3(float aspectRatio, float fov, float near, float far, float eye, float screen)
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
            return projectionMatrix;
        }

        private static Matrix4x4 StereoscopicMatrixR3(float aspectRatio, float fov, float near, float far, float eye, float screen)
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
            return projectionMatrix;
        }
    }
}

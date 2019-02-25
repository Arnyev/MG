using System;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;

namespace MG
{
    class Pipeline
    {
        private readonly Camera _camera;
        private readonly double _fov;
        private readonly double _near;
        private readonly double _far;
        private readonly PictureBox _pictureBox;
        private readonly DrawableObjectsController _drawableObjectsController;

        public Pipeline(Camera camera, double fov, double near, double far, PictureBox pictureBox, DrawableObjectsController drawableObjectsController)
        {
            _camera = camera;
            _fov = fov;
            _near = near;
            _far = far;
            _pictureBox = pictureBox;
            _drawableObjectsController = drawableObjectsController;
        }

        public void Redraw()
        {
            var pen = Pens.Black;

            var perspectiveMatrix = PerspectiveMatrix((float)_pictureBox.Width / _pictureBox.Height, _fov, _near, _far);
            var viewportMatrix = GetViewportMatrix(_pictureBox.Width, _pictureBox.Height);
            var viewMatrix = _camera.GetViewMatrix();
            var matrixViewProj = viewMatrix * perspectiveMatrix;

            using (var g = Graphics.FromImage(_pictureBox.Image))
            {
                g.Clear(Color.White);
                foreach (var torus in _drawableObjectsController.DrawableObjects)
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
                        g.DrawLine(pen, p1.X, p1.Y, p2.X, p2.Y);
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
    }
}

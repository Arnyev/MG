using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

            foreach (var ob in _objectsController.DrawableObjects)
            {
                var curve = ob as BezierCurve;

                var mvp = ob.GetModelMatrix() * matrixViewProj;
                var tuple = ob.GetLines();
                var pointsLines = tuple.Item2;

                var newPoints = new Vector4[pointsLines.Length];
                for (int i = 0; i < pointsLines.Length; i++)
                {
                    var transformed = Vector4.Transform(pointsLines[i], mvp);
                    newPoints[i] = transformed / transformed.W;
                }

                List<Tuple<Vector4, Vector4, bool>> linesToDraw = new List<Tuple<Vector4, Vector4, bool>>();

                foreach (var line in tuple.Item1)
                {
                    var p1 = newPoints[line.Start];
                    var p2 = newPoints[line.End];
                    bool draw = !(!(p1.X > -1) || !(p1.X < 1) || !(p1.Y > -1) || !(p1.Y < 1) || !(p1.Z > -1) || !(p1.Z < 1) ||
                                  !(p2.X > -1) || !(p2.X < 1) || !(p2.Y > -1) || !(p2.Y < 1) || !(p2.Z > -1) ||
                                  !(p2.Z < 1));

                    p1 = Vector4.Transform(p1, viewportMatrix);
                    p2 = Vector4.Transform(p2, viewportMatrix);
                    linesToDraw.Add(Tuple.Create(p1, p2, draw));
                }

                if (curve == null || curve.DrawLines)
                    linesToDraw.Where(x => x.Item3).ToList().
                        ForEach(l => _bitmap.DrawLine(new Point((int)l.Item1.X, (int)l.Item1.Y),
                          new Point((int)l.Item2.X, (int)l.Item2.Y), color, shouldAppend));

                if (curve == null || linesToDraw.Count == 0)
                    continue;

                var points = linesToDraw.Select(x => x.Item1).Concat(linesToDraw.Select(x => x.Item2)).ToList();
                var minX = points.Min(p => p.X);
                var maxX = points.Max(p => p.X);
                var minY = points.Min(p => p.Y);
                var maxY = points.Max(p => p.Y);
                var count = (int)(maxX - minX + maxY - minY);

                if (count > _bitmap.Width + _bitmap.Height)
                    count = _bitmap.Width + _bitmap.Height;

                var pointsToDraw = curve.GetPoints(count);

                pointsToDraw.ForEach(x => DrawPoint(color, matrixViewProj, viewportMatrix, x));
            }

            DrawPoints(color, matrixViewProj, viewportMatrix);
        }

        private void DrawPoints(MyColor color, Matrix4x4 matrixViewProj, Matrix4x4 viewportMatrix)
        {
            var g = Graphics.FromImage(_bitmap.Bitmap);
            var pen = Pens.Yellow;
            foreach (var point in _objectsController.Points)
                DrawPoint(color, matrixViewProj, viewportMatrix, point.Point, g, pen, point, point.Selected);

            g.Dispose();
        }

        private void DrawPoint(MyColor color, Matrix4x4 matrixViewProj, Matrix4x4 viewportMatrix, Vector4 point, Graphics g = null, Pen pen = null, DrawablePoint pointData = null, bool circle = false)
        {
            var transformed = Vector4.Transform(point, matrixViewProj);
            var p = transformed / transformed.W;
            if (pointData != null)
                pointData.ScreenPosition = new Point(-100, -100);

            if (!(p.X > -1) || !(p.X < 1) || !(p.Y > -1) || !(p.Y < 1) || !(p.Z > -1) || !(p.Z < 1))
                return;

            var screenPoint = Vector4.Transform(p, viewportMatrix);
            if (screenPoint.X < 2 || screenPoint.X > _bitmap.Width - 2 || screenPoint.Y < 2 ||
                screenPoint.Y > _bitmap.Height - 2)
                return;

            var x = (int)screenPoint.X;
            var y = (int)screenPoint.Y;
            _bitmap.SetPixel(x - 1, y, color);
            _bitmap.SetPixel(x, y + 1, color);
            _bitmap.SetPixel(x + 1, y, color);
            _bitmap.SetPixel(x, y - 1, color);
            _bitmap.SetPixel(x, y, color);

            if (pointData != null)
                pointData.ScreenPosition = new Point(x, y);
            if (circle && g != null && pen != null)
                g.DrawEllipse(pen, screenPoint.X - 5, screenPoint.Y - 5, 10, 10);
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
            var m = new Matrix4x4
            {
                M11 = 2 * near / (right - left),
                M22 = 2 * near / (top - bottom),
                M31 = (right + left) / (right - left),
                M32 = (top + bottom) / (top - bottom),
                M33 = -(far + near) / (far - near),
                M43 = -2 * far * near / (far - near),
                M34 = -1
            };
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

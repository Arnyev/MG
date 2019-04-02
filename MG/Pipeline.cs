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
        private readonly float _aspectRatio;
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
            _aspectRatio = (float)_bitmap.Width / _bitmap.Height;
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
                var left = StereoscopicMatrixL(_aspectRatio, _fov, _near, _far, eyesep,
                    viewDist);

                DrawSinglePass(left, new MyColor(255, 0, 0), true);

                var right = StereoscopicMatrixR(_aspectRatio, _fov, _near, _far, eyesep,
                    viewDist);

                DrawSinglePass(right, new MyColor(0, 255, 255), true);
            }
            else
            {
                var perspectiveMatrix = PerspectiveMatrix(_aspectRatio, _fov, _near, _far);
                DrawSinglePass(perspectiveMatrix, new MyColor(255, 255, 255), false);
                var middleX = _bitmap.Width / 2;
                var middleY = _bitmap.Height / 2;

                _bitmap.DrawLine(new Point(middleX - 10, middleY), new Point(middleX + 10, middleY), _cursor.Color);
                _bitmap.DrawLine(new Point(middleX, middleY - 10), new Point(middleX, middleY + 10), _cursor.Color);
            }
        }

        private static Vector4 GetNormal(Vector3 v1, Vector4 v2)
        {
            var v23 = new Vector3(v2.X, v2.Y, v2.Z);
            var nv2 = Vector3.Normalize(v23);
            var cross = Vector3.Cross(v1, nv2);
            return new Vector4(cross, 0);
        }

        private void UpdateEdgesIntersectionPoints(List<Vector4> points, List<Line> lines)
        {
            var tanFov = (float)Math.Tan(_fov / 2);
            var startZ = -_near;
            var endZ = -_far;
            var xRatio = tanFov * _aspectRatio;
            var yRatio = tanFov;

            var pointFront = new Vector4(0, 0, -_far, 1);
            var pointBack = new Vector4(0, 0, -_near, 1);
            var pointRight = new Vector4(xRatio * -_far, 0, -_far, 1);
            var pointLeft = new Vector4(-xRatio * -_far, 0, -_far, 1);
            var pointUp = new Vector4(0, yRatio * -_far, -_far, 1);
            var pointDown = new Vector4(0, -yRatio * -_far, -_far, 1);

            var normalFront = new Vector4(0, 0, -1, 0);
            var normalBack = new Vector4(0, 0, 1, 0);
            var normalRight = GetNormal(new Vector3(0, 1, 0), pointRight);
            var normalLeft = GetNormal(new Vector3(0, 1, 0), pointLeft);
            var normalUp = GetNormal(new Vector3(1, 0, 0), pointUp);
            var normalDown = GetNormal(new Vector3(1, 0, 0), pointDown);

            Vector4[] normals = { normalFront, normalBack, normalRight, normalLeft, normalUp, normalDown };
            Vector4[] planePoints = { pointFront, pointBack, pointRight, pointLeft, pointUp, pointDown };

            var newLines = new List<Line>();
            foreach (var line in lines)
            {
                List<Vector4> intersections = new List<Vector4>();
                List<float> distances = new List<float>();

                var inStart = points[line.Start];
                var inEnd = points[line.End];

                bool startInside = inStart.Z < startZ && inStart.Z > endZ &&
                                   inStart.Y > inStart.Z * yRatio && inStart.Y < inStart.Z * -yRatio &&
                                   inStart.X > inStart.Z * xRatio && inStart.X < inStart.Z * -xRatio;

                bool endInside = inEnd.Z < startZ && inEnd.Z > endZ &&
                                 inEnd.Y > inEnd.Z * yRatio && inEnd.Y < inEnd.Z * -yRatio &&
                                 inEnd.X > inEnd.Z * xRatio && inEnd.X < inEnd.Z * -xRatio;

                if (startInside & endInside)
                {
                    points[line.Start] = inStart;
                    points[line.End] = inEnd;
                    continue;
                }

                if (startInside)
                {
                    distances.Add(0);
                    intersections.Add(inStart);
                }

                if (endInside)
                {
                    distances.Add(1);
                    intersections.Add(inEnd);
                }

                var diff = inEnd - inStart;
                for (int i = 0; i < 6; i++)
                    if (CheckIntersection(planePoints[i], normals[i], inStart, diff, xRatio, yRatio, out Vector4 intersection, out float distance))
                    {
                        intersections.Add(intersection);
                        distances.Add(distance);
                    }

                if (intersections.Count == 2)
                {
                    newLines.Add(new Line(points.Count, points.Count + 1));

                    points.Add(intersections[0]);
                    points.Add(intersections[1]);

                }
                else if (intersections.Count > 2)
                {

                }
            }

            lines.AddRange(newLines);
        }

        private bool CheckIntersection(Vector4 planePoint, Vector4 planeNormal, Vector4 lineStart, Vector4 lineDirection, float xRatio, float yRatio, out Vector4 intersection, out float distanceFromStart)
        {
            const float epsilon = 1e-4f;

            distanceFromStart = Vector4.Dot(planePoint - lineStart, planeNormal) / Vector4.Dot(planeNormal, lineDirection);

            if (distanceFromStart < -epsilon || distanceFromStart > 1 + epsilon)
            {
                intersection = new Vector4();
                return false;
            }

            intersection = lineStart + distanceFromStart * lineDirection;
            if (intersection.Z > -_near + epsilon || intersection.Z < -_far - epsilon)
                return false;

            //czy należy do frustrum
            bool isOnNearOrFar = Math.Abs(intersection.Z - -_near) < epsilon || Math.Abs(intersection.Z - -_far) < epsilon;
            bool isOutSide = intersection.X / intersection.Z > xRatio + epsilon || intersection.X / intersection.Z < -xRatio - epsilon ||
                             intersection.Y / intersection.Z > yRatio + epsilon || intersection.Y / intersection.Z < -yRatio - epsilon;
            return !isOnNearOrFar || !isOutSide;
        }

        private void DrawSinglePass(Matrix4x4 perspectiveMatrix, MyColor color, bool shouldAppend)
        {

            var viewportMatrix = GetViewportMatrix(_bitmap.Width, _bitmap.Height);
            var viewMatrix = _camera.ViewMatrix;
            var matrixViewProj = viewMatrix * perspectiveMatrix;

            foreach (var ob in _objectsController.DrawableObjects)
            {
                var curve = ob as ICurve;

                var mv = ob.GetModelMatrix() * viewMatrix;
                var tuple = ob.GetLines();
                var pointsLines = tuple.Item2;

                var newPoints = pointsLines.Select(t => Vector4.Transform(t, mv)).ToList();

                var lineList = tuple.Item1.ToList();

                if (!(ob is Cursor3D))
                    UpdateEdgesIntersectionPoints(newPoints, lineList);

                for (int i = 0; i < newPoints.Count; i++)
                {
                    newPoints[i] = Vector4.Transform(newPoints[i], perspectiveMatrix);
                    newPoints[i] = newPoints[i] / newPoints[i].W;
                }

                List<Tuple<Vector4, Vector4, bool>> linesToDraw = new List<Tuple<Vector4, Vector4, bool>>();

                foreach (var line in lineList)
                {
                    var p1 = newPoints[line.Start];
                    var p2 = newPoints[line.End];
                    var minusOne = -1f - 1e-4f;
                    var plusOne = 1f + 1e-4f;

                    bool drawP1 = p1.X >= minusOne && p1.X <= plusOne && p1.Y >= minusOne && p1.Y <= plusOne && p1.Z >= minusOne && p1.Z <= 1;
                    bool drawp2 = p2.X >= minusOne && p2.X <= plusOne && p2.Y >= minusOne && p2.Y <= plusOne && p2.Z >= minusOne && p2.Z <= 1;

                    p1 = Vector4.Transform(p1, viewportMatrix);
                    p2 = Vector4.Transform(p2, viewportMatrix);
                    linesToDraw.Add(Tuple.Create(p1, p2, drawP1 & drawp2));
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

            if (p.X > -1 && p.X < 1 && p.Y > -1 && p.Y < 1 && p.Z > -1 && p.Z < 1)
            {
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

using System;
using System.Drawing;
using System.Numerics;

namespace MG
{
    public class RaycastingParameters
    {
        public float LightPositionX { get; set; } = 0.0f;
        public float LightPositionY { get; set; } = 10.0f;
        public float LightPositionZ { get; set; } = 0.0f;
        public float CosinusExponent { get; set; } = 10.0f;
        public float DistributedCoef { get; set; } = 0.3f;
        public float SpecularCoef { get; set; } = 0.3f;
        public float EllipseX { get; set; } = 1.0f;
        public float EllipseY { get; set; } = 1.0f;
        public float EllipseZ { get; set; } = 1.0f;
        public int SunRadius { get; set; } = 10;
    }

    class RayCaster
    {
        private readonly DirectBitmap _directBitmap;
        private readonly Camera _camera;
        private readonly double _fov;

        public RayCaster(DirectBitmap directBitmap, Camera camera, double fov)
        {
            _directBitmap = directBitmap;
            _camera = camera;
            _fov = fov;
        }

        public void Draw(RaycastingParameters parameters)
        {
            var width = _directBitmap.Width;
            var height = _directBitmap.Height;

            using (var g = Graphics.FromImage(_directBitmap.Bitmap))
            {
                g.Clear(Color.Black);
            }

            var viewMatrix = _camera.GetViewMatrix();
            Matrix4x4.Invert(viewMatrix, out var invViewMatrix);

            var position = _camera.Position;
            float aspect = (float)height / width;
            float s = (float)(-2.0 * Math.Tan(_fov * 0.5));
            var rx2 = parameters.EllipseX * parameters.EllipseX;
            var ry2 = parameters.EllipseY * parameters.EllipseY;
            var rz2 = parameters.EllipseZ * parameters.EllipseZ;

            var ellipse4 = new Vector4(1.0f / rx2, 1.0f / ry2, 1.0f / rz2, -1.0f);
            var posEllips = Vector4.Multiply(position, ellipse4);
            var constant = Vector4.Dot(posEllips, position);

            var direction = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
            var lightPosition = new Vector3(parameters.LightPositionX, parameters.LightPositionY,
                parameters.LightPositionZ);

            for (int y = 0; y < height; ++y)
            {
                direction.Y = -((float)y / height - 0.5f) * s * aspect;
                for (int x = 0; x < width; ++x)
                {
                    direction.X = ((float)x / width - 0.5f) * s;

                    var rotatedDirection = Vector4.Transform(direction, invViewMatrix);

                    var squaredCoef = Vector4.Dot(Vector4.Multiply(rotatedDirection, ellipse4), rotatedDirection);
                    var linearCoef = 2 * Vector4.Dot(rotatedDirection, posEllips);

                    var preRoot = linearCoef * linearCoef - 4 * squaredCoef * constant;
                    if (!(preRoot >= 0))
                        continue;

                    var sgn = squaredCoef > 0 ? 1.0 : -1.0;
                    var coef = (float)((sgn * Math.Sqrt(preRoot) - linearCoef) / (2.0 * squaredCoef));
                    if (coef > 0)
                        continue;

                    var pointPosition = position + coef * rotatedDirection;
                    var pointPosition3 = new Vector3(pointPosition.X, pointPosition.Y, pointPosition.Z);
                    var rotatedDirection3 = new Vector3(rotatedDirection.X, rotatedDirection.Y, rotatedDirection.Z);

                    _directBitmap.SetPixel(x, height - 1 - y,
                        GetPixelColor(pointPosition3, rotatedDirection3, lightPosition, parameters));
                }
            }

            if (_directBitmap.Width > 500)
                AddSun(parameters, position, lightPosition, viewMatrix, s, width, aspect, height);
        }

        private void AddSun(RaycastingParameters parameters, Vector4 position, Vector3 lightPosition, Matrix4x4 viewMatrix,
            float s, int width, float aspect, int height)
        {
            var lightDirection = position - new Vector4(lightPosition, 1.0f);
            var lightDirectionView = Vector4.Transform(lightDirection, viewMatrix);
            if (lightDirectionView.Z < 0)
                return;

            lightDirectionView /= lightDirectionView.Z;

            var xa = (lightDirectionView.X / s + 0.5f) * width;
            var ya = (lightDirectionView.Y / -s * aspect + -0.5f) * -height;

            if (xa < 0 || xa >= width || ya < 0 || ya >= height)
                return;

            for (int i = -parameters.SunRadius; i <= parameters.SunRadius; i++)
                for (int j = -parameters.SunRadius; j <= parameters.SunRadius; j++)
                    if (i * i + j * j < parameters.SunRadius * parameters.SunRadius)
                        _directBitmap.SetPixel((int)xa + i, (int)ya + j, new MyColor(255, 255, 255));
        }

        private static MyColor GetPixelColor(Vector3 pointPosition, Vector3 lookDirection, Vector3 lightPosition, RaycastingParameters parameters)
        {
            var lightDirection = Vector3.Normalize(lightPosition - pointPosition);
            var rx2 = parameters.EllipseX * parameters.EllipseX;
            var ry2 = parameters.EllipseY * parameters.EllipseY;
            var rz2 = parameters.EllipseZ * parameters.EllipseZ;

            var normal = pointPosition;
            normal.X /= rx2;
            normal.Y /= ry2;
            normal.Z /= rz2;

            normal = Vector3.Normalize(normal);

            double cos = Vector3.Dot(normal, lightDirection);
            if (cos < 0)
                cos = 0;

            var distR = (byte)(255 * cos * parameters.DistributedCoef);
            var distG = (byte)(255 * cos * parameters.DistributedCoef);

            var rVector = Vector3.Normalize(2 * Vector3.Dot(normal, lightDirection) * normal - lightDirection);
            var toObsVector = Vector3.Normalize(lookDirection);

            var specularDot = Vector3.Dot(rVector, toObsVector);
            if (specularDot < 0)
                specularDot = 0;
            var specular = Math.Pow(specularDot, parameters.CosinusExponent) * 256 * parameters.SpecularCoef;

            var r = distR + specular > 255 ? (byte)255 : (byte)(distR + specular);
            var g = distG + specular > 255 ? (byte)255 : (byte)(distG + specular);
            var b = 30 + (byte)specular > 255 ? (byte)255 : (byte)(30 + specular);

            return new MyColor(r, g, b);
        }
    }
}

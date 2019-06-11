using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media.Effects;

namespace MG
{
    class ObjectsController
    {
        private bool _isHandling;
        private readonly PropertyGrid _propertyGrid;
        private readonly ListBox _listBox;
        private readonly ListBox _listBox2;
        private readonly Cursor3D _cursor;
        public IReadOnlyList<IDrawableObject> DrawableObjects =>
            _listBox.Items.OfType<IDrawableObject>()
            .Concat(_listBox.Items.OfType<GregoryPatchContainer>().SelectMany(x => x.Patches))
                .ToList();

        public IReadOnlyList<DrawablePoint> Points =>
            _listBox.Items.OfType<DrawablePoint>()
                .Concat(_listBox.Items.OfType<BSplineCurve>().SelectMany(x => x.BernsteinPoints))
                .Concat(_listBox.Items.OfType<BasicSurface>().SelectMany(x => x.Points))
                .Concat(_listBox.Items.OfType<BSplineSurface>().SelectMany(x => x.Points))
                .ToList();

        public RaycastingParameters RaycastingParameters { get; } = new RaycastingParameters();

        public ObjectsController(PropertyGrid propertyGrid, ListBox listBox, ListBox listBox2, FlowLayoutPanel panel, Cursor3D cursor)
        {
            _propertyGrid = propertyGrid;
            _listBox = listBox;
            _listBox2 = listBox2;
            _cursor = cursor;
            //_listBox.Items.Add(RaycastingParameters);
            //_selectableObjects.Add(RaycastingParameters);
            _listBox.SelectedIndexChanged += _listBox_SelectedIndexChanged;
            _listBox.DisplayMember = "Name";
            _listBox.Items.Add(cursor);
            _listBox2.DisplayMember = "Name";
            _listBox2.SelectedIndexChanged += _listBox2_SelectedIndexChanged;
            //_listBox.Items.Add(new FrenetCurve());
            panel.Controls.Add(GetButton("Add torus", AddTorus));
            panel.Controls.Add(GetButton("Add point", AddPoint));
            panel.Controls.Add(GetButton("Add Bezier curve", AddBezierCurve));
            panel.Controls.Add(GetButton("Add spline curve", AddSplineCurve));
            panel.Controls.Add(GetButton("Add interpolating curve", AddBSplineInterpolateCurve));
            panel.Controls.Add(GetButton("Add surface", AddSurface));
            panel.Controls.Add(GetButton("Add BSpline surface", AddBsplineSurface));
            panel.Controls.Add(GetButton("Match points", MatchPoints));
            panel.Controls.Add(GetButton("Insert patch", InsertPatch));
            panel.Controls.Add(GetButton("Add Intersecting Curve", AddIntersectingCurve));
            panel.Controls.Add(GetButton("Draw Curve Params", DrawCurveParams));
            panel.Controls.Add(GetButton("Trim", Trim));
            panel.Controls.Add(GetButton("Serialize", Serialize));
            panel.Controls.Add(GetButton("Deserialize", Deserialize));
            panel.Controls.Add(GetButton("Delete object", DeleteObject));
        }

        public void DrawCurveParams()
        {
            var intersectingCurves = _listBox.Items.OfType<IntersectionCurve>().Where(x => x.Selected).ToList();
            if (intersectingCurves.Count != 1)
                return;

            var curve = intersectingCurves[0];

            var size = 500;

            List<Vector2> paramsA = new List<Vector2>();
            curve.GetPoints(out paramsA);
            var nc = new IntersectionCurve(curve.B, curve.A, _cursor);

            List<Vector2> paramsB = new List<Vector2>();
            nc.GetPoints(out paramsB);

            var bitmap1 = new DirectBitmap(size, size);
            var bitmap2 = new DirectBitmap(size, size);

            curve.A.DrawCurve(paramsA, bitmap1);
            curve.B.DrawCurve(paramsB, bitmap2);

            var form = new Form() {Size = new Size(2 * size, size)};

            form.Controls.Add(new PictureBox { Size = new Size(size, size), Image = bitmap1.Bitmap });
            form.Controls.Add(new PictureBox { Size = new Size(size, size), Location = new Point(size, 0), Image = bitmap2.Bitmap });

            form.ShowDialog();
        }

        public void Trim()
        {
            var intersectingCurves = _listBox.Items.OfType<IntersectionCurve>().Where(x => x.Selected).ToList();
            if (intersectingCurves.Count != 1)
                return;

            var curve = intersectingCurves[0];

            curve.GetPoints(out var parameters);

            curve.A.Trim(parameters);
        }

        private void InsertPatch()
        {
            var surfaces = _listBox.Items.OfType<BasicSurface>().Where(x => x.Selected).ToList();
            if (surfaces.Count != 3)
                return;

            var merged1 = surfaces[0].Points.Where(x => x.IsMerged).ToList();
            var merged2 = surfaces[1].Points.Where(x => x.IsMerged).ToList();
            var merged3 = surfaces[2].Points.Where(x => x.IsMerged).ToList();

            merged1 = merged1.Where(x => merged2.Contains(x) || merged3.Contains(x)).ToList();
            merged2 = merged2.Where(x => merged1.Contains(x) || merged3.Contains(x)).ToList();
            merged3 = merged3.Where(x => merged2.Contains(x) || merged1.Contains(x)).ToList();

            if (merged1.Count != 2 || merged2.Count != 2 || merged3.Count != 2)
                return;

            if (!surfaces[0].AreLine(merged1[0], merged1[1]))
                return;

            if (merged3.Contains(merged1[1]))
            {
                var tmps = surfaces[2];
                surfaces[2] = surfaces[1];
                surfaces[1] = tmps;

                var tmpm = merged3;
                merged3 = merged2;
                merged2 = tmpm;
            }

            if (merged2.IndexOf(merged1[1]) == 1)
            {
                var tmpp = merged2[0];
                merged2[0] = merged2[1];
                merged2[1] = tmpp;
            }

            if (merged3.IndexOf(merged2[1]) == 0)
            {
                var tmpp = merged3[0];
                merged3[0] = merged3[1];
                merged3[1] = tmpp;
            }

            if (!surfaces[1].AreLine(merged2[0], merged2[1]))
                return;

            if (!surfaces[2].AreLine(merged3[0], merged3[1]))
                return;

            var d1 = surfaces[0].GetDirection(merged1[0], merged1[1]);
            var d2 = surfaces[1].GetDirection(merged2[0], merged2[1]);
            var d3 = surfaces[2].GetDirection(merged3[0], merged3[1]);

            var container = new GregoryPatchContainer(surfaces[0], surfaces[1], surfaces[2], d1, d2, d3);
            _listBox.Items.Add(container);
            _listBox.Refresh();
        }

        private void MatchPoints()
        {
            var surfaces = _listBox.Items.OfType<BasicSurface>().Where(x => x.Points.Any(y => y.Selected && y.IsCorner)).ToList();

            if (surfaces.Count != 2)
                return;

            var ps1 = surfaces[0].Points.Where(x => x.IsCorner && x.Selected && !x.IsMerged).ToList();
            var ps2 = surfaces[1].Points.Where(x => x.IsCorner && x.Selected && !x.IsMerged).ToList();

            if (ps1.Count != 1 || ps2.Count != 1 || ps1[0] == ps2[0])
                return;

            var p1 = ps1[0];
            var p2 = ps2[0];

            var newPosition = (p1.Point + p2.Point) / 2;
            p1.Point = newPosition;
            surfaces[1].ReplacePoint(p2, p1);
            p1.IsMerged = true;
        }

        private void Deserialize()
        {
            string filename = string.Empty;

            using (var ofd = new OpenFileDialog())
            {
                var res = ofd.ShowDialog();
                if (res != DialogResult.OK)
                    return;
                else
                    filename = ofd.FileName;
            }

            var f = _listBox.Items[0];
            _listBox.Items.Clear();
            _listBox.Items.Add(f);
            var lines = File.ReadAllLines(filename);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                var spl = line.Split(' ');
                var expected = int.Parse(spl[1].Trim());
                i++;
                var type = spl[0].Trim();
                switch (type)
                {
                    case "curveC0":
                        for (int j = 0; j < expected; j++, i++)
                        {
                            var curve = new BezierCurve(lines[i]);
                            _listBox.Items.Add(curve);
                            curve.Points.ForEach(p => _listBox.Items.Add(p));
                        }
                        break;
                    case "curveC2":
                        for (int j = 0; j < expected; j++, i++)
                        {
                            var curve = new BSplineCurve(lines[i]);
                            _listBox.Items.Add(curve);
                            curve.Points.ForEach(p => _listBox.Items.Add(p));
                        }
                        break;
                    case "curveInt":
                        for (int j = 0; j < expected; j++, i++)
                        {
                            var curve = new InterpolatingBSpline(lines[i]);
                            _listBox.Items.Add(curve);
                            curve.Points.ForEach(p => _listBox.Items.Add(p));
                        }
                        break;
                    case "surfaceC0":
                        for (int j = 0; j < expected; j++, i++)
                            _listBox.Items.Add(new BasicSurface(lines[i], false));
                        break;
                    case "tubeC0":
                        for (int j = 0; j < expected; j++, i++)
                            _listBox.Items.Add(new BasicSurface(lines[i], true));
                        break;
                    case "surfaceC2":
                        for (int j = 0; j < expected; j++, i++)
                            _listBox.Items.Add(new BSplineSurface(lines[i], false));
                        break;
                    case "tubeC2":
                        for (int j = 0; j < expected; j++, i++)
                            _listBox.Items.Add(new BSplineSurface(lines[i], true));
                        break;
                    default:
                        break;
                }
                i--;
            }
        }

        private void Serialize()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            string filename = string.Empty;

            using (var ofd = new SaveFileDialog())
            {
                var res = ofd.ShowDialog();
                if (res != DialogResult.OK)
                    return;
                else
                    filename = ofd.FileName;
            }

            List<string> s = new List<string>();

            var bezierCurves = _listBox.Items.OfType<BezierCurve>().ToList();
            s.Add($"curveC0 {bezierCurves.Count}");
            bezierCurves.ForEach(c => s.Add(c.ToString()));

            var bsplines = _listBox.Items.OfType<BSplineCurve>().ToList();
            s.Add($"curveC2 {bsplines.Count}");
            bsplines.ForEach(c => s.Add(c.ToString()));

            var ints = _listBox.Items.OfType<InterpolatingBSpline>().ToList();
            s.Add($"curveInt {ints.Count}");
            ints.ForEach(c => s.Add(c.ToString()));

            var surfs = _listBox.Items.OfType<BasicSurface>().Where(a => !a.IsTube).ToList();
            s.Add($"surfaceC0 {surfs.Count}");
            surfs.ForEach(c => s.Add(c.ToString()));

            surfs = _listBox.Items.OfType<BasicSurface>().Where(a => a.IsTube).ToList();
            s.Add($"tubeC0 {surfs.Count}");
            surfs.ForEach(c => s.Add(c.ToString()));

            var surfs2 = _listBox.Items.OfType<BSplineSurface>().Where(a => !a.IsTube).ToList();
            s.Add($"surfaceC2 {surfs2.Count}");
            surfs2.ForEach(c => s.Add(c.ToString()));

            surfs2 = _listBox.Items.OfType<BSplineSurface>().Where(a => a.IsTube).ToList();
            s.Add($"tubeC2 {surfs2.Count}");
            surfs2.ForEach(c => s.Add(c.ToString()));

            File.WriteAllLines(filename, s.ToArray());
        }

        private void AddInterpolatingCurveFromIntersectingCurve()
        {
            var intersectingCurves = _listBox.Items.OfType<IntersectionCurve>().Where(x => x.Selected).ToList();
            if (intersectingCurves.Count != 1)
                return;

            var curve = intersectingCurves[0];
            var interpolating = new InterpolatingBSpline();
            var points = curve.GetPoints(out var parameters);
            foreach (var point in points)
            {
                var drawable = new DrawablePoint(point.X, point.Y, point.Z);
                interpolating.AddPoint(drawable);
            }

            _listBox.Items.Remove(curve);
            _listBox.Items.Add(interpolating);
        }

        private void AddIntersectingCurve()
        {
            var intersectingObjects = _listBox.Items.OfType<IIntersecting>().Where(x => x.Selected).ToList();
            if (intersectingObjects.Count < 1 || intersectingObjects.Count > 2)
                return;

            _listBox.Items.Add(new IntersectionCurve(intersectingObjects[0], intersectingObjects.Last(), _cursor));
            _listBox.Refresh();
        }

        private void _listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedObject = _listBox.SelectedItem;
            if (selectedObject is ISublistContaining containing)
            {
                var deletedItem = _listBox2.SelectedItem;
                containing.RemoveObject(deletedItem);
                _listBox2.Items.Clear();
                _listBox2.Items.AddRange(containing.List.ToArray());
            }
        }

        private void AddTorus()
        {
            var torus = new Torus();
            _listBox.Items.Add(torus);
            _listBox.Refresh();
        }

        private void AddSurface()
        {
            using (var form = new SurfaceForm())
            {
                if (form.ShowDialog() != DialogResult.OK)
                    return;

                _listBox.Items.Add(new BasicSurface(form.SurfaceProperties));
                _listBox.Refresh();
            }
        }

        private void AddBsplineSurface()
        {
            using (var form = new SurfaceForm())
            {
                if (form.ShowDialog() != DialogResult.OK)
                    return;

                _listBox.Items.Add(new BSplineSurface(form.SurfaceProperties));
                _listBox.Refresh();
            }
        }

        private void AddPoint()
        {
            var point = new DrawablePoint(_cursor.Position.X, _cursor.Position.Y, _cursor.Position.Z);
            _listBox.Items.Add(point);
            _listBox.Refresh();

            _listBox.Items.OfType<ICurve>().Where(x => x.Selected).ToList().ForEach(x => x.AddPoint(point));
        }

        private void AddBezierCurve()
        {
            var selectedPoints = Points.Where(x => x.Selected).ToList();
            var curve = new BezierCurve();
            selectedPoints.ForEach(x => curve.AddPoint(x));
            _listBox.Items.Add(curve);
        }

        private void AddSplineCurve()
        {
            var selectedPoints = Points.Where(x => x.Selected).ToList();
            var curve = new BSplineCurve();
            selectedPoints.ForEach(x => curve.AddPoint(x));
            _listBox.Items.Add(curve);
        }

        private void AddBSplineInterpolateCurve()
        {
            var selectedPoints = Points.Where(x => x.Selected).ToList();
            var curve = new InterpolatingBSpline();
            selectedPoints.ForEach(x => curve.AddPoint(x));
            _listBox.Items.Add(curve);
        }



        private void DeleteObject()
        {
            if (_listBox.SelectedIndex != 0)
                _listBox.Items.Remove(_listBox.SelectedItem);
        }

        private void _listBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isHandling)
                return;

            _isHandling = true;

            var index = _listBox.SelectedIndex;
            var selectedObject = _listBox.SelectedItem;
            _propertyGrid.SelectedObject = selectedObject;
            var array = _listBox.Items.OfType<object>().ToArray();
            _listBox.Items.Clear();
            _listBox.Items.AddRange(array);
            _listBox.SelectedIndex = index;

            if (selectedObject is ISublistContaining listContaining)
            {
                _listBox2.Items.Clear();
                _listBox2.Items.AddRange(listContaining.List.ToArray());
            }

            if (selectedObject is DrawablePoint point)
                _listBox.Items.OfType<ICurve>().Where(x => x.Selected).ToList().ForEach(x => x.AddPoint(point));

            _isHandling = false;
        }

        private Button GetButton(string label, Action clickAction)
        {
            var button = new Button { BackColor = Color.DodgerBlue, ForeColor = Color.White, Text = label };
            button.Size = new Size(100, 25);
            button.MouseClick += (s, e) => clickAction();

            return button;
        }
    }

    public interface IDrawableObject
    {
        Matrix4x4 GetModelMatrix();
        Tuple<Line[], Vector4[]> GetLines();
    }

    interface ISublistContaining
    {
        IReadOnlyList<object> List { get; }
        void RemoveObject(object o);
    }
}

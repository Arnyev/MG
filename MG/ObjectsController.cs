﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Windows.Forms;

namespace MG
{
    class ObjectsController
    {
        private bool _isHandling;
        private readonly PropertyGrid _propertyGrid;
        private readonly ListBox _listBox;
        private readonly ListBox _listBox2;
        private readonly Cursor3D _cursor;
        public IReadOnlyList<IDrawableObject> DrawableObjects => _listBox.Items.OfType<IDrawableObject>().ToList();

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
            panel.Controls.Add(GetButton("Serialize", Serialize));
            panel.Controls.Add(GetButton("Deserialize", Deserialize));
            panel.Controls.Add(GetButton("Delete object", DeleteObject));
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

    interface IDrawableObject
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

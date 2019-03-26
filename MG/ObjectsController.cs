using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
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
        public IReadOnlyList<DrawablePoint> Points => _listBox.Items.OfType<DrawablePoint>().ToList();
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
            panel.Controls.Add(GetButton("Add torus", AddTorus));
            panel.Controls.Add(GetButton("Add point", AddPoint));
            panel.Controls.Add(GetButton("Add curve", AddCurve));
            panel.Controls.Add(GetButton("Delete object", DeleteObject));
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

        private void AddPoint()
        {
            var point = new DrawablePoint(_cursor.Position.X, _cursor.Position.Y, _cursor.Position.Z);
            _listBox.Items.Add(point);
            _listBox.Refresh();

            _listBox.Items.OfType<BezierCurve>().Where(x => x.Selected).ToList().ForEach(x => x.AddPoint(point));
        }

        private void AddCurve()
        {
            var selectedPoints = Points.Where(x => x.Selected).ToList();
            var curve = new BezierCurve();
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
                _listBox.Items.OfType<BezierCurve>().Where(x => x.Selected).ToList().ForEach(x => x.AddPoint(point));

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

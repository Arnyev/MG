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
        private readonly Cursor3D _cursor;
        public IReadOnlyList<IDrawableObject> DrawableObjects => _listBox.Items.OfType<IDrawableObject>().ToList();
        public IReadOnlyList<DrawablePoint> Points => _listBox.Items.OfType<DrawablePoint>().ToList();

        public RaycastingParameters RaycastingParameters { get; } = new RaycastingParameters();

        public ObjectsController(PropertyGrid propertyGrid, ListBox listBox, FlowLayoutPanel panel, Cursor3D cursor)
        {
            _propertyGrid = propertyGrid;
            _listBox = listBox;
            _cursor = cursor;
            //_listBox.Items.Add(RaycastingParameters);
            //_selectableObjects.Add(RaycastingParameters);
            _listBox.SelectedIndexChanged += _listBox_SelectedIndexChanged;
            _listBox.DisplayMember = "Name";
            _listBox.Items.Add(cursor);

            panel.Controls.Add(GetButton("Add torus", AddTorus));
            panel.Controls.Add(GetButton("Add point", AddPoint));
            panel.Controls.Add(GetButton("Delete object", DeleteObject));
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
            _propertyGrid.SelectedObject = _listBox.SelectedItem;
            var array = _listBox.Items.OfType<object>().ToArray();
            _listBox.Items.Clear();
            _listBox.Items.AddRange(array);
            _listBox.SelectedIndex = index;

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

    public class DrawablePoint
    {
        private static int _pointsCount = 0;

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector4 Point
        {
            get => new Vector4(X, Y, Z, 1.0f);
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }

        }

        public Vector4 FrozenPosition { get; set; }
        public bool Selected { get; set; }
        public bool Grabbed { get; set; }

        public string Name { get; set; }

        public DrawablePoint(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
            Name = "Point " + ++_pointsCount;
        }

        public DrawablePoint()
        {
            Name = "Point " + ++_pointsCount;
        }
    }
}

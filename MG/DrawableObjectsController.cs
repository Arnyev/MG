using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;

namespace MG
{
    class DrawableObjectsController
    {
        private readonly PropertyGrid _propertyGrid;
        private readonly ListBox _listBox;
        public IReadOnlyList<IDrawableObject> DrawableObjects => _drawableObjects;

        private readonly List<IDrawableObject> _drawableObjects = new List<IDrawableObject>();

        public DrawableObjectsController(PropertyGrid propertyGrid, ListBox listBox, FlowLayoutPanel panel)
        {
            _propertyGrid = propertyGrid;
            _listBox = listBox;
            _listBox.SelectedIndexChanged += _listBox_SelectedIndexChanged;
            _listBox.DisplayMember = "Name";
            panel.Controls.Add(GetButton("Add torus", AddTorus));
        }

        private void AddTorus()
        {
            var torus = new Torus();
            _drawableObjects.Add(torus);
            _listBox.Items.Add(torus);
            _listBox.Refresh();
        }

        private void _listBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _propertyGrid.SelectedObject = _listBox.SelectedItem;
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
}

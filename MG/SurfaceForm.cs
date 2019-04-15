using System;
using System.Windows.Forms;

namespace MG
{
    public partial class SurfaceForm : Form
    {
        public SurfaceProperties SurfaceProperties { get; }=new SurfaceProperties();
        public SurfaceForm()
        {
            InitializeComponent();
            propertyGrid1.SelectedObject = SurfaceProperties;
            button1.Click += Button1_Click;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

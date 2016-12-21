using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DataMidLayer
{
    public partial class ShowTable : Form
    {
        public ShowTable()
        {
            InitializeComponent();
        }

        public ShowTable(Control ctrl)
        {
            InitializeComponent();

            ctrl.Parent = this;
            ctrl.Visible = true;
            ctrl.Dock = DockStyle.Fill;            
        }
    }
}

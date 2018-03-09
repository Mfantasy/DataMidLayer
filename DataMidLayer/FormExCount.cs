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
    public partial class FormExCount : Form
    {
       
        public FormExCount(List<Sensor> eSensors)
        {
            InitializeComponent();
    
            if (eSensors != null && eSensors.Count != 0)
            {         
                dataGridView1.Columns.Add("Name", "设备名称");
                dataGridView1.Columns.Add("MX", "设备型号");
                dataGridView1.Columns.Add("IsWorking", "是否在线");
                dataGridView1.Columns.Add("LastTime", "最后一条数据时间");
                foreach (var item in eSensors)
                {
                    dataGridView1.Rows.Add(item.Name,item.Type,item.IsWorking,item.LastTime);
                }
            }
            else
            {
                MessageBox.Show("无异常设备");
                this.Close();
            }
        
        }

        
    }
}

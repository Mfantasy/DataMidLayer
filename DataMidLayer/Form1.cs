using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace DataMidLayer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //Load
            List<Sensor> sensors = LoadConfig();
            //DataGridView Initial
            dgvEX.AutoSize = true;
            dgvEX.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvEX.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvEX.Columns.Add("name", "设备名称");
            dgvEX.Columns.Add("type", "设备型号");
            dgvEX.Columns.Add("time", "异常时间");
            dgvEX.Columns.Add("msg", "异常名称");
            dgvEX.Columns.Add("stack", "调用堆栈");
            dgvEX.RowHeadersVisible = false;            
           //treeView
           
        }
        DataGridView dgvEX = new DataGridView();
        List<int> indexEX = new List<int>();

        #region 方法
        List<Sensor> LoadConfig()
        {
            List<Sensor> listSensors = new List<Sensor>();
            try
            {
                XmlDocument xml = new XmlDocument();
                xml.Load("config.xml");
                XmlNodeList sensors = xml.FirstChild.SelectNodes("sensor");
                foreach (XmlNode item in sensors)
                {
                    Sensor s = new Sensor();
                    s.Name = item.SelectSingleNode("name").InnerText;
                    s.Node = item.SelectSingleNode("node").InnerText;
                    s.Gateway = item.SelectSingleNode("gateway").InnerText;
                    s.Model = item.SelectSingleNode("model").InnerText;
                    s.Id = item.SelectSingleNode("sitewhere").InnerText;
                    listSensors.Add(s);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("配置文件载入异常\r\n"+ex.Message+"\r\n"+ex.StackTrace);
            }
            return listSensors;
        }

        #endregion















        #region 事件
        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
            //Show异常列表
            var show = new ShowTable(dgvEX);
            show.ShowDialog();
            foreach (int i in indexEX)
            {
                dgvEX.Rows[i].DefaultCellStyle.ForeColor = Color.Black;
            }
            indexEX.Clear();
            labelEX.Text = "无新异常";
            labelEX.ForeColor = Color.Black;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int i = dgvEX.Rows.Add("测试设备", "MXtest", DateTime.Now, "ex.msg", "ex.stack");
            dgvEX.Rows[i].DefaultCellStyle.ForeColor = Color.Red;           
            indexEX.Add(i);
            labelEX.ForeColor = Color.Red;
            labelEX.Text = "新异常_" + indexEX.Count.ToString();
        }
        #endregion

        //单独功能: 要显示最新出现异常日期及异常设备.点击进入log.txt|.异常记录.异常表.设备型号,设备名:异常.时间.
        //
        //首先转发状况展示:如何展示呢?
        //设备类型分表. 出异常会有提醒数字(1)()数字颜色.红色:新出现的异常,点击查看异常文件时消失.绿色:模拟数据的设备的数量.
        //点击绿色设备,会出现选项,配置如何模拟数据值.时间频率等.最后一条数据记录. 设置:功能是否启用.
        //设备分类详情监测.table. 基于此=>. 
        //



    }
}

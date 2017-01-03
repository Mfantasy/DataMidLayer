using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace DataMidLayer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //Load
            sensors = LoadConfig();
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
            BuildTree();
            //事件绑定
            checkBox1.CheckedChanged += CheckBox1_CheckedChanged;
            checkBox2.CheckedChanged += CheckBox2_CheckedChanged;
            numericUpDown2.ValueChanged += NumericUpDown2_ValueChanged;
            numericUpDown3.ValueChanged += NumericUpDown3_ValueChanged;
        }

        private void NumericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            sensor.Config.OverTimeM = (int)numericUpDown3.Value;
        }

        private void NumericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            sensor.Config.MoniIntervalM = (int)numericUpDown2.Value;
        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            sensor.Config.Moni = checkBox2.Checked;
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            sensor.Config.Remind = checkBox1.Checked;
        }

        List<Sensor> sensors;
        DataGridView dgvEX = new DataGridView();
        List<int> indexEX = new List<int>();

        #region 方法
        private void BuildTree()
        {
            List<string> types = new List<string>();
            foreach (var item in sensors)
            {
                if(types.Contains(item.Type))
                {
                    TreeNode node = new TreeNode(item.Name);
                    node.Tag = item;                    
                    treeView1.Nodes[item.Type].Nodes.Add(node);
                }
                else
                {
                    types.Add(item.Type);
                    TreeNode nd = new TreeNode(item.Type);
                    nd.Name = item.Type;
                    treeView1.Nodes.Add(nd);
                    TreeNode node = new TreeNode(item.Name);
                    nd.Nodes.Add(node);
                    node.Tag = item;
                }
            } 
        }
        List<Sensor> LoadConfig()
        {
            List<Sensor> listSensors = new List<Sensor>();
            try
            {
                XmlDocument xml = new XmlDocument();
                xml.Load("config.xml");
                XmlNodeList sensors = xml.FirstChild.SelectNodes("sensor");
                int i = 0;
                foreach (XmlNode item in sensors)
                {
                    Sensor s = new Sensor();
                    s.Name = item.SelectSingleNode("name").InnerText;
                    s.Node = item.SelectSingleNode("node").InnerText;
                    s.Gateway = item.SelectSingleNode("gateway").InnerText;
                    s.Type = item.SelectSingleNode("model").InnerText;
                    s.SiteWhereId = item.SelectSingleNode("sitewhere").InnerText;
                    s.Config.Moni = bool.Parse(ConfigurationManager.AppSettings["是否模拟"]);
                    s.Config.Remind = false;//bool.Parse(ConfigurationManager.AppSettings["是否提醒"]);
                    s.Config.OverTimeM = int.Parse(ConfigurationManager.AppSettings["数据超时判定时间"]);
                    s.Config.MoniIntervalM = int.Parse(ConfigurationManager.AppSettings["数据模拟发送频率"]);
                    s.CatchEx += S_CatchEx;
                    listSensors.Add(s);
                    i++;
                }
                dcount.Text = "设备数量:"+i.ToString();
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

        private void S_CatchEx(object sender, EventArgs e)
        {
            Sensor ss = sender as Sensor;
            this.Invoke(new Action(() =>
            {
                int i = dgvEX.Rows.Add(ss.Name, ss.Type, DateTime.Now, ss.Error.Msg, ss.Error.StactTrace);
                dgvEX.Rows[i].DefaultCellStyle.ForeColor = Color.Red;
                indexEX.Add(i);
                labelEX.ForeColor = Color.Red;
                labelEX.Text = "异常(" + indexEX.Count.ToString() + ")";
            }));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DataSubscribe.BeginSubscribe(sensors);
            button1.Enabled = false;
            Thread thMail = new Thread(SendMail);
            thMail.IsBackground = true;
            thMail.Start();
        }
        void SendMail()
        {
            bool remind = bool.Parse(ConfigurationManager.AppSettings["是否提醒"]);
            while (remind)
            {
                Thread.Sleep(24 * 60 * 60 * 1000);
                List<Sensor> exSensors = sensors.FindAll(ss => ss.IsEx);
                string title = "沣西海绵城市设备状态提醒";
                string body = "当前异常设备(超时判定时间:" + ConfigurationManager.AppSettings["数据超时判定时间"] + ")分钟\r\n";
                foreach (Sensor item in exSensors)
                {
                    body += item.Name + "\t" + item.Addr + "\r\n";
                }
                ThreadPool.QueueUserWorkItem(new WaitCallback((o) => DataAccess.SendMailUseZj(title, body)));
            }
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            sensor.SensorModel.MoniPostData(sensor);
        }        
      
        #endregion

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("iexplore.exe", linkLabel1.Text);
        }
        Sensor sensor;
        
        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeNode node = e.Node;
            if (node.Tag != null)
            {
                sensor = node.Tag as Sensor;
                if (sensor != null)
                {
                    
                    linkLabel1.Text = sensor.Addr;
                    string s1 = string.Format("{0}\r\n状态:{1}\r\n更新时间:\r\n{2}", sensor.Data.XmlData.Name, sensor.data.XmlData.Status, DateTime.Parse(sensor.data.XmlData.TimeStr));
                    string s2 = "";
                    foreach (string item in sensor.Current)
                    {
                        s2 += item + "\r\n";
                    }
                    
                    label3.Text = s1;
                    label4.Text = s2;
                    checkBox1.Checked = sensor.Config.Remind;
                    checkBox2.Checked = sensor.Config.Moni;
                    numericUpDown2.Value = sensor.Config.MoniIntervalM;
                    numericUpDown3.Value = sensor.Config.OverTimeM;
                    listBox1.DataSource = sensor.Log;
                    listBox1.Refresh();
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //手动重连
            try
            {
                sensor.Log.Clear();
                ThreadPool.QueueUserWorkItem((o) => DataSubscribe.StartSubscribe(sensor));
            }
            catch(Exception ex) { MessageBox.Show(ex.Message); }
        } 

        private void 获取异常设备列表邮件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<Sensor> exSensors = sensors.FindAll(ss => ss.IsEx);
            string title = "沣西海绵城市设备状态提醒";
            string body = "当前异常设备(超时判定时间:" + ConfigurationManager.AppSettings["数据超时判定时间"] + ")分钟\r\n";
            foreach (Sensor item in exSensors)
            {
                body += item.Name + "\t" + item.Addr + "\r\n";
            }
            ThreadPool.QueueUserWorkItem(new WaitCallback((o) => DataAccess.SendMailUseZj(title, body)));
            MessageBox.Show("邮件已发送,请注意查收");
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
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
                foreach (XmlNode item in sensors)
                {
                    Sensor s = new Sensor();
                    s.Name = item.SelectSingleNode("name").InnerText;
                    s.Node = item.SelectSingleNode("node").InnerText;
                    s.Gateway = item.SelectSingleNode("gateway").InnerText;
                    s.Type = item.SelectSingleNode("model").InnerText;
                    s.SiteWhereId = item.SelectSingleNode("sitewhere").InnerText;
                    s.Config.Moni = bool.Parse(ConfigurationManager.AppSettings["是否模拟"]);
                    s.Config.Remind = bool.Parse(ConfigurationManager.AppSettings["是否提醒"]);
                    s.Config.OverTimeM = int.Parse(ConfigurationManager.AppSettings["数据超时判定时间"]);
                    s.Config.MoniIntervalM = int.Parse(ConfigurationManager.AppSettings["数据模拟发送频率"]);
                    s.Config.RemindIntervalH = int.Parse(ConfigurationManager.AppSettings["邮件提醒频率"]);
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
            labelEX.Text = "异常(" + indexEX.Count.ToString()+")";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            XmlRoot rt = DataAccess.SerializeXml<XmlRoot>(DataAccess.HttpGet(sensors[0].XmlApi));
            List<string> current = new List<string>();
            foreach (var item in rt.XmlData.ChildRen)
            {
                current.Add(item.Name + ":" + item.Current.Value);
            } 
            Console.WriteLine();            
        }

        public void SendMailUseZj()
        {
            MailAddress EmailFrom = new MailAddress("mfantasy@mfant.com");
            MailAddress EmailTo = new MailAddress("mengft@txsec.com");
            MailMessage mailMsg = new MailMessage(EmailFrom, EmailTo);
            mailMsg.Subject = "测试邮件";
            mailMsg.Body = "此为测试邮件，可以忽略掉";
            SmtpClient spClient = new SmtpClient("smtp.qq.com");
            spClient.EnableSsl = true;
            spClient.Credentials = new System.Net.NetworkCredential("mfantasy@mfant.com", "ryqmwpnrmoygcbdd");
            try
            {
                spClient.Send(mailMsg);
                MessageBox.Show("发送成功");
            }
            catch (System.Net.Mail.SmtpException ex)
            {
                MessageBox.Show(ex.Message, "发送邮件出错");
            }
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
                    numericUpDown1.Value = sensor.Config.RemindIntervalH;
                    numericUpDown2.Value = sensor.Config.MoniIntervalM;
                    numericUpDown3.Value = sensor.Config.OverTimeM;
                }
            }
        }
    }
}

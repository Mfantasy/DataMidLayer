﻿using DataMidLayer.Device;
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

        //2.监控 数据时间间隔 .  
        //分档 20分钟不变的前提下, 处理3分钟时间差
        //

        //3.出现问题, 计算结束 数据 与 最新数据之间间隔差 / 3分钟一条 = 应弥补的次数
        // 数值 = 无数个0.2 / 0.4随机分布, 首先分布0.2 然后计算是否足够 , 如果不够则随机补0.4,

        //4.重构数据, 除了rain,剩下与第一条数据完全相同. then begin to post



        List<Sensor> sensors;
        List<int> indexEX = new List<int>();
        public Form1()
        {
            InitializeComponent();
        
            //Load
            sensors = LoadConfig();       
            //treeView
            BuildTree();
            System.Windows.Forms.Timer tm = new System.Windows.Forms.Timer();
            tm.Interval = 30 * 1000 * 60; //30分钟
            tm.Tick += Tm_Tick;
            tm.Start();   
        }

        private void Tm_Tick(object sender, EventArgs e)
        {
            List<Sensor> noWork = sensors.FindAll(ss => !ss.IsWorking);
            foreach (var item in noWork)
            {
                ThreadPool.QueueUserWorkItem((o) => DataSubscribe.GetAndPost(item, "监测到离线设备,重新进入GAP方法", 0));
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                DataSubscribe.BeginSubscribe(sensors);
                button1.Enabled = false;
                //Thread thMail = new Thread(SendMail);
                //thMail.IsBackground = true;
                //thMail.Start();
                //缓存
                Thread exPost = new Thread(PostS.QuePost);
                exPost.IsBackground = true;
                exPost.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

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
                    s.Moni = bool.Parse(ConfigurationManager.AppSettings["是否模拟"]);                    
                    s.OverTimeM = int.Parse(ConfigurationManager.AppSettings["数据超时判定时间"]);                    

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

        private void S_CatchEx(object sender, EventArgs e)
        {
         //   Sensor ss = sender as Sensor;
            this.Invoke(new Action(() =>
            {
           //     int i = dgvEX.Rows.Add(ss.Name, ss.Type, DateTime.Now, ss.Error.Msg, ss.Error.StactTrace);
             //   dgvEX.Rows[i].DefaultCellStyle.ForeColor = Color.Red;
                indexEX.Add(1);//i
                labelEX.ForeColor = Color.Red;
                labelEX.Text = "异常(" + indexEX.Count.ToString() + ")";
            }));
        }


        void SendMail()
        {
            while (true)
            {
                Thread.Sleep(24 * 60 * 60 * 1000);
                List<Sensor> exSensors = sensors.FindAll(ss => ss.IsEx);
                string title = "沣西海绵城市设备状态提醒";
                string body = "当前异常设备(超时判定时间:" + ConfigurationManager.AppSettings["数据超时判定时间"] + ")分钟\r\n";
                foreach (Sensor item in exSensors)
                {
                    body += item.Name + "\t" + item.Addr + "\r\n";
                }
                ThreadPool.QueueUserWorkItem(new WaitCallback((o) => DataAccess.SendMail(title, body)));
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            sensor.SensorModel.PostDataByXml(sensor);
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
                ThreadPool.QueueUserWorkItem((o) => DataSubscribe.GetAndPost(sensor,"手动重连",0));
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
            ThreadPool.QueueUserWorkItem(new WaitCallback((o) => DataAccess.SendMail(title, body)));
            MessageBox.Show("邮件已发送,请注意查收");
        }
       
    }
}

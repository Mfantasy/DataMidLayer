using DataMidLayer.Device;
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
     
        List<Sensor> sensors;
        List<int> indexEX = new List<int>();
        public Form1()
        {
            InitializeComponent();
            if (ConfigurationManager.AppSettings["resource"] == "fengxi")
            {
                this.Text = "沣西海绵数据转发";
            }
            else if (ConfigurationManager.AppSettings["resource"]=="baichengcity")
            {
                this.Text = "白城海绵数据转发";
            }
            else
            {
                this.Text = "null";
            }
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
                    tV1.Nodes[item.Type].Nodes.Add(node);
                }
                else
                {
                    types.Add(item.Type);
                    TreeNode nd = new TreeNode(item.Type);
                    nd.Name = item.Type;
                    tV1.Nodes.Add(nd);
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
        
        private void tV1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeNode node = e.Node;
            if (node.Tag != null)
            {
                sensor = node.Tag as Sensor;
                if (sensor != null)
                {
                    
                    linkLabel1.Text = sensor.Addr;
                    sensor.RefreshXmlData();
                    string s1 = string.Format("{0}\r\n状态:{1}\r\n更新时间:\r\n{2}", sensor.XmlTitle, sensor.XmlStatus, DateTime.Parse(sensor.XmlTime));
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

        void MonitorData(Object obj)
        {
            Sensor sensor = obj as Sensor;
            double lastRain = -1;
            DateTime lastTime = DateTime.MinValue;
            while (true)
            {
                string rain = null;
                sensor.RefreshXmlData();
                Console.WriteLine("数据刷新时间{0}",DateTime.Now);
                //判断feed中有没有port
                if (sensor.XmlValues.Count == 5)
                {
                    rain = sensor.XmlValues[0];
                }
                else
                {
                    rain = sensor.XmlValues[1];
                }
                if (string.IsNullOrWhiteSpace(rain))
                {
                    continue;
                }
                //初始化初始数据
                if (lastRain == -1) 
                {
                    lastRain = Math.Round(double.Parse(rain), 1); //0.2mm
                    lastTime = DateTime.Parse(sensor.XmlTime);
                    sensor.StatusByXml = true;
                    sensor.TimesByXml = 0;
                    Console.WriteLine("初始化数据结束");
                }
                //开始工作流程
                else
                {
                    if (lastTime != DateTime.Parse(sensor.XmlTime))
                    {
                        //说明没有数据中断,判断设备当前状态,如果处于异常状态,则说明此时设备数据已恢复正常,进行计算处理
                        Console.WriteLine("数据正常");
                        if (!sensor.StatusByXml)
                        {                          
                            //计算
                            try
                            {
                                Console.WriteLine("重算及转发");                        
                                CalAndPost(sensor, lastRain, double.Parse(rain), lastTime);
                            }
                            catch (Exception ex)
                            {
                                Utils.WriteError(ex.Message+"\r\n"+DateTime.Now+"\r\n", "气象站数据补传异常CAP.txt");
                            }                            
                        }
                        //更新雨量,更新时间,更新状态,更新次数
                        sensor.StatusByXml = true;
                        sensor.TimesByXml = 0;
                        lastRain = Math.Round(double.Parse(rain), 1);
                        lastTime = DateTime.Parse(sensor.XmlTime);
                    }
                    else
                    {
                        
                        //说明设备3分钟内没有进行数据上行,数据中断,将设备状态false,times+1
                        sensor.StatusByXml = false;
                        sensor.TimesByXml++;
                        Console.WriteLine("数据异常{0}",sensor.TimesByXml);
                    }
                }
                Thread.Sleep(3 * 60 * 1000);
            }

        }

        private void CalAndPost(Sensor sensor, double lastRain, double rain,DateTime lastTime)
        {                   
            //计算具体逻辑.0.2为基准量
            //1.取增量 . 2.根据增量与基数0.2计算出应该补充的0.2的数量, 然后将数量与次数做比较,
            //如果少于0.2的数量少于需要post的次数,那么做随机分布算法. 如果数量多于次数, 那么取多出的次数,然后先次数0.2,再随机分布新0.2
            //3.循环,Post
            double rAdd = rain - lastRain;
            int countBase = (int)(rAdd / 0.2);
            DateTime tempTime = lastTime;

            //首先得对翻斗值做特殊处理
            //先判断是否翻斗,然后再判断是否是正常翻斗
            if (rAdd < 0)
            {
                if (lastRain > 25.4)
                {
                    DataAccess.SendMail("雨量数据值超过25.4!!!",sensor.Name);
                    return;
                }                

                if ((25.4 - lastRain) < 2) //误差在2mm之内皆为正常范围
                {
                    rAdd = 25.4 - lastRain + rain;                    
                }
                else //超出误差处理范围,抛弃lastRain进行计算
                {
                    rAdd = rain;                    
                }
                countBase = (int)(rAdd / 0.2);
            }
            //如果base为0,说明没下雨,自动补
            else if (countBase == 0)
            {
                for (int i = 0; i < sensor.TimesByXml; i++)
                {
                    tempTime = tempTime.AddMinutes(3);
                    (sensor.SensorModel as MXS5000).PostByXml(sensor, rain.ToString("0.0"), tempTime);
                }
            }
            else
            {
                //下雨了,判断base和time谁多
                int countTemp = sensor.TimesByXml - countBase;
                double rainTemp = lastRain;
                if (countTemp > 0)
                {
                    //次数多,随机分布0.2   
                    List<int> tiChu = GetTiChuRandomNum(sensor.TimesByXml, countTemp);
                    for (int i = 0; i < sensor.TimesByXml; i++)
                    {
                        tempTime = tempTime.AddMinutes(3);
                        if (!tiChu.Contains(i))
                        {
                            rainTemp = rainTemp + 0.2;
                            if (rainTemp >= 25.6)
                            {
                                rainTemp = 0;
                            }
                        }
                        (sensor.SensorModel as MXS5000).PostByXml(sensor, rainTemp.ToString("0.0"), tempTime);
                    }
                }
                else if (countTemp == 0)
                {
                    for (int i = 0; i < sensor.TimesByXml; i++)
                    {
                        tempTime = tempTime.AddMinutes(3);
                        rainTemp = rainTemp + 0.2;
                        if (rainTemp >= 25.6)
                        {
                            rainTemp = 0;
                        }
                        (sensor.SensorModel as MXS5000).PostByXml(sensor, rainTemp.ToString("0.0"), tempTime);
                    }
                }
                else if (countTemp < 0)
                {
                    List<int> add = GetTiChuRandomNum(sensor.TimesByXml, countTemp * -1);
                    for (int i = 0; i < sensor.TimesByXml; i++)
                    {
                        tempTime = tempTime.AddMinutes(3);
                        rainTemp = rainTemp + 0.2;
                        if (add.Contains(i))
                        {
                            rainTemp = rainTemp + 0.2;
                        }
                        if (rainTemp >= 25.6)
                        {
                            rainTemp = 0;
                        }
                        (sensor.SensorModel as MXS5000).PostByXml(sensor, rainTemp.ToString("0.0"), tempTime);
                    }
                }
            }
        }

        private List<int> GetTiChuRandomNum(int timesByXml, int countTemp)
        {
            //随机剔除
            List<int> l = new List<int>();
            Random r = new Random();
            for (int i = 0; i < countTemp; i++)
            {
                int num = r.Next(0, timesByXml);
                while (l.Contains(num))
                {
                    num = r.Next(0, timesByXml);
                }
                l.Add(num);
            }
            return l;
        }

        //一.根据XML判定设备超时具体思路
        //情景.
        //1.如果两次访问数据时间不相同,说明数据没有中断,不做处理. ----- 如果此时sensor状态为false 跳入处理方案2.
        //2.如果两次访问数据时间相同, 说明数据已经中断3分钟,此时应该跳入处理方案1.
        //处理方案
        //1.sensor状态false,times+1 . 记录雨量
        //2.sensor状态true,统计次数 . 计算雨量  跳入处理方案3
        //3.传入雨量及次数 , 做循环,并加入时间的计算.然后封包然后post
        //关于时间计算    最新数据之间间隔差 / 3分钟一条 = 应弥补的次数
        // 数值 = 无数个0.2 / 0.4随机分布, 首先分布0.2 然后计算是否足够 , 如果不够则随机补0.4,

        private void 打开ToolStripMenuItem_Click(object sender, EventArgs e)
        {                                                                                                                                            
            打开ToolStripMenuItem.Checked = true;
            打开ToolStripMenuItem.Enabled = false;

            List<Sensor> mxs5000List = sensors.FindAll(s => s.SensorModel is MXS5000);
            foreach (Sensor mxs5000 in mxs5000List)
            {
                Thread thm = new Thread(MonitorData);
                thm.IsBackground = true;
                thm.Start(mxs5000);                
            }
        }
    }
}

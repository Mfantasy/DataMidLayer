using DataMidLayer.DeviceModel;
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
                西咸私有云发送ToolStripMenuItem.Visible = false;
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

            //我想要获取 当前处于Ex状态的设备列表(超时或离线都是Ex)
            //working状态下的设备 ( 离线是working (暂时发现有时候会一直连不上服务))
            //获取这些东西的目的是 根据数量判定服务是否运行稳定 




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
                try
                {
                    sensor.RefreshXmlData();
                }
                catch (Exception ex)
                {
                    Utils.WriteError(ex.Message + "\r\n" + DateTime.Now + "\r\n", "气象站数据接口调用异常.txt");
                    Thread.Sleep(50 * 1000); // 50秒一监测
                    continue;                       
                }
                
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
                             
                }
                //开始工作流程
                else
                {
                    DateTime now = DateTime.Parse(sensor.XmlTime);
                    if (lastTime != now)
                    {
                        //说明没有数据中断,判断设备当前状态,如果处于异常状态,则说明此时设备数据已恢复正常,进行计算处理                        
                        if (!sensor.StatusByXml && (now - lastTime).TotalMinutes > 6)
                        {                          
                            //计算
                            try
                            {                                
                                CalAndPost(sensor, lastRain, Math.Round(double.Parse(rain),1), lastTime,now);
                            }
                            catch (Exception ex)
                            {
                                Utils.WriteError(ex.Message+"\r\n"+DateTime.Now+"\r\n", "气象站数据补传异常CAP.txt");
                            }                            
                        }
                        //更新雨量,更新时间,更新状态,更新次数
                        sensor.StatusByXml = true;
                        
                        lastRain = Math.Round(double.Parse(rain), 1);
                        lastTime = DateTime.Parse(sensor.XmlTime);
                    }
                    else
                    {
                        
                        //说明设备50s内没有进行数据上行,数据中断,将设备状态false,times+1
                        sensor.StatusByXml = false;
                        
                        
                    }
                }
                Thread.Sleep(50 * 1000); // 50秒一监测
            }

        }

        private void Form1_Shown(object sender, EventArgs e)
        {
                        
        }
      
        private void CalAndPost(Sensor sensor, double lastRain, double rain,DateTime lastTime,DateTime now)
        {                   
            //计算具体逻辑.0.2为基准量
            //1.取增量 . 2.根据增量与基数0.2计算出应该补充的0.2的数量, 然后将数量与次数做比较,
            //如果少于0.2的数量少于需要post的次数,那么做随机分布算法. 如果数量多于次数, 那么取多出的次数,然后先次数0.2,再随机分布新0.2
            //3.循环,Post
            double rAdd = rain - lastRain;
            int countBase = (int)(rAdd / 0.2);
            DateTime tempTime = lastTime;
            int adTimes = (int)((now - lastTime).TotalMinutes / 3)-1;
            //首先得对翻斗值做特殊处理
            //先判断是否翻斗,然后再判断是否是正常翻斗
            if (rAdd < 0)
            {
                DataAccess.SendMail("雨量续传时翻斗或出现异常状况",string.Format("设备{0}\r\n历史雨量{1}\r\n最新雨量{2}",sensor.Name,lastRain,rain));

                if (lastRain > 25.6)
                {
                    //说明雨量是从25.6开始计算
                    rAdd = 51.2 - lastRain + rain;
                }
                else
                {
                    rAdd = 25.6 - lastRain + rain;                    
                }             
                countBase = (int)(rAdd / 0.2);
            }
            //如果base为0,说明没下雨,自动补
            if (countBase == 0)
            {
                for (int i = 0; i < adTimes; i++)
                {
                    tempTime = tempTime.AddMinutes(3);
                    (sensor.SensorModel as MXS5000).PostByXml(sensor, rain.ToString("0.0"), tempTime);
                    Console.WriteLine(rain.ToString("0.0") + "\t" + tempTime);
                }
            }
            else
            {
                //下雨了,判断base和time谁多
                int countTemp = adTimes - countBase;
                double rainTemp = lastRain;
                if (countTemp > 0)
                {
                    //次数多,随机分布0.2   
                    List<int> tiChu = GetTiChuRandomNum(adTimes, countTemp);
                    for (int i = 0; i < adTimes; i++)
                    {
                        tempTime = tempTime.AddMinutes(3);
                        if (!tiChu.Contains(i))
                        {
                            rainTemp = rainTemp + 0.2;
                            if (lastRain > 25.6)
                            {
                                if (Math.Round(rainTemp, 1) >= 51.2)
                                {
                                    rainTemp = 0;
                                }
                            }
                            else
                            {
                                if (Math.Round(rainTemp, 1) >= 25.6)
                                {
                                    rainTemp = 0;
                                }
                            }
                        }
                        Console.WriteLine(rainTemp + "\t" + tempTime);
                        (sensor.SensorModel as MXS5000).PostByXml(sensor, rainTemp.ToString("0.0"), tempTime);
                    }
                }
                else if (countTemp == 0)
                {
                    for (int i = 0; i < adTimes; i++)
                    {
                        tempTime = tempTime.AddMinutes(3);
                        rainTemp = rainTemp + 0.2;
                        if (lastRain > 25.6)
                        {
                            if (Math.Round(rainTemp, 1) >= 51.2)
                            {
                                rainTemp = 0;
                            }
                        }
                        else
                        {
                            if (Math.Round(rainTemp, 1) >= 25.6)
                            {
                                rainTemp = 0;
                            }
                        }
                        Console.WriteLine(rainTemp + "\t" + tempTime);
                        (sensor.SensorModel as MXS5000).PostByXml(sensor, rainTemp.ToString("0.0"), tempTime);
                    }
                }
                else if (countTemp < 0)
                {
                    //次数多,雨量次数少.先算增量累计次数及平均每次累加几个0.2,增量次数 = c*-1 / t 
                    int addTimes = countBase  / adTimes;
                    int exTimes = countBase % adTimes;

                    double addOnce = 0.2 * addTimes;

                    List<int> add = GetTiChuRandomNum(adTimes, exTimes);
                    for (int i = 0; i < adTimes; i++)
                    {
                        tempTime = tempTime.AddMinutes(3);
                        rainTemp = rainTemp + addOnce;
                        if (add.Contains(i))
                        {
                            rainTemp = rainTemp + 0.2;
                        }
                        if (lastRain > 25.6)
                        {
                            if (Math.Round(rainTemp,1) >= 51.2)
                            {
                                rainTemp = 0;
                            }
                        }
                        else
                        {
                            if (Math.Round(rainTemp,1) >= 25.6)
                            {
                                rainTemp = 0;
                            }
                        }
                        (sensor.SensorModel as MXS5000).PostByXml(sensor, rainTemp.ToString("0.0"), tempTime);
                        Console.WriteLine(rainTemp+"\t"+tempTime);
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

        private void 西咸私有云发送ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (DataAccess.IsSendSi)
            {
                西咸私有云发送ToolStripMenuItem.Text = "西咸私有云发送:关(当前)";
                DataAccess.IsSendSi = false;
            }
            else
            {
                西咸私有云发送ToolStripMenuItem.Text = "西咸私有云发送:开(当前)";
                DataAccess.IsSendSi = true;
            }
        }

        private void 异常设备统计ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<Sensor> exSensors = sensors.FindAll(ss => ss.IsEx);
            FormExCount fec = new FormExCount(exSensors);
            fec.Show();
        }

        private void toolStripStatusLabel2_Click(object sender, EventArgs e)
        {
            //刷新异常设备数量
            List<Sensor> exSensors = sensors.FindAll(ss => ss.IsEx);
            toolStripStatusLabel2.Text = "刷新异常设备数量: " + exSensors.Count;
        }
    }
}

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace DataMidLayer
{
    static class DataAccess
    {

        public static object lockObj = new object();
        /// <summary>
        /// 发送邮件(此方法占用网络时间,需用开线程调用)
        /// 需配置
        /// </summary>                
        public static void SendMail(string title, string body)
        {
            MailAddress EmailFrom = new MailAddress(ConfigurationManager.AppSettings["发件人地址"], ConfigurationManager.AppSettings["发件人昵称"]);
            MailMessage mailMsg = new MailMessage();
            mailMsg.From = EmailFrom;
            string receivers = ConfigurationManager.AppSettings["邮件接收人"];
            mailMsg.To.Add(receivers);
            mailMsg.Subject = title;
            mailMsg.Body = body;
            SmtpClient spClient = new SmtpClient(ConfigurationManager.AppSettings["发件服务器"]);
            spClient.Timeout = 600 * 1000;
            spClient.EnableSsl = true;
            spClient.Credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["发件用户名"], ConfigurationManager.AppSettings["发件密码"]);

            try
            {
                spClient.Send(mailMsg);
            }
            catch (System.Net.Mail.SmtpException ex)
            {
                lock (lockObj)
                {
                    File.AppendAllText("error.txt", ex.Message + "\r\n发送邮件失败\r\n" + title + "\r\n" + body + "\r\n" + DateTime.Now.ToString());
                }
            }
        }
        //HttpGetStream
        public static Stream HttpGet(string url)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse receiveStream = (HttpWebResponse)req.GetResponse();
            return receiveStream.GetResponseStream();
        }
        //序列化
        public static T SerializeXml<T>(Stream stream)
        {
            XmlSerializerFactory factory = new XmlSerializerFactory();
            XmlSerializer serializer = factory.CreateSerializer(typeof(T));
            object cacheData = serializer.Deserialize(stream);
            return cacheData == null ? default(T) : (T)cacheData;
        }
    }

    public static class DataSubscribe
    {
        public static void BeginSubscribe(List<Sensor> listSensor)
        {
            foreach (Sensor item in listSensor)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback((o) => GetAndPost(item, "进入GetAndPost方法", 0)));
            }
        }
        static void WriteError(string msg)
        {
            lock (obj)
            {
                File.AppendAllText("error.txt", msg);
            }
        }

        static object obj = new object();
        static int exId = 0;
        public static void GetAndPost(Sensor ss, string log1, int times)
        {
            int ini = 0;
            ss.Log.Add(DateTime.Now.ToString() + log1);  
              ss.IsWorking = true;
            TcpClient tcp = new TcpClient();
            NetworkStream stream = null;
            string ip = ConfigurationManager.AppSettings["ip"];
            int port = Int32.Parse(ConfigurationManager.AppSettings["port"]);
            //开始连接json9011实时数据接口
            try
            {
                tcp.Connect(ip, port);
                stream = tcp.GetStream();
                tcp.ReceiveTimeout = ss.Config.OverTimeM * 60 * 1000;
                byte[] cmd = Encoding.UTF8.GetBytes(ss.Feed);
                stream.Write(cmd, 0, cmd.Length);
            }
            catch (Exception exGet)
            {
                ss.ExCatched();
                ss.Log.Add(string.Format("在{0}因为{1}断线", DateTime.Now, exGet.Message));
                ss.IsEx = true;

                exId++;
                string msg = string.Format("异常索引:{0}\r\n异常设备:{1}\r\n异常时间{2}\r\n异常名称:{3}\r\n异常备注:{4}\r\n", exId, ss.Name + " " + ss.Type, DateTime.Now, exGet.Message, "订阅异常");
                WriteError(msg);

                ss.IsWorking = false;
                if (tcp != null && tcp.Connected)
                {
                    tcp.Close();
                }
                Thread.Sleep(3 * 1000);
                return;
            }
            //订阅成功后开始搞事情,处理处理数据流
            {
                if (ss.Log.Count < 2)
                    ss.Log.Add("");
                while (true)
                {
                    try
                    {
                        byte[] bytes = new byte[1024 * 4];
                        int l = stream.Read(bytes, 0, bytes.Length);
                        string jstr = Encoding.UTF8.GetString(bytes, 0, l);
                        if (jstr.EndsWith("\"name\":"))//数据包中断
                        {
                            byte[] byteAdd = new byte[1024 * 4];
                            int ladd = stream.Read(byteAdd, 0, byteAdd.Length);
                            jstr += Encoding.UTF8.GetString(byteAdd, 0, ladd);
                        }
                        if (jstr.Length < 128)
                        {
                            if (ini == 0)
                            {
                                ini++;
                                continue;
                            }
                            ss.IsWorking = false;
                            ss.Log.Add("收到异常数据包 " + DateTime.Now.ToString());
                            ss.IsEx = true;
                            if (tcp != null && tcp.Connected)
                            {
                                tcp.Close();
                            }
                            Thread.Sleep(3 * 1000);
                            return;
                        }
                        try  //json格式校验
                        { JObject.Parse(jstr); }
                        catch { ss.Log.Add("json解析失败 " + DateTime.Now.ToString()); continue; }
                        try  //Post
                        {
                            ss.SensorModel.Post(jstr, ss);
                            times++;
                            ss.Log[1] = DateTime.Now.ToString() + " 次数:\t" + times.ToString();
                            ss.IsEx = false;
                        }
                        catch (Exception exPost)
                        {
                            ss.ExCatched();

                            exId++;
                            string msg = string.Format("异常索引:{0}\r\n异常设备:{1}\r\n异常时间{2}\r\n异常名称:{3}\r\n异常备注:{4}\r\n", exId, ss.Name + " " + ss.Type, DateTime.Now, exPost.Message, "Post异常");
                            WriteError(msg);

                        }
                    }
                    catch (Exception exM)
                    {
                        ss.IsEx = true;
                        exId++;
                        string msg = string.Format("异常索引:{0}\r\n异常设备:{1}\r\n异常时间{2}\r\n异常名称:{3}\r\n异常备注:{4}\r\n", exId, ss.Name + " " + ss.Type, DateTime.Now, exM.Message, "连接异常");

                        WriteError(msg);

                        if (exM.HResult == -2146232800) //特定超时异常
                        {
                            ss.Log.Add(DateTime.Now.ToString()+" 启动数据缓存线程");
                            ThreadPool.QueueUserWorkItem(new WaitCallback((o) => MoniPost(ss)));
                            ThreadPool.QueueUserWorkItem(new WaitCallback((o) => GetAndPost(ss,  "因为超时重新进入GetAndPost方法", times)));
                            if (tcp != null && tcp.Connected)
                            {
                                tcp.Close();
                            }
                            Thread.Sleep(3 * 1000);
                            return;
                        }
                        else //其他位置异常
                        {
                            ThreadPool.QueueUserWorkItem(new WaitCallback((o) => GetAndPost(ss, "因为其他异常重新进入GetAndPost方法", times)));
                            if (tcp != null && tcp.Connected)
                            {
                                tcp.Close();
                            }
                            Thread.Sleep(3 * 1000);
                            return;
                        }
                    }
                }
            }
        }

        public static void MoniPost(Sensor ss)
        {
            //出现异常了,首先得再启动一个线程不断去接收,等待连接.等正常数据来了.ex = false.然后发个邮件通知
            //然后再启动一个模拟线程去给sw发送模拟数据.等ex=false.停.
            while (ss.IsEx)
            {
                try
                {
                    Thread.Sleep(ss.Config.MoniIntervalM * 60 * 1000);
                    if (ss.Config.Moni)
                    {
                        ss.SensorModel.MoniPostData(ss);
                    }
                }
                catch { }
            }
            ss.Log.Add(DateTime.Now.ToString() + "缓存结束");
        }

        public static void StartSubscribe(Sensor ss)
        {
            TcpClient tcp = new TcpClient();
            NetworkStream streamToServer = null;
            try
            {
                tcp.Connect(ConfigurationManager.AppSettings["ip"], Int32.Parse(ConfigurationManager.AppSettings["port"]));
                //发送指令
                streamToServer = tcp.GetStream();

                tcp.ReceiveTimeout = ss.Config.OverTimeM * 60 * 1000;
                //string command = "{ method: \"subscribe\", headers: undefined, resource: \"/fengxi/" + fengxiGateway + "\", token: 0 }";
                string command = ss.Feed;
                byte[] bufferS = Encoding.UTF8.GetBytes(command); //msg为发送的字符串 

                streamToServer.Write(bufferS, 0, bufferS.Length);
            }
            catch (Exception ex)
            {
                ss.Error.Msg = ex.Message;
                ss.Error.StactTrace = ex.StackTrace;
                ss.ExCatched();
                Thread.Sleep(10 * 1000);
                ThreadPool.QueueUserWorkItem(new WaitCallback((o) => DataAccess.SendMail("[异常]数据转发", "发送订阅指令时异常\r\n" + ex.Message + "\r\n" + ex.StackTrace + "\r\n" + ss.Name)));
                return;
            }
            ss.Log.Add("\t订阅实时数据成功\t" + DateTime.Now.ToString());
            int i = 0;
            while (tcp.Connected)
            {
                try
                {
                    byte[] bufferR = new byte[1024 * 16]; int bfLength = 0;
                    bfLength = streamToServer.Read(bufferR, 0, bufferR.Length);
                    string str = Encoding.UTF8.GetString(bufferR, 0, bfLength);
                    if (str.EndsWith("\"name\":"))//处理数据包中断
                    {
                        byte[] bufferR0 = new byte[1024 * 16];
                        int bfLength0 = streamToServer.Read(bufferR0, 0, bufferR0.Length);
                        str += Encoding.UTF8.GetString(bufferR0, 0, bfLength0);
                    }
                    if (str.Length < 256)
                        continue;
                    try
                    {
                        JObject.Parse(str);
                    }
                    catch
                    {
                        continue;
                    }
                    try
                    {
                        ss.SensorModel.Post(str, ss);
                    }
                    catch (Exception pex)
                    {
                        ss.Error.Msg = pex.Message;
                        ss.Error.StactTrace = pex.StackTrace;
                        ss.ExCatched();
                        //ss.Log.Add("siteWhere服务器异常" + DateTime.Now.ToString());
                        //ThreadPool.QueueUserWorkItem(new WaitCallback((o) => DataAccess.SendMailUseZj("[异常]数据转发", "siteWhere服务器异常\r\n" + pex.Message + "\r\n" + pex.StackTrace + "\r\n" + ss.Name)));
                        continue;
                    }
                    if (i == 0)
                    {
                        ss.Log.Add("\t首次post到siteWhere请求成功\t" + DateTime.Now.ToString());
                        ss.Log.Add("\t次数:\t" + i.ToString() + "\t" + DateTime.Now.ToString());
                    }
                    i++;
                    ss.Log[2] = "\t次数:\t" + i.ToString() + "\t" + DateTime.Now.ToString();
                    ss.IsEx = false;
                }
                catch (Exception ex)
                {
                    if (ex.HResult == -2146232800)
                    {
                        ThreadPool.QueueUserWorkItem(new WaitCallback((o) => ExSubscribe(ss, i)));
                        ss.IsEx = true;
                        ss.Log.Add(ss.Name + "\t超时\t尝试重连 . . .," + DateTime.Now.ToString());
                        ThreadPool.QueueUserWorkItem(new WaitCallback((o) => MoniPost(ss)));
                        break;
                    }
                    else
                    {
                        //如果不是超时异常,那么重新订阅.                               
                        ss.Error.Msg = ex.Message;
                        ss.Error.StactTrace = ex.StackTrace;
                        ss.ExCatched();
                        Thread.Sleep(10 * 60 * 1000);
                        ThreadPool.QueueUserWorkItem(new WaitCallback((o) => ReSubscribe(ss, i)));
                        ss.Log.Add("\t" + ex.Message + "\t" + DateTime.Now);
                        break;
                    }
                }
            }
        }

        public static void ReSubscribe(Sensor ss, int i)
        {
            TcpClient tcp = new TcpClient();
            NetworkStream streamToServer = null;
            try
            {
                tcp.Connect(ConfigurationManager.AppSettings["ip"], Int32.Parse(ConfigurationManager.AppSettings["port"]));
                //发送指令
                streamToServer = tcp.GetStream();
                tcp.ReceiveTimeout = ss.Config.OverTimeM * 60 * 1000;
                string command = ss.Feed;
                byte[] bufferS = Encoding.UTF8.GetBytes(command); //msg为发送的字符串 

                streamToServer.Write(bufferS, 0, bufferS.Length);
            }
            catch (Exception ex)
            {
                ss.Error.Msg = ex.Message;
                ss.Error.StactTrace = ex.StackTrace;
                ss.ExCatched();
                Thread.Sleep(10 * 1000);
                ThreadPool.QueueUserWorkItem(new WaitCallback((o) => DataAccess.SendMail("[异常]数据转发", "发送订阅指令时异常\r\n" + ex.Message + "\r\n" + ex.StackTrace + "\r\n" + ss.Name)));
                return;
            }
            ss.Log.Add("\t重连成功\t" + DateTime.Now.ToString());
            int j = 0;
            while (tcp.Connected)
            {
                try
                {
                    byte[] bufferR = new byte[1024 * 16]; int bfLength = 0;
                    bfLength = streamToServer.Read(bufferR, 0, bufferR.Length);
                    string str = Encoding.UTF8.GetString(bufferR, 0, bfLength);
                    if (str.EndsWith("\"name\":"))//处理数据包中断
                    {
                        byte[] bufferR0 = new byte[1024 * 16];
                        int bfLength0 = streamToServer.Read(bufferR0, 0, bufferR0.Length);
                        str += Encoding.UTF8.GetString(bufferR0, 0, bfLength0);
                    }
                    if (str.Length < 256)
                        continue;
                    try
                    {
                        JObject.Parse(str);
                    }
                    catch
                    {
                        continue;
                    }
                    try
                    {
                        ss.SensorModel.Post(str, ss);
                    }
                    catch (Exception pex)
                    {
                        ss.Error.Msg = pex.Message;
                        ss.Error.StactTrace = pex.StackTrace;
                        ss.ExCatched();
                        //ss.Log.Add("siteWhere服务器异常" + DateTime.Now.ToString());
                        //ThreadPool.QueueUserWorkItem(new WaitCallback((o) => DataAccess.SendMailUseZj("[异常]数据转发", "siteWhere服务器异常\r\n" + pex.Message + "\r\n" + pex.StackTrace + "\r\n" + ss.Name)));
                        continue;
                    }
                    if (j == 0)
                    {
                        ss.Log.Add("\t重连post到siteWhere请求成功\t" + DateTime.Now.ToString());
                    }
                    ss.IsEx = false;
                    i++;
                    j++;
                    ss.Log[2] = "\t次数:\t" + i.ToString() + "\t" + DateTime.Now.ToString();
                    ss.IsEx = false;
                }
                catch (Exception ex)
                {
                    try { tcp.Close(); } catch (Exception exx) { ss.Log.Add(exx.Message); }
                    if (ex.HResult == -2146232800)
                    {
                        ThreadPool.QueueUserWorkItem(new WaitCallback((o) => ExSubscribe(ss, i)));
                        ss.IsEx = true;
                        ss.Log.Add(ss.Name + "\t超时\t已尝试重连" + DateTime.Now.ToString());

                        if (ss.Config.Remind)
                        {
                            ThreadPool.QueueUserWorkItem(new WaitCallback((o) => DataAccess.SendMail(ss.Name + "\t超时", ss.Addr)));
                        }
                        ThreadPool.QueueUserWorkItem(new WaitCallback((o) => MoniPost(ss)));

                        break;
                    }
                    else
                    {

                        ss.Error.Msg = ex.Message;
                        ss.Error.StactTrace = ex.StackTrace;
                        ss.ExCatched();
                        ThreadPool.QueueUserWorkItem(new WaitCallback((o) => ReSubscribe(ss, i)));
                        ss.Log.Add("\t" + ex.Message + "\t" + DateTime.Now + "\t尝试重连");
                        break;
                    }
                }
            }
        }

        public static void ExSubscribe(Sensor ss, int i)
        {
            TcpClient tcp = new TcpClient();
            tcp.ReceiveTimeout = ss.Config.OverTimeM * 60 * 1000;
            NetworkStream streamToServer = null;
            try
            {
                tcp.Connect(ConfigurationManager.AppSettings["ip"], Int32.Parse(ConfigurationManager.AppSettings["port"]));
                //发送指令
                streamToServer = tcp.GetStream();
                string command = ss.Feed;
                byte[] bufferS = Encoding.UTF8.GetBytes(command); //msg为发送的字符串 

                streamToServer.Write(bufferS, 0, bufferS.Length);
            }
            catch (Exception ex)
            {
                ss.Error.Msg = ex.Message;
                ss.Error.StactTrace = ex.StackTrace;
                ss.ExCatched();
                Thread.Sleep(10 * 1000);
                ThreadPool.QueueUserWorkItem(new WaitCallback((o) => DataAccess.SendMail("[异常]数据转发", "发送订阅指令时异常\r\n" + ex.Message + "\r\n" + ex.StackTrace + "\r\n" + ss.Name)));
                return;
            }

            while (tcp.Connected)
            {
                try
                {
                    byte[] bufferR = new byte[1024 * 16]; int bfLength = 0;
                    bfLength = streamToServer.Read(bufferR, 0, bufferR.Length);
                    string str = Encoding.UTF8.GetString(bufferR, 0, bfLength);
                    if (str.EndsWith("\"name\":"))//处理数据包中断
                    {
                        byte[] bufferR0 = new byte[1024 * 16];
                        int bfLength0 = streamToServer.Read(bufferR0, 0, bufferR0.Length);
                        str += Encoding.UTF8.GetString(bufferR0, 0, bfLength0);
                    }
                    if (str.Length < 256)
                        continue;
                    try
                    {
                        JObject.Parse(str);
                    }
                    catch
                    {
                        continue;
                    }
                    try
                    {
                        ss.SensorModel.Post(str, ss);
                    }
                    catch (Exception pex)
                    {
                        ss.Error.Msg = pex.Message;
                        ss.Error.StactTrace = pex.StackTrace;
                        ss.ExCatched();
                        // ss.Log.Add("siteWhere服务器异常" + DateTime.Now.ToString());
                        //ThreadPool.QueueUserWorkItem(new WaitCallback((o) => DataAccess.SendMailUseZj("[异常]数据转发", "siteWhere服务器异常\r\n" + pex.Message + "\r\n" + pex.StackTrace + "\r\n" + ss.Name)));
                        continue;
                    }
                    ss.IsEx = false;
                    ThreadPool.QueueUserWorkItem(new WaitCallback((o) => ReSubscribe(ss, i)));
                    ss.Log.Add("\t转发已恢复正常\t" + DateTime.Now.ToString());
                    break;

                }
                catch (Exception ex)
                {
                    try { tcp.Close(); } catch (Exception exx) { ss.Log.Add(exx.Message); }
                    //ss.Error.Msg = ex.Message;
                    //ss.Error.StactTrace = ex.StackTrace;
                    //ss.ExCatched();
                    //Thread.Sleep(10 * 60 * 1000);
                    ThreadPool.QueueUserWorkItem(new WaitCallback((o) => ExSubscribe(ss, i)));

                    break;
                }
            }



        }
    }
}

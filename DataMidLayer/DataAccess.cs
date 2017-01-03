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

        public static void SendMailUseZj(string title, string body)
        {
            MailAddress EmailFrom = new MailAddress("mfantasy@mfant.com");
            MailAddress EmailTo = new MailAddress(ConfigurationManager.AppSettings["邮件接收人"]);
            MailMessage mailMsg = new MailMessage(EmailFrom, EmailTo);
            mailMsg.CC.Add(ConfigurationManager.AppSettings["邮件抄送"]);
            mailMsg.Subject = title;
            mailMsg.Body = body;
            SmtpClient spClient = new SmtpClient("smtp.qq.com");
            spClient.Timeout = 600*1000;
            spClient.EnableSsl = true;
            spClient.Credentials = new System.Net.NetworkCredential("mfantasy@mfant.com", "ryqmwpnrmoygcbdd");

            try
            {
                spClient.Send(mailMsg);
            }
            catch (System.Net.Mail.SmtpException ex)
            {
                File.AppendAllText("error.txt", ex.Message + "\r\n发送邮件失败\r\n" + title + "\r\n" + body+"\r\n"+DateTime.Now.ToString());
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
                ThreadPool.QueueUserWorkItem(new WaitCallback((o) => StartSubscribe(item)));
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
                    if (ss.Config.Moni)
                    {
                        ss.SensorModel.MoniPostData(ss);                    
                    }
                    Thread.Sleep(ss.Config.MoniIntervalM * 60 * 1000);
                }
                catch (Exception ex)
                {
                    ss.Error.Msg = ex.Message;
                    ss.Error.StactTrace = ex.StackTrace;
                    ss.ExCatched();
                    ss.Log.Add("模拟数据:siteWhere服务器异常" + DateTime.Now.ToString());
                    return;
                }             
            }
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

                ThreadPool.QueueUserWorkItem(new WaitCallback((o) => DataAccess.SendMailUseZj("[异常]数据转发", "发送订阅指令时异常\r\n" + ex.Message + "\r\n" + ex.StackTrace + "\r\n" + ss.Name)));
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
                        ss.Log.Add("siteWhere服务器异常" + DateTime.Now.ToString());
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
                        try { tcp.Close(); } catch { }
                        ss.IsEx = true;
                        ss.Log.Add(ss.Name+"\t超时"+DateTime.Now.ToString());
                        ThreadPool.QueueUserWorkItem(new WaitCallback((o) => ExSubscribe(ss, i)));
                        if (ss.Config.Remind)
                        {
                            ThreadPool.QueueUserWorkItem(new WaitCallback((o) => DataAccess.SendMailUseZj(ss.Name + "\t超时", "软件启动以来第一次出现异常的设备\r\n"+ss.Addr)));
                        }                                                
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

                        ss.Log.Add("\t" + ex.Message + "\t" + DateTime.Now + "\t尝试重连");
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
                ThreadPool.QueueUserWorkItem(new WaitCallback((o) => DataAccess.SendMailUseZj("[异常]数据转发", "发送订阅指令时异常\r\n" + ex.Message + "\r\n" + ex.StackTrace + "\r\n" + ss.Name)));
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
                        ss.Log.Add("siteWhere服务器异常" + DateTime.Now.ToString());
                        continue;
                    }
                    if (j == 0)
                    {
                        ss.Log.Add("\t重连post到siteWhere请求成功\t" + DateTime.Now.ToString());
                    }
                    i++;
                    j++;
                    ss.Log[2] = "\t次数:\t" + i.ToString() + "\t" + DateTime.Now.ToString();
                    ss.IsEx = false;
                }
                catch (Exception ex)
                {
                    if (ex.HResult == -2146232800)
                    {
                        try { tcp.Close(); } catch {  }
                        ss.IsEx = true;
                        ss.Log.Add(ss.Name + "\t超时" + DateTime.Now.ToString());
                        ThreadPool.QueueUserWorkItem(new WaitCallback((o) => ExSubscribe(ss, i)));
                        if (ss.Config.Remind)
                        {
                            ThreadPool.QueueUserWorkItem(new WaitCallback((o) => DataAccess.SendMailUseZj(ss.Name + "\t超时", ss.Addr)));
                        }
                        //出现异常了,首先得再启动一个线程不断去接收,等待连接.等正常数据来了.ex = false.然后发个邮件通知
                        //然后再启动一个模拟线程去给sw发送模拟数据.等ex=false.停.                        
                            ThreadPool.QueueUserWorkItem(new WaitCallback((o) => MoniPost(ss)));
                        
                        break;
                    }
                    else
                    {
                                               
                        ss.Error.Msg = ex.Message;
                        ss.Error.StactTrace = ex.StackTrace;
                        ss.ExCatched();
                        Thread.Sleep(10 * 60 * 1000);
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
                ThreadPool.QueueUserWorkItem(new WaitCallback((o) => DataAccess.SendMailUseZj("[异常]数据转发", "发送订阅指令时异常\r\n" + ex.Message + "\r\n" + ex.StackTrace + "\r\n" + ss.Name)));
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
                        ss.Log.Add("siteWhere服务器异常"+DateTime.Now.ToString());
                        continue;
                    }
                    ss.IsEx = false;
                    ss.Log.Add("\t超时恢复\t" + DateTime.Now.ToString());
                    if (ss.Config.Remind)
                    {
                        ThreadPool.QueueUserWorkItem(new WaitCallback((o) => DataAccess.SendMailUseZj(ss.Name + "\t已恢复正常", "")));
                    }
                    ThreadPool.QueueUserWorkItem(new WaitCallback((o) => ReSubscribe(ss, i)));
                    
                    break;

                }
                catch (Exception ex)
                {
                    ss.Error.Msg = ex.Message;
                    ss.Error.StactTrace = ex.StackTrace;
                    ss.ExCatched();
                    Thread.Sleep(10 * 60 * 1000);
                    ThreadPool.QueueUserWorkItem(new WaitCallback((o) => ExSubscribe(ss, i)));

                    break;
                }
            }



        }
    }
}

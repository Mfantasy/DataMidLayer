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
        public static void SendMailUseZj(string title,string body)
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
            }
            catch (System.Net.Mail.SmtpException ex)
            {
                File.AppendAllText("error.txt", ex.Message+"\r\n发送邮件失败\r\n"+title+"\r\n"+body);
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

        public static void StartSubscribe(Sensor ss)
        {           
            TcpClient tcp = new TcpClient();
            NetworkStream streamToServer =null;
            try
            {
                tcp.Connect(ConfigurationManager.AppSettings["ip"], Int32.Parse(ConfigurationManager.AppSettings["port"]));
                //发送指令
                streamToServer = tcp.GetStream();
                //tcp.ReceiveTimeout = ss.Config.OverTimeM*60*1000;
                tcp.ReceiveTimeout = 2000;
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
                //Mail to wodeyouxiang 订阅失败.请检查.
                
                //Thread th = new Thread(new ThreadStart(() => StartSubscribe(ss)));
                //th.Start();                
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
                    ss.SensorModel.Post(str, ss);
                    if (i == 0)
                    {
                        ss.Log.Add("\t首次post到siteWhere请求成功\t" + DateTime.Now.ToString());
                        ss.Log.Add("\t次数:\t" + i.ToString() + "\t" + DateTime.Now.ToString());
                    }
                    i++;
                    ss.Log[2] = "\t次数:\t" + i.ToString() + "\t" + DateTime.Now.ToString();
                }
                catch (Exception ex)
                {
                    if (ex.HResult == -2146232800)
                    {
                        MessageBox.Show("超市啦");
                        //如果异常是因为超时没收到数据.那么我们就模拟一份继续转发.
                        //并且发送邮件提醒我自己
                        //等到真实数据来了,取消掉此功能.
                        break;
                    }
                    else
                    {
                        //如果不是超时异常,那么重新订阅.
                        Console.WriteLine("EX");
                        ss.Error.Msg = ex.Message;
                        ss.Error.StactTrace = ex.StackTrace;
                        ss.ExCatched();
                        Thread.Sleep(10 * 60 * 1000);
                        Thread th = new Thread(new ThreadStart(() => StartSubscribe(ss)));
                        th.Start();
                        break;
                    }
                }
            }
        }
      

      


    }
}

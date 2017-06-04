using DataMidLayer.Device;
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
        public static void SendMail(string title, string body, string rec="")
        {
            MailAddress EmailFrom = new MailAddress(ConfigurationManager.AppSettings["发件人地址"], ConfigurationManager.AppSettings["发件人昵称"]);
            MailMessage mailMsg = new MailMessage();
            mailMsg.From = EmailFrom;
            string receivers = ConfigurationManager.AppSettings["邮件接收人"];
            if (rec == "")
            {
                mailMsg.To.Add(receivers);
            }
            else
            {
                mailMsg.To.Add(rec);
            }
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
                tcp.ReceiveTimeout = ss.OverTimeM * 60 * 1000;
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
                Utils.WriteError(msg);

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
                            Utils.WriteError(msg);

                        }
                    }
                    catch (Exception exM)
                    {
                        ss.IsEx = true;
                        ss.ExCatched();
                        exId++;
                        string msg = string.Format("异常索引:{0}\r\n异常设备:{1}\r\n异常时间{2}\r\n异常名称:{3}\r\n异常备注:{4}\r\n", exId, ss.Name + " " + ss.Type, DateTime.Now, exM.Message, "连接异常");

                        Utils.WriteError(msg);

                        if (exM.HResult == -2146232800) //特定超时异常
                        {
                            if (ss.SensorModel is MXS5000 && isMain)
                            {
                                DataAccess.SendMail(ss.Name.Remove(4) + "超时", "", "mengfantong@smeshlink.com");
                            }
                            if (!ss.IsXmlPosting)
                            {
                                ss.IsXmlPosting = true;
                                ss.Log.Add(DateTime.Now.ToString() + " 启动PostByXmlData线程");
                                ThreadPool.QueueUserWorkItem(new WaitCallback((o) => PostByXml(ss)));
                            }
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

        static bool isMain = ConfigurationManager.AppSettings["url"] == "http://hm-iot.chinacloudapp.cn:80/api/deviceEvents";
        public static void PostByXml(Sensor ss)
        {
            //出现异常了,首先得再启动一个线程不断去接收,等待连接.等正常数据来了.ex = false.然后发个邮件通知
            //然后再启动一个模拟线程去给sw发送模拟数据.等ex=false.停.           
            ss.RefreshXmlData();
            while (ss.IsEx)
            {
                try
                {                    
                    if (ss.Moni)
                    {
                        ss.SensorModel.PostDataByXml(ss);
                    }
                    Thread.Sleep(ss.SensorModel.Interval * 1000);
                }
                catch { }                
            }
            ss.Log.Add(DateTime.Now.ToString() + "PostByXml结束");
            ss.IsXmlPosting = false;
            if (ss.SensorModel is MXS5000 && isMain)
            {
                DataAccess.SendMail(ss.Name.Remove(4) + "恢复", "", "mengfantong@smeshlink.com");
            }
        }            
    }

    public static class PostS
    {          
        static int postExIndex = 0;
        static string url = ConfigurationManager.AppSettings["url"];
        public static void PostToSW(string deviceId, int index, string data)
        {
            string postData = GetJson(deviceId, index.ToString(), data);
            try
            {
                RequestPost(url, postData);
            }
            catch (Exception ex)
            {
                postExIndex++;
                string exMsg = string.Format("索引:{0}\r\n异常信息:{1}\r\n异常地址:{2}\r\n异常时间:{3}", postExIndex, ex.Message, url,DateTime.Now);
                postDatas.Add(postData);
                Utils.WriteError(exMsg, "enno异常列表.txt");
            }                                    
        }

        public static void PostToSW(string deviceId, int index, string data,DateTime dtime)
        {
            string time = ((long)ToUnixTimestamp(dtime)).ToString();
            string postData = GetJson(deviceId, index.ToString(), data,time);
            try
            {
                RequestPost(url, postData);
            }
            catch (Exception ex)
            {
                postExIndex++;
                string exMsg = string.Format("索引:{0}\r\n异常信息:{1}\r\n异常地址:{2}\r\n记录时间:{3}", postExIndex, ex.Message, url, dtime);
                postDatas.Add(postData);
                Utils.WriteError(exMsg, "enno异常列表(气象站数据补录).txt");
            }
        }

        //数据缓存机制
        static List<string> postDatas = new List<string>();
        static int times = 0;
        public static void QuePost()
        {
            //有个死循环,死循环不断去读错误列表,如果错误列表有项,那么就Req.Req失败了,休息5秒继续Req.
            while (true)
            {
                if (postDatas.Count > 0)
                {
                    string postData = postDatas[0];
                    postDatas.RemoveAt(0);
                    try
                    {
                        RequestPost(url, postData);
                        times ++;
                        Program.Fm1.Invoke(new Action(() => Program.Fm1.toolStripStatusLabel1.Text ="缓存次数" + times.ToString() + " 最新时间" +DateTime.Now.ToString()));
                    }
                    catch (Exception ex)
                    {
                        postDatas.Add(postData);
                    }
                }
                Thread.Sleep(3000);
            }
        }


        
        readonly static DateTime UnixTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private static Double ToUnixTimestamp(DateTime date)
        {
            if (date.Kind != DateTimeKind.Utc)
                date = date.ToUniversalTime();
            return (date - UnixTime).TotalMilliseconds;
        }
        public static string GetJson(string deviceId, string index, string data)
        {
            string time = ((long)ToUnixTimestamp(DateTime.Now)).ToString();
            JObject jobj = new JObject();
            jobj.Add("deviceId", deviceId);
            jobj.Add("attributeIndex", index);
            jobj.Add("attributeData", data);
            jobj.Add("ingressTime", time);
            jobj.Add("deviceTime", time);
            jobj.Add("source", "");
            return jobj.ToString();
        }

        public static string GetJson(string deviceId, string index, string data, string time)
        {
            JObject jobj = new JObject();
            jobj.Add("deviceId", deviceId);
            jobj.Add("attributeIndex", index);
            jobj.Add("attributeData", data);
            jobj.Add("ingressTime", time);
            jobj.Add("deviceTime", time);
            jobj.Add("source", "");
            return jobj.ToString();
        }

        private static void RequestPost(string posturl, string postData)
        {           
            Stream outstream = null;
            Stream instream = null;
            StreamReader sr = null;
            HttpWebResponse response = null;
            HttpWebRequest request = null;
            Encoding encoding = Encoding.UTF8;
            byte[] data = encoding.GetBytes(postData);
            // 准备请求...
            try
            {
                // 设置参数
                request = WebRequest.Create(posturl) as HttpWebRequest;
                CookieContainer cookieContainer = new CookieContainer();
                request.CookieContainer = cookieContainer;
                request.AllowAutoRedirect = true;
                request.Method = "POST";
                request.Timeout = 30 * 1000;
                request.Headers.Add("Authorization", "Basic ZXRhZG1pbkBzaXRlOmFiY2QxMjM=");
                request.ContentType = "application/json";
                request.ContentLength = data.Length;
                outstream = request.GetRequestStream();
                outstream.Write(data, 0, data.Length);
                outstream.Close();
                //发送请求并获取相应回应数据                
                response = request.GetResponse() as HttpWebResponse;
                ////直到request.GetResponse()程序才开始向目标网页发送Post请求
                instream = response.GetResponseStream();
                sr = new StreamReader(instream, encoding);
                ////返回结果网页（html）代码
                string content = sr.ReadToEnd();
                ////string err = string.Empty;
                ////HttpContext.Current.Response.Write(content);
                //Console.WriteLine(content);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}

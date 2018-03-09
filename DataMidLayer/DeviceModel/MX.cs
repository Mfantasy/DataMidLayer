using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace DataMidLayer.DeviceModel
{
    public abstract class MX
    {
        //频率波动(s)        
        public virtual int Interval
        {
            get
            {
                return 5 * 60 + AddRandom(100);
            }
        }
        Random r = new Random();
        protected int AddRandom(int range)
        {
            return r.Next(-1 * range, range);
        }

        private string title;

        public string Titile
        {
            get { return title; }
            set { title = value; }
        }
        private string time;

        public string Time
        {
            get { return time; }
            set { time = value; }
        }

        public void Post(string str,Sensor ss)
        {
            ss.LastTime = DateTime.Now;
            JObject jobj = JObject.Parse(str);
            PostData(jobj,ss);
        }

        protected abstract void PostData(JObject jobj,Sensor ss);
        public abstract void PostDataByXml(Sensor ss);
            
    }
     
}

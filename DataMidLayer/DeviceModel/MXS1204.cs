using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json.Linq;
using static DataMidLayer.DataSubscribe;

namespace DataMidLayer.DeviceModel
{
    class MXS1204 : MX
    {
        public override int Interval
        {
            get
            {
                return 5 * 60 + AddRandom(50);
            }
        }
        private string temperature;

        public string Temperature
        {
            get { return temperature; }
            set { temperature = value; }
        }
        private string pressure;

        public String Pressure
        {
            get { return pressure; }
            set { pressure = value; }
        }
        private string height;

        public String Height
        {
            get { return height; }
            set { height = value; }
        }

        protected override void PostData(JObject jobj, Sensor ss)
        {
            Temperature = jobj["body"]["children"][2]["data"][0]["value"].ToString();
            Pressure = jobj["body"]["children"][3]["data"][0]["value"].ToString();
            Height = jobj["body"]["children"][4]["data"][0]["value"].ToString();
            PostS.PostToSW(ss.SiteWhereId, 1, Temperature);
            PostS.PostToSW(ss.SiteWhereId, 2, Pressure);
            PostS.PostToSW(ss.SiteWhereId, 3, Height);
        }
        public override void PostDataByXml(Sensor ss)
        {
            PostS.PostToSW(ss.SiteWhereId, 1, ss.XmlValues[2]);
            PostS.PostToSW(ss.SiteWhereId, 2, ss.XmlValues[3]);
            PostS.PostToSW(ss.SiteWhereId, 3, ss.XmlValues[4]);
        }
    }
}

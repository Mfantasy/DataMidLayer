using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json.Linq;
using static DataMidLayer.DataSubscribe;

namespace DataMidLayer.Device
{
    class MXS1501:MX
    {
        public override int Interval
        {
            get
            {
                return 5 * 60 + AddRandom(50);
            }
        }
        private string light;

        public String Light
        {
            get { return light; }
            set { light = value; }
        }

        protected override void PostData(JObject jobj, Sensor ss)
        {
            Light = jobj["body"]["children"][2]["data"][0]["value"].ToString();
            PostS.PostToSW(ss.SiteWhereId, 1, Light);
        }

        public override void PostDataByXml(Sensor ss)
        {
            PostS.PostToSW(ss.SiteWhereId, 1, ss.XmlValues[2]);           
        }
    }
}

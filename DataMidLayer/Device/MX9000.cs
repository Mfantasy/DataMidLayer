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
    class MX9000:MX
    {
        private string waterLevel;

        public string WaterLevel
        {
            get { return waterLevel; }
            set { waterLevel = value; }
        }

        private string flow;

        public string Flow
        {
            get { return flow; }
            set { flow = value; }
        }
        private string temp;

        public string Temp
        {
            get { return temp; }
            set { temp = value; }
        }

        protected override void PostData(JObject jobj, Sensor ss)
        {
            WaterLevel = jobj["body"]["children"][1]["data"][0]["value"].ToString(); 
            Flow = jobj["body"]["children"][2]["data"][0]["value"].ToString();
            Temp = jobj["body"]["children"][3]["data"][0]["value"].ToString();
            PostS.PostToSW(ss.SiteWhereId, 1, WaterLevel);
            PostS.PostToSW(ss.SiteWhereId, 2, Flow);
            PostS.PostToSW(ss.SiteWhereId, 3, Temp);
        }
        public override void MoniPostData(Sensor ss)
        {
            PostS.PostToSW(ss.SiteWhereId, 1, ss.XmlValues[1]);
            PostS.PostToSW(ss.SiteWhereId, 2, ss.XmlValues[2]);
            PostS.PostToSW(ss.SiteWhereId, 3, ss.XmlValues[3]);
        }
    }
}

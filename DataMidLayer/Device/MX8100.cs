using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using static DataMidLayer.DataSubscribe;

namespace DataMidLayer.Device
{
    class MX8100 : MX
    {
        private string waterLevel;

        public string WaterLevel
        {
            get { return waterLevel; }
            set { waterLevel = value; }
        }

        protected override void PostData(JObject jobj, Sensor ss)
        {
            WaterLevel = jobj["body"]["children"][1]["data"][0]["value"].ToString();
            PostS.PostToSW(ss.SiteWhereId, 1, WaterLevel);
     
        }
        public override void MoniPostData(Sensor ss)
        {
            PostS.PostToSW(ss.SiteWhereId, 1, ss.XmlValues[1]);
        }
    }
}

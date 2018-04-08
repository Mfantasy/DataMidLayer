using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataMidLayer.DeviceModel
{
    class MX7000 : MX
    {
        private string waterLevel;

        public string WaterLevel
        {
            get { return waterLevel; }
            set { waterLevel = value; }
        }

        public override void PostDataByXml(Sensor ss)
        {
            PostS.PostToSW(ss.SiteWhereId, 1, ss.XmlValues[1]);
        }

        public override void SaveData(string tbHeader)
        {
            throw new NotImplementedException();
        }

        protected override void PostData(JObject jobj, Sensor ss)
        {
            WaterLevel = jobj["body"]["children"][1]["data"][0]["value"].ToString();
            PostS.PostToSW(ss.SiteWhereId, 1, WaterLevel);
        }
    }
}
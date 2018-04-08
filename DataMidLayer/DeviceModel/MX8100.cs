using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using static DataMidLayer.DataSubscribe;

namespace DataMidLayer.DeviceModel
{
    class MX8100 : MX
    {
        public override int Interval
        {
            get
            {
                return 5 * 60 + AddRandom(50);
            }
        }

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
        public override void PostDataByXml(Sensor ss)
        {
            PostS.PostToSW(ss.SiteWhereId, 1, ss.XmlValues[1]);
        }

        public override void SaveData(string tbHeader)
        {
            throw new NotImplementedException();
        }
    }
}

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
    class MXS1402:MX
    {
    
        private string soilTemperature;

        public string SoilTemperature
        {
            get { return soilTemperature; }
            set { soilTemperature = value; }
        }
        private string soilHumidity;

        public string SoilHumidity
        {
            get { return soilHumidity; }
            set { soilHumidity = value; }
        }

        protected override void PostData(JObject jobj, Sensor ss)
        {
            SoilHumidity = jobj["body"]["children"][2]["data"][0]["value"].ToString();
            SoilTemperature = jobj["body"]["children"][3]["data"][0]["value"].ToString();
            PostS.PostToSW(ss.SiteWhereId, 1, SoilTemperature);
            PostS.PostToSW(ss.SiteWhereId, 2, SoilHumidity);
        }
        public override void MoniPostData(Sensor ss)
        {
            if (ss.XmlValues.Count == 2)
            {
                PostS.PostToSW(ss.SiteWhereId, 2, ss.XmlValues[1]);
                PostS.PostToSW(ss.SiteWhereId, 1, ss.XmlValues[0]);
            }
            else
            {
                PostS.PostToSW(ss.SiteWhereId, 2, ss.XmlValues[2]);
                PostS.PostToSW(ss.SiteWhereId, 1, ss.XmlValues[3]);
            }
        }
    }
}

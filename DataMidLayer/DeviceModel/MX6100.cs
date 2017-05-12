using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json.Linq;
using DataMidLayer;
using static DataMidLayer.DataSubscribe;

namespace DataMidLayer.Device
{
    /// <summary>
    /// 水采
    /// </summary>
    class MX6100:MX
    {
        public override int Interval
        {
            get
            {
                return 15 * 60 + AddRandom(100);
            }
        }

        private string status;

        public string Status
        {
            get { return status; }
            set { status = value; }
        }

        public override void PostDataByXml(Sensor ss)
        {
            PostS.PostToSW(ss.SiteWhereId, 1, ss.XmlValues[1]);
        }

        protected override void PostData(JObject jobj, Sensor ss)
        {
           Status = jobj["body"]["children"][1]["data"][0]["value"].ToString();
           PostS.PostToSW(ss.SiteWhereId, 1, Status);
        }
    }
}

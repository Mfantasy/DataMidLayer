using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataMidLayer.DeviceModel
{
    class MX6600 : MX
    {
        public string Flow { get; set; }
        public string ZFlow { get; set; }

        public override void PostDataByXml(Sensor ss)
        {
            PostS.PostToSW(ss.SiteWhereId, 1, ss.XmlValues[0]);
        }

        protected override void PostData(JObject jobj, Sensor ss)
        {
            Flow = jobj["body"]["children"][1]["data"][0]["value"].ToString();
            ZFlow = jobj["body"]["children"][2]["data"][0]["value"].ToString();
            PostS.PostToSW(ss.SiteWhereId, 1, Flow);
            PostS.PostToSW(ss.SiteWhereId, 2, ZFlow);
        }
    }
}
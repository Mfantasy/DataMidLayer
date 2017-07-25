using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataMidLayer.DeviceModel
{
    class MX9110:MX
    {
        public string LiuLiang { get; set; }
        public string YeWei { get; set; }
        public string LiuSu { get; set; }
        public string WenDu { get; set; }

        public override void PostDataByXml(Sensor ss)
        {
            PostS.PostToSW(ss.SiteWhereId, 1, ss.XmlValues[0]);
            PostS.PostToSW(ss.SiteWhereId, 2, ss.XmlValues[1]);
            PostS.PostToSW(ss.SiteWhereId, 3, ss.XmlValues[3]);
            PostS.PostToSW(ss.SiteWhereId, 4, ss.XmlValues[4]);
        }

        protected override void PostData(JObject jobj, Sensor ss)
        {
            LiuLiang = jobj["body"]["children"][0]["data"][0]["value"].ToString();
            YeWei = jobj["body"]["children"][1]["data"][0]["value"].ToString();
            LiuSu = jobj["body"]["children"][3]["data"][0]["value"].ToString();
            WenDu = jobj["body"]["children"][4]["data"][0]["value"].ToString();
            PostS.PostToSW(ss.SiteWhereId, 1, LiuLiang);
            PostS.PostToSW(ss.SiteWhereId, 2, YeWei);
            PostS.PostToSW(ss.SiteWhereId, 3, LiuSu);
            PostS.PostToSW(ss.SiteWhereId, 4, WenDu);
        }
    }
}

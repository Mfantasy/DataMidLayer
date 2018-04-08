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
        //流速
        public string Flow { get; set; }
        //流量
        public string ZFlow { get; set; }

        public override void PostDataByXml(Sensor ss)
        {
            PostS.PostToSW(ss.SiteWhereId, 1, ss.XmlValues[1]);
            PostS.PostToSW(ss.SiteWhereId, 2, ss.XmlValues[2]);
        }

        public override void SaveData(string tbHeader)
        {
            throw new NotImplementedException();
        }

        protected override void PostData(JObject jobj, Sensor ss)
        {
            Flow = jobj["body"]["children"][1]["data"][0]["value"].ToString();
            ZFlow = jobj["body"]["children"][2]["data"][0]["value"].ToString();
            //Post索引视enno字段顺序而定
            PostS.PostToSW(ss.SiteWhereId, 1, Flow);
            PostS.PostToSW(ss.SiteWhereId, 2, ZFlow);
        }
    }
}
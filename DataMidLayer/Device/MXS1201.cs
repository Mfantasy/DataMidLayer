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
    class MXS1201:MX
    {
        private string humid;

        public string Humid
        {
            get { return humid; }
            set { humid = value; }
        }
        private string humtemp;

        public string Humtemp
        {
            get { return humtemp; }
            set { humtemp = value; }
        }

        protected override void PostData(JObject jobj, Sensor ss)
        {
           Humid = jobj["body"]["children"][2]["data"][0]["value"].ToString();
            Humtemp = jobj["body"]["children"][3]["data"][0]["value"].ToString();
            PostS.PostToSW(ss.SiteWhereId, 1, Humid);
            PostS.PostToSW(ss.SiteWhereId, 2, Humtemp);
        }
    }
}

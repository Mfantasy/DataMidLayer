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
    class MX6100:MX
    {
        private string status;

        public string Status
        {
            get { return status; }
            set { status = value; }
        }

        protected override void PostData(JObject jobj, Sensor ss)
        {
           Status = jobj["body"]["children"][1]["data"][0]["value"].ToString();
           PostS.PostToSW(ss.SiteWhereId, 1, Status);
        }
    }
}

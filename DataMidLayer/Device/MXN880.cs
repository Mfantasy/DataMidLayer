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
    class MXN880 : MX
    {
       
        private string battVol;

        public String BattVol
        {
            get { return battVol; }
            set { battVol = value; }
        }


        private string chargeVol;

        public String ChargeVol
        {
            get { return chargeVol; }
            set { chargeVol = value; }
        }

        protected override void PostData(JObject jobj, Sensor ss)
        {
            ChargeVol = jobj["body"]["children"][2]["data"][0]["value"].ToString();
            BattVol = jobj["body"]["children"][3]["data"][0]["value"].ToString();
            PostS.PostToSW(ss.SiteWhereId, 1, ChargeVol);
            PostS.PostToSW(ss.SiteWhereId, 2, BattVol);
        }
    }
}

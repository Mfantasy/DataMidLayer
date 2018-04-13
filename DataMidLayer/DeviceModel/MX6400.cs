﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataMidLayer.DeviceModel
{
    class MX6400:MX
    {
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

        public override void SaveData(string tbHeader)
        {
            string tb = tbHeader + "TSS值";
            SQL sql = new SQL();
            sql.Insert(tb, status);
        }

        protected override void PostData(JObject jobj, Sensor ss)
        {
            Status = jobj["body"]["children"][1]["data"][0]["value"].ToString();
            PostS.PostToSW(ss.SiteWhereId, 1, Status);
        }
    }
}
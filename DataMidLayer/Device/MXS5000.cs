﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json.Linq;
using static DataMidLayer.DataSubscribe;

namespace DataMidLayer.Device
{
    class MXS5000 : MX
    {   
        private string rain;

        public string Rain
        {
            get { return rain; }
            set { rain = value; }
        }
        private string windSpeed;

        public string WindSpeed
        {
            get { return windSpeed; }
            set { windSpeed = value; }
        }
        private string windDirection;

        public string WindDirection
        {
            get { return windDirection; }
            set { windDirection = value; }
        }
        private string temperature;

        public string Temperature
        {
            get { return temperature; }
            set { temperature = value; }
        }

        private string humidity;

        public string Humidity
        {
            get { return humidity; }
            set { humidity = value; }
        }
        


        protected override void PostData(JObject jobj, Sensor ss)
        {
            Rain = jobj["body"]["children"][2]["data"][0]["value"].ToString();
            WindDirection = jobj["body"]["children"][4]["data"][0]["value"].ToString();
            WindSpeed = jobj["body"]["children"][3]["data"][0]["value"].ToString();
            Temperature = jobj["body"]["children"][7]["data"][0]["value"].ToString();
            Humidity = jobj["body"]["children"][8]["data"][0]["value"].ToString();
            PostS.PostToSW(ss.SiteWhereId, 1, Rain);
            PostS.PostToSW(ss.SiteWhereId, 2, Temperature);
            PostS.PostToSW(ss.SiteWhereId, 3, Humidity);
            PostS.PostToSW(ss.SiteWhereId, 4, WindSpeed);
            PostS.PostToSW(ss.SiteWhereId, 5, WindDirection);
        }

        public override void MoniPostData(Sensor ss)
        {
            if (ss.XmlValues.Count == 5)
            {
                PostS.PostToSW(ss.SiteWhereId, 1, ss.XmlValues[0]);
                PostS.PostToSW(ss.SiteWhereId, 4, ss.XmlValues[1]);
                PostS.PostToSW(ss.SiteWhereId, 5, ss.XmlValues[2]);
                PostS.PostToSW(ss.SiteWhereId, 2, ss.XmlValues[3]);
                PostS.PostToSW(ss.SiteWhereId, 3, ss.XmlValues[4]);
            }
            else
            {
                PostS.PostToSW(ss.SiteWhereId, 1, ss.XmlValues[1]);
                PostS.PostToSW(ss.SiteWhereId, 4, ss.XmlValues[2]);
                PostS.PostToSW(ss.SiteWhereId, 5, ss.XmlValues[3]);
                PostS.PostToSW(ss.SiteWhereId, 2, ss.XmlValues[4]);
                PostS.PostToSW(ss.SiteWhereId, 3, ss.XmlValues[5]);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataMidLayer
{
    public class Sensor
    {
        public class Cfg
        {
            public bool Remind { get; set; } //停止时间
            public int OverTimeM { get; set; } //超时时间(分)
            public int MoniIntervalM { get; set; } //模拟发送频率(分)
            public bool Moni { get; set; } //是否模拟数据
            public int RemindIntervalH { get; set; }//提醒频率(时)
        }
      
       
        public string SiteWhereId { get; set; }
        public string Name { get; set; }
        public string Gateway { get; set; }
        public string Node { get; set; }
        public string Type { get; set; }
        private Cfg config = new Cfg();
        public Cfg Config
        {
            get { return config; }
            set { config = value; }
        }


        public bool Priority { get; set; } //是否为关键设备

        public string Feed
        {
            get { return "{ method: \"subscribe\", headers: undefined, resource: \"/fengxi/" + Gateway + "/" + Node + "/" + Type + "\", token: 0 }"; }
        }
        public List<string> Current
        {
            get
            {                                                
                List<string> current = new List<string>();
                if (data != null)
                {
                    foreach (var item in data.XmlData.ChildRen)
                    {
                        current.Add(item.Name + ":" + item.Current.Value);
                    }
                }
                return current;
            }
        }

        public XmlRoot data;

        public XmlRoot Data
        {
            get
            {
                data = DataAccess.SerializeXml<XmlRoot>(DataAccess.HttpGet(XmlApi));
                return data;
            }
        }

        public string XmlApi { get { return "http://api." + Addr.Replace("http://","") + ".xml"; } }

        public string Addr
        {
            get
            {
                return "http://misty.smeshlink.com/fengxi/"+Gateway+"/"+Node+"/"+Type;
            }
        }

        public override string ToString()
        {
            return this.Name;
        }
    }

    [XmlRoot("xfml")]
    public class XmlRoot
    {
        [XmlElement("feed")]
        public XmlData XmlData { get; set; }
    }
    public class XmlData
    {
        [XmlElement("title")]
        public string Name { get; set; }
        [XmlElement("status")]
        public string Status { get; set; }
        [XmlElement("updated")]
        public string TimeStr { get; set; }
        [XmlArray("children"), XmlArrayItem("feed")]
        public List<Feed> ChildRen { get; set; }
    }
    public class Feed
    {
        [XmlElement("title")]
        public string Name { get; set; }
        [XmlElement("valueType")]
        public string ValueType { get; set; }
        [XmlElement("current")]
        public Current Current { get; set; }
    }
    public class Current
    {
        [XmlElement("number")]
        public string Number { get; set; }
        [XmlElement("integer")]
        public string Integer { get; set; }
        [XmlIgnore]
        public string Value 
        {
            get
            {
                if (Number != null)
                {
                    return Number;
                }
                else if(Integer != null)
                {
                    return Integer;
                }
                return null;
            }
        }
    }


}

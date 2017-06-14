using DataMidLayer.Device;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataMidLayer
{
    public class Sensor
    {
        //EXProperties
        public bool IsWorking { get; set; }
        public bool IsEx { get; set; }
        public bool IsXmlPosting { get; set; }
        public bool Remind { get; set; } //停止时间
        public int OverTimeM { get; set; } //超时时间(分)        
        public bool Moni { get; set; } //是否模拟数据

        public event EventHandler CatchEx;
        public void ExCatched()
        {
            CatchEx?.Invoke(this, null);
        }

        //异常及错误日志处理
        public class ErrorStr
        {
            public string Msg { get; set; }
            public string StactTrace { get; set; }
        }        
        public ErrorStr error = new ErrorStr();
        public ErrorStr Error { get { return error; } set { error = value; } }
        List<string> log = new List<string>();
        public List<string> Log { get { return log; } set { log = value; } }

        public MX SensorModel
        {
            get {             
                MX mx = null;
                switch (Type.ToUpper())
                {
                    case "MXS5000":
                        mx = new MXS5000();
                        break;
                    case "MXS1501":
                        mx = new MXS1501();
                        break;
                    case "MXS1402":
                        mx = new MXS1402();
                        break;
                    case "MXS1204":
                        mx = new MXS1204();
                        break;           
                    case "MX9000":
                        mx = new MX9000();
                        break;
                    case "MX7200":
                        mx = new MX7200();
                        break;
                    case "MX6100":
                        mx = new MX6100();
                        break;
                    case "MXS1201":
                        mx = new MXS1201();
                        break;
                    case "MX8000":
                        mx = new MX8000();
                        break;
                    case "MX8100":
                        mx = new MX8100();
                        break;
                }
                return mx;
            }
        }
        
        public string SiteWhereId { get; set; }
        public string Name { get; set; }
        public string Gateway { get; set; }
        public string Node { get; set; }
        public string Type { get; set; }   
      
        public List<string> XmlValues
        {
            get
            {
                List<string> current = new List<string>();
                foreach (var item in Data.XmlData.ChildRen)
                {
                    current.Add(item.Current.Value);
                }                
                return current;
            }
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
                else
                {
                    foreach (var item in Data.XmlData.ChildRen)
                    {
                        current.Add(item.Name + ":" + item.Current.Value);
                    }
                }
                return current;
            }
        }

        XmlRoot data;

        public XmlRoot Data
        {
            get
            {
                if (data == null)
                { data = DataAccess.SerializeXml<XmlRoot>(DataAccess.HttpGet(XmlApi)); }
                return data;
            }
        }

        string resourceAddr = ConfigurationManager.AppSettings["resource"];
        public string Feed
        {
            get
            {
                return "{"+string.Format(" method: \"subscribe\", headers: undefined, resource: \"/{0}/{1}/{2}/{3}\", token: 0 ", resourceAddr, Gateway, Node, Type)+"}";                
            }
        }

        public string XmlApi { get { return  Addr.Replace("http://misty.", "http://api.") + ".xml"; } }

        public string Addr
        {
            get
            {
                return string.Format("http://misty.smeshlink.com/{0}/{1}/{2}/{3}", resourceAddr,Gateway,Node,Type);                
            }
        }

        public override string ToString()
        {
            return this.Name;
        }

        public void RefreshXmlData() { data = DataAccess.SerializeXml<XmlRoot>(DataAccess.HttpGet(XmlApi)); }
        public string XmlTitle { get { return Data.XmlData.Name; } }
        public string XmlStatus { get { return Data.XmlData.Status; } }
        public string XmlTime { get { return Data.XmlData.TimeStr; } }

        public bool StatusByXml { get; set; }
        


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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataMidLayer
{
    static class DataAccess
    {




        

        //HttpGetStream
        public static Stream HttpGet(string url)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse receiveStream = (HttpWebResponse)req.GetResponse();
            return receiveStream.GetResponseStream();
        }    
        //序列化
        public static T SerializeXml<T>(Stream stream)
        {
            XmlSerializerFactory factory = new XmlSerializerFactory();
            XmlSerializer serializer = factory.CreateSerializer(typeof(T));            
            object cacheData = serializer.Deserialize(stream);
            return cacheData == null ? default(T) : (T)cacheData;
        }
    }
}

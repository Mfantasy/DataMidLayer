using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataMidLayer
{
    class Sensor
    {
        public string Feed
        {
            get { return "{ method: \"subscribe\", headers: undefined, resource: \"/fengxi/" + Gateway + "/" + Node + "/" + Type + "\", token: 0 }"; }
        }
        /// <summary>
        /// Misty地址
        /// </summary>
        public string Addr { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Gateway { get; set; }
        public string Node { get; set; }
        public string Type { get; set; }

        private string current;

        public string Current
        {
            get { return current; }
            set { current = value; }
        }


        public override string ToString()
        {
            return this.Name;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataMidLayer
{
    public static class Utils
    {
        static object obj = new object();
        public static void WriteError(string msg,string fileName="error.txt")
        {
            lock (obj)
            {
                File.AppendAllText(fileName, msg);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace DataMidLayer
{
    class SQL
    {
        string conStr = ConfigurationManager.ConnectionStrings["sqlServer"].ConnectionString;        
        public void Test()
        {
           
            string sql = "INSERT INTO Person (Name) VALUES (@name)";
            SqlConnection con = new SqlConnection(conStr);
            SqlCommand cmd = con.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("name", "阿X");
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();
           
        }

        public void Insert(string tb,string val)
        {
            string sql = $"INSERT INTO [{tb}] (f_data_value) VALUES (@val)";
            SqlConnection con = new SqlConnection(conStr);
            SqlCommand cmd = con.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("val", val);
            try
            {
                con.Open();
                cmd.ExecuteNonQuery();
            }
            finally
            {
                if (con != null)
                {
                    con.Close();
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokayokeTracking
{
    public class CheckSPs
    {
        public static void Revisa()
        {
            string sqlConnectionString = @"Server=.;Database=ProductionDatas;Trusted_Connection=True;";

            string script = File.ReadAllText("Script.sql");

            SqlConnection conn = new SqlConnection(sqlConnectionString);

            SqlCommand sql = new SqlCommand();

            sql.CommandText = script;
            conn.Open();
            sql.ExecuteNonQuery();
        }
    }
}

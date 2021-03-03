using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvicsTest
{
    public static class SQLMethods
    {
        public static string ConStr = "Server=.;Database=ProductionDatas;Trusted_Connection=True;";
        public static DataTable spSearchProductioByActualDayAndOperation(string Operation)
        {
            DataTable dtData = new DataTable();
            using (SqlConnection conn = new SqlConnection(ConStr))
            {
                try
                {
                    SqlCommand command = new SqlCommand("spSearchProductioByActualDayAndOperation", conn);
                    SqlDataAdapter sqlAdapter = new SqlDataAdapter(command);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Operation", Operation);
                    sqlAdapter.Fill(dtData);
                }
                catch (Exception ex)
                {
                    dtData = null;
                }
            }
            return dtData;
        }
       
    }
}

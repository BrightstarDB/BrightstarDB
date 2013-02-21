using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlTypes;

namespace BrightstarDB.SdShare.Test
{
    public class DataManagementHelper
    {
        private static void ExecuteSqlUpdate(string dsnConnection, string query)
        {
            using (var connection = new OdbcConnection(dsnConnection))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = query;
                cmd.ExecuteNonQuery();
            }
        }

        public static void ClearUpdateLogs()
        {
            ExecuteSqlUpdate("DSN=CompanyData32", "delete from CustomerChangeLog");
        }

        public void UpdateCustomer(string id, string name)
        {

        }

        public void AddCustomer(string id, string name)
        {
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Sql;
using System.Data.SqlClient;

namespace BrightstarDB.SdShare
{
    public class EPiServerTMCoreHierarchyDataSourceManager : BaseDataSourceManager
    {
        private string _dbConnectionString;
        public EPiServerTMCoreHierarchyDataSourceManager(string fileName, string epiServerConnectionString) : base(fileName) 
        {
            _dbConnectionString = epiServerConnectionString;
        }

        private const string BulkEPi_PageHierarchyQuery = @"
select tblTree.fkParentID as parentId, tblTree.fkChildID as childId, parentPage.PageGUID as parentGuid, childPage.PageGUID as childGuid
from tblTree, tblPage as parentPage, tblPage as childPage 
where tblTree.fkParentID = parentPage.pkID and tblTree.fkChildID = childPage.pkID
and
childPage.PendingPublish = 0 AND parentPage.PendingPublish = 0 and nestinglevel = 1 order by parentId
";

        public override IEnumerable<EntityInfo> GetEntityInfos()
        {
            Logging.LogInfo("Getting episerver page hierarchy");
            using (var connection = new SqlConnection(_dbConnectionString))
            {
                SqlCommand command = new SqlCommand(string.Format(BulkEPi_PageHierarchyQuery), connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var parentPageGuid = reader["parentGuid"].ToString();
                        var childPageGuid = reader["childGuid"].ToString();
                        yield return new EntityInfo() { HashValue = (parentPageGuid.ToString() + childPageGuid.ToString()).GetHashCode(), EntityId = parentPageGuid.ToString(), EntityKey = parentPageGuid.ToString() + childPageGuid.ToString() };
                    }
                }
                reader.Close();
                connection.Close();
            }
        }
    }
}

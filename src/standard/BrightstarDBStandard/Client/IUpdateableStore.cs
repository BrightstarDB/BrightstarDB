using System.Collections.Generic;
using System.IO;
using BrightstarDB.EntityFramework.Query;
using BrightstarDB.Model;

namespace BrightstarDB.Client
{
    internal interface IUpdateableStore
    {
        SparqlResult ExecuteQuery(SparqlQueryContext queryContext, IList<string> datasetGraphUris);

        void ApplyTransaction(IEnumerable<ITriple> existencePreconditions, IEnumerable<ITriple> nonexistencePreconditions,
                              IEnumerable<ITriple> deletePatterns, IEnumerable<ITriple> inserts,
                              string updateGraphUri);

        void Cleanup();
    }
}

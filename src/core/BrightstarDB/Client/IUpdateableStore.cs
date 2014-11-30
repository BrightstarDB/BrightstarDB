using System.Collections.Generic;
using System.IO;
using BrightstarDB.Model;

namespace BrightstarDB.Client
{
    internal interface IUpdateableStore
    {
        Stream ExecuteQuery(string queryExpression, IList<string> datasetGraphUris);

        void ApplyTransaction(IEnumerable<ITriple> existencePreconditions, IEnumerable<ITriple> nonexistencePreconditions,
                              IEnumerable<ITriple> deletePatterns, IEnumerable<ITriple> inserts,
                              string updateGraphUri);

        void Cleanup();
    }
}

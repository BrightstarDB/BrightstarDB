using System.Collections.Generic;
using System.IO;
using BrightstarDB.Model;

namespace BrightstarDB.Client
{
    internal interface IUpdateableStore
    {
        Stream ExecuteQuery(string queryExpression, IList<string> datasetGraphUris);

        void ApplyTransaction(IList<ITriple> existencePreconditions, IList<ITriple> nonexistencePreconditions,
                              IList<ITriple> deletePatterns, IList<ITriple> inserts,
                              string updateGraphUri);

        void Cleanup();
    }
}

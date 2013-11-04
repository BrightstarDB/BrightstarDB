using System.Collections.Generic;
using System.IO;
using BrightstarDB.Model;

namespace BrightstarDB.Client
{
    internal interface IUpdateableStore
    {
        Stream ExecuteQuery(string queryExpression, IList<string> datasetGraphUris);

        void ApplyTransaction(IList<Triple> preconditions, IList<Triple> deletePatterns, IList<Triple> inserts,
                              string updateGraphUri);

        void Cleanup();
    }
}

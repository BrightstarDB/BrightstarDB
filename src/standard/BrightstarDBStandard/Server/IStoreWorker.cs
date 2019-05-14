using System;
using System.IO;

namespace BrightstarDB.Server
{
    internal interface IStoreWorker
    {
        void Query(string queryExpression, Stream resultsStream);
        Guid ProcessTransaction(string deletePatterns, string insertData, string format);
        Guid Insert(String data, string format);
        void ExportData(Stream stream);
    }
}

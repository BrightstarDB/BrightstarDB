using System;
using BrightstarDB.Storage;

namespace BrightstarDB.Portable.Tests
{
    public class TestsBase
    {
        private readonly string _runId;
        public TestsBase()
        {
            _runId = DateTime.Now.ToString("yyyyMMdd_HHmmss") + GetType().Name + ".";
        }
        public string MakeStoreName(string testName, PersistenceType persistenceType)
        {
            return String.Format("{0}.{1}_{2}", _runId, testName, persistenceType);
        }
    }
}
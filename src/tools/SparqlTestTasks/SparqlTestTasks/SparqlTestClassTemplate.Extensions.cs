using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparqlTestTasks
{
    partial class SparqlTestClassTemplate
    {
        private TestManifest _testManifest;
        internal SparqlTestClassTemplate(TestManifest testManifest)
        {
            _testManifest = testManifest;
        }
    }
}

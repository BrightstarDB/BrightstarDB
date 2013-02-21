using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests
{
    [TestClass]
    public class AssemblySetUpAndTearDown
    {
        [AssemblyInitialize]
        public static void AssemblySetUp(TestContext context)
        {
        }
    }
}

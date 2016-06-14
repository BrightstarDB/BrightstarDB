using System.IO;
using NUnit.Framework;

namespace BrightstarDB.InternalTests
{
    internal static class TestPaths
    {
        public static string DataPath => Path.Combine(TestContext.CurrentContext.TestDirectory, "..\\..\\..\\BrightstarDB.Tests\\Data\\");
    }
}

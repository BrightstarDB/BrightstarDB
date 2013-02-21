using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Mobile.Tests
{
    [TestClass]
    public class AssemblySetUpAndTearDown
    {
        [AssemblyInitialize]
        public static void AssemblySetUp(TestContext context)
        {
            Licensing.License.Validate("support@brightstardb.com", "MOB-PW08C-ZBX4Y-00200-0000G");
        }
    }
}

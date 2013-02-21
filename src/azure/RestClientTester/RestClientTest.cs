using System;
using System.Linq;
using System.Reflection;
using BrightstarDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RestClientTester
{
    public class RestClientTest
    {
        protected ConnectionString ConnectionString { get; set; }
        public RestClientTest(ConnectionString connectionString)
        {
            ConnectionString = connectionString;
        }

        public virtual void Run()
        {
            Console.WriteLine("Testing: " + GetType().FullName);
            foreach (
                var testMethod in
                    GetType().GetMethods().Where(
                        m => m.IsPublic&& m.Name.StartsWith("Test") && m.GetParameters().Length == 0))
            {
                Console.Write("\t" + testMethod.Name + ": ");
                try
                {
                    testMethod.Invoke(this, new object[] {});
                    Console.WriteLine("PASSED.");
                }
                catch (AssertFailedException assertionFailed)
                {
                    Console.WriteLine("FAILED: " + assertionFailed.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: {0}", ex);
                }
            }
        }
    }
}
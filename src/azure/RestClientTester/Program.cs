using System;
using System.Linq;
using System.Reflection;
using BrightstarDB;

namespace RestClientTester
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 1)
                {
                    var connectionString = new ConnectionString(args[0]);
                    // Run all the tests
                    foreach (
                        var testClass in
                            Assembly.GetExecutingAssembly().GetTypes().Where(
                                t => typeof (RestClientTest).IsAssignableFrom(t) && !(t == typeof (RestClientTest))))
                    {
                        var testClassInstance = Activator.CreateInstance(testClass, connectionString) as RestClientTest;
                        Console.WriteLine("Test class: " + testClass.FullName);
                        testClassInstance.Run();
                    }
                }
                else if (args.Length > 1)
                {
                    var connectionString = new ConnectionString(args[0]);
                    foreach (var className in args.Skip(1))
                    {
                        RunTestClass(className, connectionString);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("RestClientTester terminated with exception.");
                Console.Error.WriteLine(ex);
            }
        }

        private static void RunTestClass(string testClassName, ConnectionString connectionString)
        {
            var testClass = Type.GetType("RestClientTester." + testClassName);
            if (testClass == null)
            {
                Console.Error.WriteLine("Could not find test class: " + testClassName);
                return;
            }
            var testClassInstance = Activator.CreateInstance(testClass, connectionString) as RestClientTest;
            testClassInstance.Run();
        }

    }
}

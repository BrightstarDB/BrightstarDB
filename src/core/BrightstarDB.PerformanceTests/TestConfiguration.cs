using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB.PerformanceTests
{
    internal static class TestConfiguration
    {
#if PORTABLE

        public static string StoreLocation
        {
            get { return "BrightstarDB"; }
        }

        public static string DataLocation
        {
            get { return System.IO.Path.GetFullPath("..\\..\\..\\..\\..\\..\\src\\core\\BrightstarDB.PerformanceTests\\Data\\"); }
        }
#else
        public static string StoreLocation
        {
            get
            {
                return
                    System.Configuration.ConfigurationManager.AppSettings["BrightstarDB.StoreLocation"];
            }
        }

        public static string DataLocation
        {
            get { return "..\\..\\Data\\"; }
        }
#endif
    }
}

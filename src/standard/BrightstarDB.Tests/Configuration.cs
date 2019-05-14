using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB.Tests
{
    internal static class Configuration
    {
#if PORTABLE

        public static string StoreLocation
        {
            get { return "BrightstarDB"; }
        }

        public static string DataLocation
        {
            get { return System.IO.Path.GetFullPath("..\\..\\..\\..\\..\\..\\src\\core\\BrightstarDB.Tests\\Data\\"); }
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

        public  static string DataLocation
        {
            get { return "..\\..\\Data\\"; }
        }
#endif
    }
}

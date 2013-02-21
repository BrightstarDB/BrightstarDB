using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB.Tests
{
    internal static class Configuration
    {
        public static string StoreLocation
        {
            get
            {
                return
                    System.Configuration.ConfigurationManager.AppSettings["BrightstarDB.StoreLocation"];
            }
        }
    }
}

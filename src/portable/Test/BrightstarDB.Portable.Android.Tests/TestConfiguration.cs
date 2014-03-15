using System;

namespace BrightstarDB.Portable.Android.Tests
{
    public static class TestConfiguration
    {
        public static string StoreLocation
        {
            get
            {
                return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                              "BrightstarDB");
            }
        }
    }
}


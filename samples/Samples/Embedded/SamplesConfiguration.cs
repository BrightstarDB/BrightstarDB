using System;
using System.IO;

namespace BrightstarDB.Samples
{
    /// <summary>
    /// This class provides a common place for all of the sample applications to initialize the
    /// BrightstarDB license. Once you have updated this file for one sample to build and run correctly, 
    /// all the other samples should also then build and run without any further changes required.
    /// </summary>
    static class SamplesConfiguration
    {
        /// <summary>
        /// OPTIONAL: If you would like the samples to store their data in a different folder, replace
        /// the value of this property with the path to use (the directory will be created if it does not exist)
        /// </summary>
        public static string StoresDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BrightstarSamplesData");

        public static void Register()
        {
            // Ensure that the directory we want to use for storing samples data exists.
            // If it does not, create it.
            var dir = new DirectoryInfo(StoresDirectory);
            if (!dir.Exists)
            {
                dir.Create();
            }
        }
    }
}

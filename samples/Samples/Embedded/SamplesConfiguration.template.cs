using System;
using System.IO;
using BrightstarDB.Licensing;

namespace BrightstarDB.Samples
{
    /// <summary>
    /// This class provides a common place for all of the sample applications to initialize the
    /// BrightstarDB license. Once you have updated this file for one sample to build and run correctly, 
    /// all the other samples should also then build and run without any further changes required.
    /// </summary>
    static class SamplesConfiguration
    {

        // NOTE: These values are your private license information.
        // You should always take steps to ensure that this information is 
        // protected in any application you distribute - e.g. by using
        // a .NET obfuscation tool to encrypt the string constants.

        private const string LicensedUserId = ""; // TODO: Enter the licensed user ID (email address) from your license here
        private const string LicenseKey = ""; // TODO: Enter the license key from your license here


        /// <summary>
        /// OPTIONAL: If you would like the samples to store their data in a different folder, replace
        /// the value of this property with the path to use (the directory will be created if it does not exist)
        /// </summary>
        
        public static string StoresDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\BrightstarSamplesData";

        public static void Register()
        {
            // Registers the license with BrightstarDB
            License.Validate(LicensedUserId, LicenseKey);

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

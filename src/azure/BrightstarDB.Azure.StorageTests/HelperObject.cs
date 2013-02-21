using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BrightstarDB.Azure.StorageTests
{
    public class HelperObject
    {
        public static string AzureSDK
        {
            get
            {
                string[] locations = new string[]
                {
                    @"C:\Program Files\Windows Azure Emulator\",
                };

                return (from location in locations where Directory.Exists(location) select location).First();
            }
        }

        public static string AzureCsrun
        {
            get
            {
                string path = Path.Combine(AzureSDK, @"bin\csrun.exe");

                return path;
            }
        }
        public static string AzureDsInit
        {
            get
            {
                string path = Path.Combine(AzureSDK, @"bin\devstore\dsinit.exe");

                return path;
            }
        }

        public static void RunCsrun(string arguments)
        {
            var process = System.Diagnostics.Process.Start(AzureCsrun, arguments);
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new ApplicationException(string.Format("process exit code is nonzero ({0})", process.ExitCode));
            }
        }
        public static void InitDb()
        {
            var process = System.Diagnostics.Process.Start(AzureDsInit, "/forceCreate");
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new ApplicationException(string.Format("process exit code is nonzero ({0})", process.ExitCode));
            }

        }
        public static bool IsProcessOpen(string name)
        {
            //here we're going to get a list of all running processes on
            //the computer
            foreach (Process clsProcess in Process.GetProcesses())
            {
                if (clsProcess.ProcessName.Contains(name))
                {
                    //if the process is found to be running then we
                    //return a true
                    return true;
                }
            }
            //otherwise we return a false
            return false;
        }

        public static bool FabricLoaded()
        {
            return IsProcessOpen("DFService") & IsProcessOpen("DFAgent") &
                   IsProcessOpen("DFMonitor") & IsProcessOpen("DFloadbalancer");
        }


    }
}
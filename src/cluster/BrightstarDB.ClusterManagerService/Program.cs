using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using BrightstarDB.ClusterManager;

namespace BrightstarDB.ClusterManagerService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            if (Environment.UserInteractive)
            {
                StartService();
                while(true)
                {
                    Thread.Sleep(1000);
                }
            }
            else
            {
                ServiceBase.Run(new Service());
            }
        }

        private static void StartService()
        {
            WriteWelcomeHeader();
            // TODO: redirect logging to console
            try
            {
                var serviceHostFactory = new ClusterManagerServiceHostFactory();
                var serviceHost = serviceHostFactory.CreateServiceHost();
                serviceHost.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error initializing Brightstar service: {0}", ex.Message);
            }
        }

        private static void WriteWelcomeHeader()
        {
            var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            var assemVer = fvi.FileMajorPart + "." + fvi.FileMinorPart + "." + fvi.FileBuildPart;

            Console.WriteLine("Brightstar Cluster Manager {0}.", assemVer);
            Console.WriteLine(fvi.LegalCopyright);
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine();
        }
    }
}

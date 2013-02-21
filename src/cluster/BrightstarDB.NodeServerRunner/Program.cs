using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using BrightstarDB;
using BrightstarDB.ClusterNodeService;
using BrightstarDB.Service;

namespace BrightstarDB.NodeServerRunner
{
    static class Program
    {
        private static void PrintUsage()
        {
            Console.WriteLine("Usage: BrightstarNodeService.exe [no params] || [<base location> <http port> <tcp port> <pipe name> <clusterport>]");
            Console.WriteLine("e.g. BrightstarNodeService.exe c:\brightstar 8091 8086 bstar 10001");
            Console.WriteLine("Please provide 0 parameters or all 5 required parameters.");
        }

        static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                if (args.Length > 0 && args.Length != 5)
                {
                    PrintUsage();
                    return;
                }

                if (args.Length == 5)
                {
                    try
                    {
                        var baseLocation = args[0];
                        var httpPort = int.Parse(args[1]);
                        var tcpPort = int.Parse(args[2]);
                        var pipeName = args[3];
                        var clusterPort = int.Parse(args[4]);
                        Configuration.StoreLocation = baseLocation;
                        Configuration.HttPort = httpPort;
                        Configuration.TcpPort = tcpPort;
                        Configuration.NamedPipeName = pipeName;
                        Configuration.ClusterNodePort = clusterPort;
                    } catch (Exception ex)
                    {
                        Console.WriteLine("Error with parameters " + ex.Message);
                        PrintUsage();
                        return;
                    }
                }
                
                StartService();
            }
            else
            {
                ServiceBase.Run(new Service1());
            }
        }

        public static void StartService()
        {
            WriteWelcomeHeader();
#if DEBUG
            Logging.EnableConsoleOutput(true);
#else
            Logging.EnableConsoleOutput(false);
#endif
            Logging.LogInfo("Logging started");
            Logging.LogInfo("Starting Brightstar Node Server");
            try
            {
                var serviceHostFactory = new BrightstarServiceHostFactory();
                var service = new BrightstarNodeService();
                var serviceHost = serviceHostFactory.CreateServiceHost(service, StopNode);
                serviceHost.Open();
                Logging.LogInfo("Brightstar Node Server started");
                Console.ReadLine();
            }
            catch (AddressAccessDeniedException ex)
            {
                Logging.LogError(BrightstarEventId.AddressAccessDenied, "You do not have the privileges required to register the Brightstar service: " +
                                 ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServiceHostStartupFailed, "Error registering or starting Brightstar service: " + ex.Message);
                throw;
            }
        }

        public static void StopNode(object sender, EventArgs e)
        {
            var serviceHost = sender as ServiceHost;
            if (serviceHost != null)
            {
                var nodeService = serviceHost.SingletonInstance as BrightstarNodeService;
                if (nodeService != null)
                {
                    nodeService.Stop();
                }
            }
        }

        private static void WriteWelcomeHeader()
        {
            var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            var assemVer = fvi.FileMajorPart + "." + fvi.FileMinorPart + "." + fvi.FileBuildPart;
            
            Console.WriteLine("BrightstarDB Cluster Node Server {0}.", assemVer);
            Console.WriteLine(fvi.LegalCopyright);
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine();
        }
    }

}

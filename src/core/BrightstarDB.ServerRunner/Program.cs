using System;
using System.Reflection;
using System.ServiceModel;
using System.ServiceProcess;
using BrightstarDB.Service;

namespace BrightstarDB.ServerRunner
{
    public class Program
    {
        private static void PrintUsage()
        {
            Console.WriteLine("Usage: BrightstarService.exe [no params] || [<base location> <http port> <tcp port> <pipe name>]");
            Console.WriteLine("e.g. BrighstarService.exe c:\brightstar 8091 8086 bstar");
            Console.WriteLine("Please provide 0 parameters or all 4 required parameters.");
        }

        static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                if (args.Length > 0 && args.Length != 4)
                {
                    PrintUsage();
                    return;
                }

                if (args.Length == 4)
                {
                    try
                    {
                        var baseLocation = args[0];
                        var httpPort = int.Parse(args[1]);
                        var tcpPort = int.Parse(args[2]);
                        var pipeName = args[3];
                        Configuration.StoreLocation = baseLocation;
                        Configuration.HttPort = httpPort;
                        Configuration.TcpPort = tcpPort;
                        Configuration.NamedPipeName = pipeName;
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
                ServiceBase.Run(new Service());
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
            Logging.LogInfo("Starting Brightstar server");
            try
            {
                var serviceHostFactory = new BrightstarServiceHostFactory();
                var serviceHost = serviceHostFactory.CreateServiceHost();
                serviceHost.Open();
                Logging.LogInfo("Brightstar server started");
                Console.ReadLine();
            }
            catch (AddressAccessDeniedException ex)
            {
                Logging.LogError(BrightstarEventId.AddressAccessDenied,
                                 "You do not have the privileges required to register the Brightstar service: " +
                                 ex.Message);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServiceHostStartupFailed, "Error registering or starting Brightstar service: " + ex.Message);
            }
        }

        private static void WriteWelcomeHeader()
        {
            var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            var assemVer = fvi.FileMajorPart + "." + fvi.FileMinorPart + "." + fvi.FileBuildPart;
            
            Console.WriteLine("Brightstar Server {0}.", assemVer);
            Console.WriteLine(fvi.LegalCopyright);
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine();
        }
    }
}

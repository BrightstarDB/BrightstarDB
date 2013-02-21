using System;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceProcess;
using BrightstarDB.SdShare;
using BrightstarDB.SdShare.Client;
using BrightstarDB.SdShare.Service;

namespace BrightstarDB.SDShare.Server
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            try
            {
                if (Environment.UserInteractive)
                {
                    StartService();
                }
                else
                {
                    ServiceBase.Run(new Service());
                }
            }
            catch (Exception ex)
            {
                Logging.LogError(1, "Unable to start service due to {0} in {1}", ex.Message, ex.StackTrace);
            }
        }

        private static EntityChangeManager EntityChangeManager;
        private static SdShareClientFeedProcessor SdShareClientFeedProcessor;

        public static void StartService()
        {
            WriteWelcomeHeader();
#if DEBUG
            Logging.EnableConsoleOutput(true);
#else
            Logging.EnableConsoleOutput(false);
#endif
            Logging.LogInfo("Logging started");
            Logging.LogInfo("Starting SDShare server");
            try
            {
                // starts web service to service requests
                // var serviceHostFactory = new SdShareServiceHostFactory();
                // var serviceHost = serviceHostFactory.CreateServiceHost();
                var serviceHost = new ServiceHost(typeof(PublishingService));                
                serviceHost.Open();

                // start entity change manager to deal with data source with no last updated time
                if (ConfigurationReader.Configuration != null)
                {
                    EntityChangeManager.Instance.Start();
                } else
                {
                    Logging.LogError(1, "No valid configuration exists to start entity change manager.");
                }

                // starting client processors
                if (ConfigurationReader.Configuration != null)
                {
                    SdShareClientFeedProcessor = new SdShareClientFeedProcessor(ConfigurationReader.Configuration.FeedSources.ToList(), 
                                                                         ConfigurationReader.Configuration.ClientAdaptors.ToList());
                    SdShareClientFeedProcessor.Start();
                }
                else
                {
                    Logging.LogError(1, "No valid configuration exists to start client processor.");
                }

                Logging.LogInfo("SDShare server started");
                Console.ReadLine();
            }
            catch (AddressAccessDeniedException ex)
            {
                Logging.LogError(0, "You do not have the privileges required to register the SDShare service {0} {1} ", ex.Message, ex.StackTrace);
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Logging.LogError(0, "Error registering or starting SDShare service {0} {1}", ex.Message, ex.StackTrace);
                Console.ReadLine();
            }
        }

        private static void WriteWelcomeHeader()
        {
            var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            var assemVer = fvi.FileMajorPart + "." + fvi.FileMinorPart + "." + fvi.FileBuildPart;
            Console.WriteLine("SDShare Server {0}.", assemVer);
            Console.WriteLine("Copyright (c) 2012 BrightstarDB Ltd. All rights reserved.");
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine();
        }
    }
}

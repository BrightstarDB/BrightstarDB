using System;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
#if !SDK_TESTS
using BrightstarDB.Service;
#endif
using NUnit.Framework;

namespace BrightstarDB.Tests
{
    public class ClientTestBase// : IDisposable
    {
        private static ServiceHost _serviceHost;
        private static bool _closed;
        private static bool _started;
        private static readonly object HostLock = new object();

        protected static void StartService()
        {
            var serverTask = new Task(StartServer);
            serverTask.Start();
        }

        protected static void CloseService()
        {
        }

        private static void StartServer()
        {
            lock (HostLock)
            {
                try
                {
#if SDK_TESTS
                    // We assume that the test framework starts up the service for us.
#else
                    if (_serviceHost == null || _closed)
                    {
                        var serviceHostFactory = new BrightstarServiceHostFactory();
                        _serviceHost = serviceHostFactory.CreateServiceHost();
                        _serviceHost.Open();
                        _serviceHost.Closed += HandleServiceClosed;
                        _serviceHost.Faulted += HandleServiceFaulted;
                        while (!_closed)
                        {
                            Thread.Sleep(1000);
                        }
                    }
#endif
                }
                catch (AddressAlreadyInUseException)
                {
                    Console.WriteLine("Server address already in use. Assuming this is OK.");
                }
            }
        }

        private static void HandleServiceClosed(object sender, EventArgs e)
        {
            _closed = true;
        }

        private static void HandleServiceFaulted(object sender, EventArgs e)
        {
            _closed = true;
            Assert.Fail("Service faulted unexpectedly");
        }

        /*
        public void Dispose()
        {
            if (!_closed)
                try
                {
                    _serviceHost.Close();
                    while (!_closed)
                    {
                        Thread.Sleep(10);
                    }
                }
                catch (Exception)
                {
                }
        }
         */
    }
}

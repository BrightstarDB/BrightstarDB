using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using BrightstarDB.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests
{
    public class InternalClientTestBase
    {
        private static ServiceHost _serviceHost;
        private static bool _closed;

        protected static void StartService()
        {
            var serverTask = new Task(StartServer);
            serverTask.Start();
        }

        protected static void CloseService()
        {
            try
            {
                _serviceHost.Close();
                while (!_closed)
                {
                    Thread.Sleep(10);
                }
            } catch(Exception){}
        }

        private static void StartServer()
        {
            var serviceHostFactory = new BrightstarServiceHostFactory();
            _serviceHost = serviceHostFactory.CreateServiceHost();
            _serviceHost.Open();
            _serviceHost.Closed += HandleServiceClosed;
            _serviceHost.Faulted += HandleServiceFaulted;
            while(!_closed)
            {
                Thread.Sleep(1000);
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
    }
}

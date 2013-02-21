using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceProcess;
using BrightstarDB.Service;

namespace BrightstarDB.ServerRunner
{
    partial class Service : ServiceBase
    {
        private ServiceHost _serviceHost;
        private const string EventSource = "BrightstarDB";
        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                Logging.LogInfo("Logging started");
                Logging.LogInfo("Starting Brightstar service.");
                var serviceHostFactory = new BrightstarServiceHostFactory();
                _serviceHost = serviceHostFactory.CreateServiceHost();
                _serviceHost.Open();
                Logging.LogInfo("Brightstar service started.");
                TryLog("Brightstar service started.");
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServiceStartupFailed, "Service startup failed. Cause: {0}", ex);
                TryLog("Brightstar service failed to start. Cause: " + ex, EventLogEntryType.Error);
                Stop();
            }
        }

        protected override void OnStop()
        {
            Logging.LogInfo("Brightstar service stopping.");
           _serviceHost.Close();
            Logging.LogInfo("Brightstar service stopped.");
            TryLog("Brightstar service stopped.");
        }

        private void TryLog(string msg, EventLogEntryType entryType = EventLogEntryType.Information)
        {
            try
            {
                EventLog.WriteEntry(EventSource, msg, entryType);
            }
            catch (Exception)
            {
                // Logging failed
            }
        }
    }
}

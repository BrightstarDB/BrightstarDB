using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace BrightstarDB.Azure.Common
{
    /// <summary>
    /// Provides common helper code for use in the startup of web and worker roles
    /// </summary>
    public class RoleStartupHelper
    {
        /// <summary>
        /// Initializes Azure diagnostics with log transfer settings read from the role configuration settings
        /// </summary>
        public static void StartDiagnostics()
        {
            var diagnostics = DiagnosticMonitor.GetDefaultInitialConfiguration();
            diagnostics.OverallQuotaInMB = 8192;
            var logLevelFilter = GetLogLevelFilter();
            var szScheduledTransferPeriod = RoleEnvironment.GetConfigurationSettingValue(AzureConstants.ScheduledTransferPeriodPropertyName);
            var scheduledTransferPeriod = TimeSpan.FromMinutes(Convert.ToDouble(szScheduledTransferPeriod));
            var szSampleRate = RoleEnvironment.GetConfigurationSettingValue(AzureConstants.SampleRatePropertyName);
            var sampleRate = TimeSpan.FromSeconds(Convert.ToInt32(szSampleRate));

            // Infrastructure Logs
            diagnostics.DiagnosticInfrastructureLogs.ScheduledTransferLogLevelFilter = logLevelFilter;
            diagnostics.DiagnosticInfrastructureLogs.ScheduledTransferPeriod = scheduledTransferPeriod;

            // Azure Logs
            diagnostics.Logs.ScheduledTransferLogLevelFilter = logLevelFilter;
            diagnostics.Logs.ScheduledTransferPeriod = scheduledTransferPeriod;

            // Performance counters
            diagnostics.PerformanceCounters.ScheduledTransferPeriod = scheduledTransferPeriod;
            ConfigurePerformanceCounters(diagnostics, sampleRate);

            // Event logs
            //diagnostics.WindowsEventLog.DataSources.Add(AzureConstants.AzureEventLogFilter);
            diagnostics.WindowsEventLog.DataSources.Add(AzureConstants.AllApplicationEvents);
            diagnostics.WindowsEventLog.DataSources.Add(AzureConstants.AllSystemEvents);
            diagnostics.WindowsEventLog.ScheduledTransferLogLevelFilter = logLevelFilter;
            diagnostics.WindowsEventLog.ScheduledTransferPeriod = scheduledTransferPeriod;

            // Directory logs
            diagnostics.Directories.ScheduledTransferPeriod = scheduledTransferPeriod;
            // Quotas
            diagnostics.Logs.BufferQuotaInMB = 1024;
            diagnostics.Directories.BufferQuotaInMB = 0;
            diagnostics.WindowsEventLog.BufferQuotaInMB = 1024;
            diagnostics.PerformanceCounters.BufferQuotaInMB = 1024;
            diagnostics.DiagnosticInfrastructureLogs.BufferQuotaInMB = 1024;

            DiagnosticMonitor.Start("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString", diagnostics);

            // Connect Brightstar tracing to a diagnostics monitor listener
            var listener = new DiagnosticMonitorTraceListener();
            BrightstarDB.Logging.BrightstarTraceSource.Listeners.Add(listener);
            BrightstarDB.Logging.BrightstarTraceSource.Switch.Level = SourceLevels.Verbose;
            
        }

        private static void ConfigurePerformanceCounters(DiagnosticMonitorConfiguration diagnostics, TimeSpan sampleRate)
        {
            var perfCounters = GetPerformanceCounters().Split(';');
            foreach(var pc in perfCounters.Select(x=>x.Trim()))
            {
                if (!String.IsNullOrEmpty(pc))
                {
                    diagnostics.PerformanceCounters.DataSources.Add(
                        new PerformanceCounterConfiguration{CounterSpecifier = pc, SampleRate = sampleRate});
                }
            }
        }
        private static LogLevel GetLogLevelFilter()
        {
            var logLevelFilter = RoleEnvironment.GetConfigurationSettingValue("LogLevelFilter");
            switch (logLevelFilter)
            {
                case "Verbose":
                    return LogLevel.Verbose;
                case "Information":
                    return LogLevel.Information;
                case "Warning":
                    return LogLevel.Warning;
                case "Error":
                    return LogLevel.Error;
                case "Critical":
                    return LogLevel.Critical;
                default:
                    return LogLevel.Undefined;
            }
        }

        private static string GetPerformanceCounters()
        {
            try
            {
                return RoleEnvironment.GetConfigurationSettingValue(AzureConstants.PerformanceCountersPropertyName);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

    }
}

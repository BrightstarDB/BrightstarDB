using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using VDS.RDF.Parsing.Tokens;

namespace BrightstarDB.Polaris
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal static readonly TraceSource PolarisTraceSource = new TraceSource("Polaris", SourceLevels.Warning);
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var logFileArg = e.Args.FirstOrDefault(x => x.Length > 5 && x.ToLowerInvariant().StartsWith("/log:"));
            if (logFileArg != null)
            {
                var logFileName = logFileArg.Substring(5);
                var writer = new TextWriterTraceListener(logFileName)
                                                     {TraceOutputOptions = TraceOptions.DateTime};
                Logging.BrightstarTraceSource.Listeners.Add(writer);
                PolarisTraceSource.Listeners.Add(writer);
                if (e.Args.Any(x => x.ToLowerInvariant().Equals("/verbose")))
                {
                    Logging.BrightstarTraceSource.Switch.Level = SourceLevels.Verbose;
                    PolarisTraceSource.Switch.Level = SourceLevels.Verbose;
                }
            }
            VDS.RDF.Options.DefaultTokenQueueMode = TokenQueueMode.AsynchronousBufferDuringParsing;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        }

        private static void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                PolarisTraceSource.TraceEvent(TraceEventType.Critical, 0, "Unhandled application domain exception. {0}", e.ExceptionObject);
            }
            catch (Exception)
            {
            }
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                PolarisTraceSource.TraceEvent(TraceEventType.Critical, 0, "Unhandled application domain exception. {0}", e.Exception);
            }
            catch (Exception)
            {

            }
        }

        
    }
}

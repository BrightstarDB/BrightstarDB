#if SILVERLIGHT
#else
using System.Diagnostics;

#endif

namespace BrightstarDB
{
    /// <summary>
    /// Provides methods to configure the Brightstar logging system
    /// </summary>
    /// <remarks>This class will be subject to review prior to the 1.0 release of Brightstar and may change significantly for the final release.</remarks>
    public class Logging
    {
#if SILVERLIGHT
        internal static void LogInfo(string msg, params object[] args){}
        internal static void LogError(BrightstarEventId eventId, string msg, params object[] args){}
        internal static void LogWarning(BrightstarEventId eventId, string msg, params object[] args){}
        internal static void LogError(string msg, params object [] args){}
        internal static void LogDebug(string msg, params object [] args){}
        internal static bool IsDebugEnabled
        {
            get { return false; }
        }

#else
        /// <summary>
        /// Gets the <see cref="TraceSource"/> that BrightstarDB writes all logging to
        /// </summary>
        public static readonly TraceSource BrightstarTraceSource = new TraceSource("BrightstarDB",SourceLevels.Information);

        private static void Initialise(string logfileName)
        {
            var traceListener = new TextWriterTraceListener(logfileName);
            traceListener.TraceOutputOptions = TraceOptions.DateTime | TraceOptions.Timestamp | TraceOptions.ProcessId;
            traceListener.Name = "Default";
            Trace.AutoFlush = true;
            BrightstarTraceSource.Listeners.Add(traceListener);
        }

        static Logging()
        {
            if (Configuration.StoreLocation != null)
            {
                Initialise(System.IO.Path.Combine(Configuration.StoreLocation, "log.txt"));
            }
        }

        internal static void LogDebug(string msg, params object[] args)
        {
            if (BrightstarTraceSource.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                BrightstarTraceSource.TraceInformation(msg, args);
            }
        }

        internal static void LogInfo(string msg, params object[] args)
        {
            BrightstarTraceSource.TraceInformation(msg, args);
        }

        internal static void LogWarning(BrightstarEventId eventId, string msg, params object[] args)
        {
            BrightstarTraceSource.TraceEvent(TraceEventType.Warning, (int)eventId, msg, args);
        }


        internal static void LogError(BrightstarEventId eventId, string msg, params object[] args)
        {
            BrightstarTraceSource.TraceEvent(TraceEventType.Error, (int)eventId, msg, args);
        }

        /// <summary>
        /// Directs all logging output to the console as well as any other configured log appenders.
        /// </summary>
        /// <param name="debugOn"></param>
        public static void EnableConsoleOutput(bool debugOn)
        {
            var consoleListener = new ConsoleTraceListener {TraceOutputOptions = TraceOptions.DateTime};
            if (debugOn)
            {
                BrightstarTraceSource.Switch.Level = SourceLevels.Verbose;
            }
            BrightstarTraceSource.Listeners.Add(consoleListener);
        }

        internal static bool IsDebugEnabled { get { return BrightstarTraceSource.Switch.Level >= SourceLevels.Verbose; } }
        internal static bool IsInfoEnabled { get { return BrightstarTraceSource.Switch.Level >= SourceLevels.Information; } }
        internal static bool IsWarnEnabled { get { return BrightstarTraceSource.Switch.Level >= SourceLevels.Warning; } }
        internal static bool IsErrorEnabled { get { return BrightstarTraceSource.Switch.Level >= SourceLevels.Error; } }
#endif

    }
}

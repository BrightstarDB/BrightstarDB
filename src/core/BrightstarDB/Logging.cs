#if PORTABLE
using BrightstarDB.Portable.Adaptation;
using BrightstarDB.Portable.Compatibility;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BrightstarDB.Storage;
#endif
using System.Diagnostics;

namespace BrightstarDB
{
    /// <summary>
    /// Provides methods to configure the Brightstar logging system
    /// </summary>
    /// <remarks>This class will be subject to review prior to the 1.0 release of Brightstar and may change significantly for the final release.</remarks>
    public class Logging
    {
#if SILVERLIGHT || PORTABLE
        private class LogItem
        {
            private string Message { get; set; }
            private object[] Args { get; set; }

            public LogItem(string msg, object[] args)
            {
                Message = msg;
                Args = args;
            }

            public void WriteTo(TextWriter tw)
            {
                if (Args != null)
                {
                    tw.WriteLine(Message, Args);
                }
                else
                {
                    tw.WriteLine(Message);
                }
            }
        }

        public enum LogLevel
        {
            Debug,
            Info,
            Warn,
            Error
        }

        private static readonly ConcurrentQueue<LogItem> LogQueue = new ConcurrentQueue<LogItem>();
        private static IPersistenceManager _persistenceManager;
        private static string _logFileName;
        private static readonly ManualResetEvent Wait = new ManualResetEvent(false);
        private static readonly Task LogWorker;
        private static bool _stopping;

        public static void Initialise(string logFileName, bool enableDebug = false)
        {
            _persistenceManager = PlatformAdapter.Resolve<IPersistenceManager>();
            if (logFileName != null)
            {
                if (!_persistenceManager.FileExists(logFileName))
                {
                    _persistenceManager.CreateFile(logFileName);
                }
                _logFileName = logFileName;
            }
            IsDebugEnabled = enableDebug;
        }

        static Logging()
        {
            LogWorker = new Task(DoWriteLog);
            LogWorker.Start();
        }

        public static void Shutdown()
        {
            _stopping = true;
            Wait.Set();
            LogWorker.Wait(2000);
        }

        /// <summary>
        /// Background write of log messages to log file.
        /// </summary>
        static void DoWriteLog()
        {
            while (!_stopping)
            {
                if (!LogQueue.IsEmpty)
                {
                    if (!String.IsNullOrEmpty(_logFileName))
                    {
                        using (var logStream = _persistenceManager.GetOutputStream(_logFileName, FileMode.Append))
                        {
                            using (var logWriter = new StreamWriter(logStream))
                            {
                                LogItem logItem;
                                while (LogQueue.TryDequeue(out logItem))
                                {
                                    logItem.WriteTo(logWriter);
                                }
                                logStream.Flush();
                                logWriter.Close();
                            }
                        }
                    }
                    else
                    {
                        // Not currently logging to a file - so just dequeue all the messages
                        LogItem logItem;
                        while (LogQueue.TryDequeue(out logItem))
                        {
                            // No-op
                        }
                    }
                }
                Wait.Reset();
                Wait.WaitOne(1000);
            }
        }

        private static int ThreadId
        {
            get
            {
                return Thread.CurrentThread == null ? -1 : Thread.CurrentThread.ManagedThreadId;
            }
        }

        private static void WriteLog(LogLevel lvl, string msg, params object[] args)
        {
            var logMessage = DateTime.Now.ToString(System.Globalization.CultureInfo.CurrentCulture) +
                             " (" + ThreadId + ") : " + lvl + " : " + msg;
            if (args != null && args.Length > 0) // Under PCL args can be an empty array if there are no params. 
            {
                Debug.WriteLine(logMessage, args);
            }
            else
            {
                Debug.WriteLine(logMessage);
            }
            if (_logFileName != null)
            {
                LogQueue.Enqueue(new LogItem(logMessage, args));
                Wait.Set();
            }
        }

        internal static void LogInfo(string msg, params object[] args)
        {
            WriteLog(LogLevel.Info, msg, args);
        }

        internal static void LogError(BrightstarEventId eventId, string msg, params object[] args)
        {
            WriteLog(LogLevel.Error, msg, args);
        }

        internal static void LogWarning(BrightstarEventId eventId, string msg, params object[] args)
        {
            WriteLog(LogLevel.Warn, msg, args);
        }

        internal static void LogError(string msg, params object[] args)
        {
            WriteLog(LogLevel.Error, msg, args);
        }

        internal static void LogDebug(string msg, params object[] args)
        {
            if (IsDebugEnabled)
            {
                WriteLog(LogLevel.Debug, msg, args);
            }
        }

        internal static bool IsDebugEnabled { get; set; }

        internal static bool IsProfilingEnabled
        {
            get { return false; }
        }
#else
        /// <summary>
        /// Gets the <see cref="TraceSource"/> that BrightstarDB writes all logging to
        /// </summary>
        public static readonly TraceSource BrightstarTraceSource = new TraceSource("BrightstarDB",SourceLevels.Information);

        private static void Initialise(string logfileName, bool debugEnabled = false)
        {
            var traceListener = new TextWriterTraceListener(logfileName)
            {
                TraceOutputOptions = TraceOptions.DateTime | TraceOptions.Timestamp | TraceOptions.ProcessId,
                Name = "Default"
            };
            Trace.AutoFlush = true;
            BrightstarTraceSource.Listeners.Add(traceListener);
            if (debugEnabled)
            {
                BrightstarTraceSource.Switch.Level = SourceLevels.Verbose;
            }
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

        internal static void LogDebug(string msg)
        {
            if (BrightstarTraceSource.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                BrightstarTraceSource.TraceInformation(msg);
            }
        }

        internal static void LogInfo(string msg)
        {
            BrightstarTraceSource.TraceInformation(msg);
        }

        internal static void LogInfo(string msg, params object[] args)
        {
            BrightstarTraceSource.TraceInformation(msg, args);
        }

        internal static void LogWarning(BrightstarEventId eventId, string msg)
        {
            BrightstarTraceSource.TraceEvent(TraceEventType.Warning, (int)eventId, msg);
        }

        internal static void LogWarning(BrightstarEventId eventId, string msg, params object[] args)
        {
            BrightstarTraceSource.TraceEvent(TraceEventType.Warning, (int)eventId, msg, args);
        }

        internal static void LogError(BrightstarEventId eventId, string msg)
        {
            BrightstarTraceSource.TraceEvent(TraceEventType.Error, (int)eventId, msg);
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

        /// <summary>
        /// Enable or disable profiling.
        /// </summary>
        /// <param name="profilingOn">True to turn on profiling, false otherwise</param>
        /// <remarks>By default profiling is disabled. If profiling is enabled several key components of the code will generate and emit profiling information. 
        /// Collecting this information is a small CPU overhead but can be a substantial memory overhead for large operations.
        /// It is recommended only to enable profiling if you are absolutely sure you need the information it produces.</remarks>
        public static void EnableProfiling(bool profilingOn)
        {
            IsProfilingEnabled = profilingOn;
        }

        internal static bool IsDebugEnabled { get { return BrightstarTraceSource.Switch.Level >= SourceLevels.Verbose; } }
        internal static bool IsInfoEnabled { get { return BrightstarTraceSource.Switch.Level >= SourceLevels.Information; } }
        internal static bool IsWarnEnabled { get { return BrightstarTraceSource.Switch.Level >= SourceLevels.Warning; } }
        internal static bool IsErrorEnabled { get { return BrightstarTraceSource.Switch.Level >= SourceLevels.Error; } }
        internal static bool IsProfilingEnabled { get; private set; }
#endif

    }
}

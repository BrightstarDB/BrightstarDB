using System;
using Serilog;
using Serilog.Events;

namespace BrightstarDB
{
    /// <summary>
    /// Provides methods to configure the Brightstar logging system
    /// </summary>
    /// <remarks>This class will be subject to review prior to the 1.0 release of Brightstar and may change significantly for the final release.</remarks>
    public class Logging
    {

        private static void Initialise(string logfileName, bool debugEnabled = false)
        {
            var configuration = new LoggerConfiguration().WriteTo.File(logfileName);
            if (debugEnabled)
            {
                configuration.MinimumLevel.Debug();
            }
            else
            {
                configuration.MinimumLevel.Information();
            }

            Log.Logger = configuration.CreateLogger();
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
            Log.Logger.Debug(msg, args);
        }

        internal static void LogDebug(string msg)
        {
            Log.Logger.Debug(msg);
        }

        internal static void LogInfo(string msg)
        {
            Log.Logger.Information(msg);
        }

        internal static void LogInfo(string msg, params object[] args)
        {
            Log.Logger.Information(msg, args);
        }

        internal static void LogWarning(BrightstarEventId eventId, string msg)
        {
            Log.Logger.Warning("{EventId} : " + msg, eventId);
        }

        internal static void LogWarning(BrightstarEventId eventId, string msg, params object[] args)
        {
            var modifiedArgs = new object[args.Length + 1];
            modifiedArgs[0] = eventId;
            Array.Copy(args, 0, modifiedArgs, 1, args.Length);
            Log.Logger.Warning("{EventId} : " + msg, modifiedArgs);
        }

        internal static void LogError(BrightstarEventId eventId, string msg)
        {
            Log.Logger.Error("{EventId} : " + msg, eventId);
        }

        internal static void LogError(BrightstarEventId eventId, string msg, params object[] args)
        {
            var modifiedArgs = new object[args.Length + 1];
            modifiedArgs[0] = eventId;
            Array.Copy(args, 0, modifiedArgs, 1, args.Length);
            Log.Logger.Error("{EventId} : " + msg, modifiedArgs);
        }

        /// <summary>
        /// Directs all logging output to the console as well as any other configured log appenders.
        /// </summary>
        /// <param name="debugOn"></param>
        public static void EnableConsoleOutput(bool debugOn)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Logger(Log.Logger).WriteTo
                .Console(debugOn ? LogEventLevel.Information : LogEventLevel.Verbose).CreateLogger();
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

        internal static bool IsDebugEnabled => Log.Logger.IsEnabled(LogEventLevel.Debug);
        internal static bool IsInfoEnabled => Log.Logger.IsEnabled(LogEventLevel.Information);
        internal static bool IsWarnEnabled => Log.Logger.IsEnabled(LogEventLevel.Warning);
        internal static bool IsErrorEnabled => Log.Logger.IsEnabled(LogEventLevel.Error);
        internal static bool IsProfilingEnabled { get; private set; }

    }
}

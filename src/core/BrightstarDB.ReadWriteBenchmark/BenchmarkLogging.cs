using System.Diagnostics;
using System.IO;

namespace BrightstarDB.ReadWriteBenchmark
{
    class BenchmarkLogging
    {
        private static readonly TraceSource TraceSource = new TraceSource("BrightstarDB.ReadWriteBenchmark", SourceLevels.All);

        static BenchmarkLogging()
        {
            TraceSource.Listeners.Remove("Default");
            var console = new ConsoleTraceListener(false)
            {
                Name = "console",
                Filter = new EventTypeFilter(SourceLevels.Information)
            };
            TraceSource.Listeners.Add(console);
        }

        public static void EnableFileLogging(string logFilePath)
        {
            var logFileWriter = new StreamWriter(logFilePath);
            var fileListenerIx = TraceSource.Listeners.Add(new TextWriterTraceListener(logFileWriter));
            TraceSource.Listeners[fileListenerIx].Name = "file";
        }

        public static void Info(string msg, params object[] args)
        {
            TraceSource.TraceEvent(TraceEventType.Information, 1, msg, args);
        }

        public static void Close()
        {
            TraceSource.Flush();
            TraceSource.Close();
        }

        public static void Error(string fmt, params object[]args)
        {
            TraceSource.TraceEvent(TraceEventType.Error, 1, fmt, args);
        }

        public static void Debug(string fmt, params object[] args)
        {
            TraceSource.TraceEvent(TraceEventType.Verbose, 1, fmt, args);
        }
    }
}

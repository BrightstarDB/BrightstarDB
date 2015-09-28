using CommandLine;

namespace BrightstarDB.ReadWriteBenchmark
{

    public class BenchmarkArgs
    {
        public BenchmarkArgs()
        {
            ConnectionString = "type=rest;endpoint=http://localhost:8090/brightstar";
            TestDataDirectory = ".";
        }

        [Argument(ArgumentType.AtMostOnce, ShortName = "c",
    HelpText = "The connection string for the BrightstarDB server. If not provided, the value must be specified in the appSettings section of the configuration file.")]
        public string ConnectionString;

        [Argument(ArgumentType.AtMostOnce, ShortName = "d",
            HelpText = "The path to the directory that contains the test data to be loaded")]
        public string TestDataDirectory;

        [Argument(ArgumentType.AtMostOnce, ShortName = "l",
            HelpText = "Path to the log file to write progress and summary messages to")]
        public string LogFilePath;

        [Argument(ArgumentType.AtMostOnce, ShortName = "n", HelpText = "The maximum number of input files to process")]
        public int MaxNumberFiles;
    }
}

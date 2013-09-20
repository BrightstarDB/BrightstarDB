using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
namespace BulkImport
{
    public class BulkImportArguments
    {
        [Argument(ArgumentType.AtMostOnce, ShortName = "c",
            HelpText = "The BrightstarDB connection string to use to open the store to import into.")] 
        public string ConnectionString = "type=http;endpoint=http://localhost:8090/brightstar";

        [Argument(ArgumentType.Required, ShortName = "s", HelpText = "The name of the store to import data into. The store will be created if it does not already exist.")]
        public string StoreName;

        [Argument(ArgumentType.Required, ShortName = "d", HelpText = "The path to the import directory containing the files to be imported")]
        public string ImportDirectory;

        [Argument(ArgumentType.AtMostOnce, ShortName = "p", HelpText = "The pattern to use to match the names of the files to be imported")]
        public string FilePattern = "*.*";

        [Argument(ArgumentType.AtMostOnce, ShortName = "m",
            HelpText = "Specify the directory to which imported file should be moved after successful import")] 
        public string MoveTo;

        [Argument(ArgumentType.AtMostOnce, ShortName = "l", HelpText = "The path to the file to write logging information to.")]
        public string LogFile = "import.log";

        [Argument(ArgumentType.AtMostOnce, DefaultValue = false, ShortName = "pr", HelpText = "Periodically log the import job status message to the console.")]
        public bool LogProgress = false;
    }
}

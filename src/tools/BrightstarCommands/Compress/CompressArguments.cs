using CommandLine;
namespace Compress
{
    public class CompressArguments
    {
        [Argument(ArgumentType.AtMostOnce, ShortName = "c",
            HelpText = "The BrightstarDB connection string to use to open the store to import into.")] 
        public string ConnectionString = "type=http;endpoint=http://localhost:8090/brightstar";

        [Argument(ArgumentType.Required, ShortName = "s", HelpText = "The name of the store to import data into. The store will be created if it does not already exist.")]
        public string StoreName;
    }
}

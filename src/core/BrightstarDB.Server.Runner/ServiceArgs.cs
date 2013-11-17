using BrightstarDB.Server.Modules.Permissions;
using CommandLine;

namespace BrightstarDB.Server.Runner
{
    
    public class ServiceArgs
    {
        public ServiceArgs()
        {
            BaseUris = new[] {"http://localhost:8090/brightstar/"};
        }

        [Argument(ArgumentType.MultipleUnique,
            LongName = "ServiceUri",
            ShortName = "u",
            HelpText =
                "The base URI that the service will listen on for connections. Can be repeated multiple times to listen on multiple endpoints."
            ,
            DefaultValue = new[] {"http://localhost:8090/brightstar/"})] 
        public string[] BaseUris;

        [Argument(ArgumentType.AtMostOnce, ShortName = "c",
            HelpText = "The connection string for the BrightstarDB server. If not provided, the value must be specified in the appSettings section of the configuration file.")] 
        public string ConnectionString;

        // TODO: Not currently supported
        //[Argument(ArgumentType.AtMostOnce, ShortName = "https",
        //    HelpText = "Require connections to use HTTPS")] 
        //public bool RequireHttps;

        [Argument(ArgumentType.AtMostOnce, HelpText = "The full path to the directory containing the Views and assets folder for the server",
            ShortName = "r")]
        public string RootPath;

        [Argument(ArgumentType.AtMostOnce, ShortName = "t",
            HelpText = "Configure Nancy to display error traces in the browser")] 
        public bool ShowErrorTraces;
    }
}

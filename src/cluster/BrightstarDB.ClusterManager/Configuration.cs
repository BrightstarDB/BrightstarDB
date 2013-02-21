using System.Configuration;

namespace BrightstarDB.ClusterManager
{
    internal class Configuration
    {
        private const string HttpPortName = "BrightstarDB.HttpPort";
        private const string TcpPortName = "BrightstarDB.TcpPort";
        private const string NetNamedPipeName = "BrightstarDB.NetNamedPipeName";
        private const string LogLevelPropertyName = "BrightstarDB.LogLevel";

        public static string LogLevel { get; set; }

        // These are the ports used by the ClusterManagerService
        public static int HttpPort { get; set; }
        public static int TcpPort { get; set; }
        public static string NamedPipeName { get; set; }

        /// <summary>
        /// This is the port 
        /// </summary>
        public static int ManagementPort { get; set; }
        public const int DefaultHttpPort = 9090;
        public const int DefaultTcpPort = 9095;
        public const string DefaultPipeName = "brightstarcluster";

        static Configuration()
        {
            var appSettings = ConfigurationManager.AppSettings;

            LogLevel = appSettings.Get(LogLevelPropertyName);

            var httpPortValue = appSettings.Get(HttpPortName);
            if (!string.IsNullOrEmpty(httpPortValue))
            {
                int port;
                if (!int.TryParse(httpPortValue, out port))
                {
                    port = DefaultHttpPort;
                }
                HttpPort = port;
            }
            else
            {
                HttpPort = DefaultHttpPort;
            }

            var tcpPortValue = appSettings.Get(TcpPortName);
            if (!string.IsNullOrEmpty(tcpPortValue))
            {
                int port;
                if (!int.TryParse(tcpPortValue, out port))
                {
                    port = DefaultTcpPort;
                }
                TcpPort = port;
            }
            else
            {
                TcpPort = DefaultTcpPort;
            }

            var namedPipeValue = appSettings.Get(NetNamedPipeName);
            NamedPipeName = !string.IsNullOrEmpty(namedPipeValue) ? namedPipeValue : DefaultPipeName;
        }
    }
}

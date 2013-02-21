using System.Net.Sockets;

namespace BrightstarDB.ClusterNode
{
    public class SyncContext
    {
        internal TcpClient Connection { get; set; }
    }
}
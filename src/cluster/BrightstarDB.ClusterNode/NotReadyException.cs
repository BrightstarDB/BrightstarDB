using System;

namespace BrightstarDB.ClusterNode
{
    /// <summary>
    /// Exception thrown when the node is asked to handle a request while it is still initializing.
    /// </summary>
    public class NotReadyException : Exception
    {
    }
}
using System;

namespace BrightstarDB
{
    /// <summary>
    /// An enumeration of the allowed values for the Type parameter of a connection string.
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>
        /// Connection type for an embedded connection to a directory
        /// </summary>
        Embedded,

        /// <summary>
        /// Connection type for a client connection to a Brightstar server over HTTP
        /// </summary>
        [Obsolete("Http ConnectionType is no longer supported and will be removed")]
        Http,
        /// <summary>
        /// Connection type for a client connection to a Brightstar server over TCP
        /// </summary>
        [Obsolete("Http ConnectionType is no longer supported and will be removed")]
        Tcp,
        /// <summary>
        /// Connection type for a client connection to a Brightstar server over a named pipe
        /// </summary>
        [Obsolete("Http ConnectionType is no longer supported and will be removed")]
        NamedPipe,

        /// <summary>
        /// Connection type for a client connection to a Brightstar REST endpoint over HTTP or HTTPS
        /// </summary>
        Rest
    }
}
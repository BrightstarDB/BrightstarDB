using System;
using BrightstarDB.Storage.Persistence;
using SmartAssembly.Attributes;

namespace BrightstarDB.Storage
{
    /// <summary>
    /// Exception raised when an attempt is made to write to a block other than the active block
    /// of an <see cref="IBlockProvider"/>
    /// </summary>
    [DoNotObfuscate]
    public class InvalidWriteOffsetException : BrightstarException
    {
        /// <summary>
        /// Get the path of the block store that was accessed
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Get the offset that the caller tried to write to
        /// </summary>
        public long Offset { get; private set; }

        internal InvalidWriteOffsetException(string path, long offset) : base(String.Format("The offset {0} is not the current active block offset for the path {1}", offset, path))
        {
            Path = path;
            Offset = offset;
        }
    }
}
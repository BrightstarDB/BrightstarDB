using System;

namespace BrightstarDB.Azure.Management
{
    /// <summary>
    /// Enumeration of the different access privileges that can be granted on a store.
    /// Values can be combined as flags.
    /// </summary>
    [Flags]
    public enum StoreAccessLevel
    {
        None= 0,
        Read = 1,
        Write = 2,
        Export = 4,
        Admin = 8,
        Owner = 16,
        Superuser = 32
    }
}
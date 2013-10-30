using System;

namespace BrightstarDB.Server.Modules.Permissions
{
    [Flags]
    public enum SystemPermissions
    {
        None = 0x0,

        ListStores = 0x01,
        CreateStore = 0x02,
        Admin = 0x8000,

        All = ListStores | CreateStore | Admin
    }
}
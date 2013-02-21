using BrightstarDB.EntityFramework;

namespace BrightstarDB.Azure.Management.Accounts
{
    [Entity]
    public interface IStoreAccess
    {
        [Identifier("http://demand.brightstar.com/accounts/storeAccess/")]
        string Id { get; }

        /// <summary>
        /// The user granted access
        /// </summary>
        IAccount Grantee { get; set; }

        /// <summary>
        /// The level of access granted
        /// </summary>
        int GrantLevel { get; set; }

        /// <summary>
        /// The store that this resource grants access to
        /// </summary>
        IStore AccessStore { get; set; }
    }
}

using BrightstarDB.EntityFramework;

namespace BrightstarDB.Azure.Management.Accounts
{
    [Entity]
    public interface IUserToken
    {
        [Identifier("http://demand.brightstardb.com/accounts/usertoken/")]
        string Id { get; }
    }
}
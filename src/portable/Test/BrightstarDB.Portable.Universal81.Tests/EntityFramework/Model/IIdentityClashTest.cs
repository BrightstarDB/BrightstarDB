using BrightstarDB.EntityFramework;

namespace BrightstarDB.Portable.Tests.EntityFramework.Model
{
    [Entity]
    public interface IIdentityClashTest
    {
        string Id { get; }
    }

    [Entity]
    public interface IIdentityClashTestLevel1 : IIdentityClashTest
    {
        //[Identifier]
        //string MyId { get; }
    }

    [Entity]
    public interface IIdentityClashTestLevel2 : IIdentityClashTestLevel1
    {
        //[Identifier]
        //string MyId { get; }
    }

}

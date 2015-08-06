using BrightstarDB.EntityFramework;

namespace BrightstarDB.Portable.Tests.EntityFramework.Model
{
    [Entity]
    public interface ISession
    {
        string Id { get; }
        string Speaker { get; set; }
    }

    [Entity]
    public interface IEveningSession : ISession
    {
        string DateTime { get; set; }
        bool FreeBeer { get; set; }
    }

    [Entity]
    public interface ITechnicalEveningSession : IEveningSession
    {
        string Subject { get; set; }
    }
}

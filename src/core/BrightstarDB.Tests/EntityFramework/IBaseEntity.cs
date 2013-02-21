using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity]
    public interface IBaseEntity
    {
        string BaseStringValue { get; set; }
    }
}

using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity]
    public interface IBaseEntity
    {
        [Identifier("http://example.org/entities/")]
        string Id { get; }

        string BaseStringValue { get; set; }
    }
}

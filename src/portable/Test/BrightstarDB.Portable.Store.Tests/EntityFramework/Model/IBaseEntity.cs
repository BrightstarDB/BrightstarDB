using BrightstarDB.EntityFramework;

namespace BrightstarDB.Portable.Tests.EntityFramework.Model
{
    [Entity]
    public interface IBaseEntity
    {
        [Identifier("http://example.org/entities/")]
        string Id { get; }

        string BaseStringValue { get; set; }
    }
}

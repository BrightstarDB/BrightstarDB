using BrightstarDB.EntityFramework;

namespace BrightstarDB.Portable.Tests.EntityFramework.Model
{
    [Entity]
    public interface IStringKeyEntity
    {
        [Identifier("http://example.org/auto-entity/", KeyProperties = new[]{"Name"})]
        string Id { get; }

        string Name { get; set; }

        string Description { get; set; }
    }
}

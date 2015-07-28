using BrightstarDB.EntityFramework;

namespace BrightstarDB.Portable.Tests.EntityFramework.Model
{
    [Entity]
    public interface IHierarchicalKeyEntity
    {
        [Identifier(BaseAddress = "http://example.org/", KeyProperties = new[]{"Parent", "Code"})]
        string Id { get; }

        IHierarchicalKeyEntity Parent { get; set; }
        string Code { get; set; }
    }
}

using BrightstarDB.EntityFramework;

namespace BrightstarDB.Portable.Tests.EntityFramework.Model
{
    [Entity]
    public interface IChildKeyEntity
    {
        [Identifier(BaseAddress = "http://example.org/", KeyProperties = new[]{"Parent", "Position"})]
        string Id { get; }

        IBaseEntity Parent { get; set; }
        int Position { get; set; }
    }
}

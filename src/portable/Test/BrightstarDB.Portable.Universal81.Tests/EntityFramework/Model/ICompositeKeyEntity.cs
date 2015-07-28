using BrightstarDB.EntityFramework;

namespace BrightstarDB.Portable.Tests.EntityFramework.Model
{
    [Entity]
    public interface ICompositeKeyEntity
    {
        [Identifier(BaseAddress = "http://example.org/composite/", KeyProperties = new[]{"First", "Second"}, KeySeparator = ".")]
        string Id { get; }

        string First { get; set; }
        int Second { get; set; }
    }
}

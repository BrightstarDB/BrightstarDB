using BrightstarDB.EntityFramework;

namespace BrightstarDB.Portable.Tests.EntityFramework.Model
{

    [Entity]
    interface ILowerKeyEntity
    {
        [Identifier(BaseAddress = "http://example.org/", KeyConverterType = typeof(LowercaseKeyConverter), KeyProperties = new[]{"Name"})]
        string Id { get; }
        string Name { get; set; }
    }
}

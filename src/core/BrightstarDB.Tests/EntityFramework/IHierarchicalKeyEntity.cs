using System.Collections.Generic;
using System.Text;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
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

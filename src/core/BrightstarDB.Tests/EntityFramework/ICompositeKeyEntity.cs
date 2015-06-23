using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity]
    public interface ICompositeKeyEntity
    {
        [Identifier(BaseAddress = "http://example.org/composite/", KeyProperties = new[]{"First", "Second"}, KeySeparator = ".")]
        string Id { get; }

        string First { get; set; }
        int Second { get; set; }

        string Description { get; set; }
    }
}

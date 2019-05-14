using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
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

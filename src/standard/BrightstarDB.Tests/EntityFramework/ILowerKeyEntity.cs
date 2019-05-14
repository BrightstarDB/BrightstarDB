using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{

    [Entity]
    interface ILowerKeyEntity
    {
        [Identifier(BaseAddress = "http://example.org/", KeyConverterType = typeof(LowercaseKeyConverter), KeyProperties = new[]{"Name"})]
        string Id { get; }
        string Name { get; set; }
    }
}

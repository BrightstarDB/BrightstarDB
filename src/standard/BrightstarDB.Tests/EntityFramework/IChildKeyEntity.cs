using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
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

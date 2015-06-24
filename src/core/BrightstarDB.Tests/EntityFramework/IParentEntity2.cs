using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity]
    public interface IParentEntity2
    {
        [Identifier("http://example.org/repro/")]
        string Id { get; }

        [InverseProperty("Parent")]
        ICollection<IChildEntity2> Children { get; set; }
    }
}

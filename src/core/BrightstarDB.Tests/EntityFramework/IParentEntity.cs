using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity]
    public interface IParentEntity
    {
        [Identifier("http://example.org/repro/")]
        string Id { get; }

        ICollection<IChildEntity> Children { get; set; } 
    }
}

using System;
using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity]
    public interface IDerivedEntity : IBaseEntity
    {
        DateTime DateTimeProperty { get; set; }
        ICollection<IBaseEntity> RelatedEntities { get; set; }
    }
}
using System;
using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity]
    public interface IDerivedEntity : IBaseEntity
    {
        string Id { get; }
        DateTime DateTimeProperty { get; set; }
        ICollection<IBaseEntity> RelatedEntities { get; set; }
    }
}
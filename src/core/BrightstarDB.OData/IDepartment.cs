using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.OData
{
    [Entity]
    public interface IDepartment
    {
        [Identifier("http://www.brightstardb.com/.well-known/genid/")]
        string Id { get; }
        
        string Name { get; set; }

        ICollection<IPerson> Members { get; set; } 
    }
}
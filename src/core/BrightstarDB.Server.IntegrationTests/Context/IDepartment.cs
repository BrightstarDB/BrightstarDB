using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Server.IntegrationTests.Context
{
    [Entity]
    public interface IDepartment
    {
        string Id { get; }
        string Name { get; set; }

        int DeptId { get; set; }

        [InverseProperty("Department")]
        ICollection<IPerson> Persons { get; }
    }
}

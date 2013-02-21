using System;
using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.OData.Tests
{
    [Entity]
    public interface ICompany
    {
        [Identifier("http://example.org/companies/")]
        string Id { get; }

        string Name { get; set; }

        string Address { get; set; }

        DateTime DateFormed { get; set; }

        decimal SomeDecimal { get; set; }

        double SomeDouble { get; set; }

        [InverseProperty("Company")]
        ICollection<IDepartment> Departments { get; set; } 
    }
}

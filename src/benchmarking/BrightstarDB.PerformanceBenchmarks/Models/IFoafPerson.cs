using System;
using System.Collections.Generic;
using System.Text;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.PerformanceBenchmarks.Models
{
    [Entity("http://xmlns.com/foaf/0.1/Person")]
    public interface IFoafPerson
    {
        string Id { get; }

        [PropertyType("http://xmlns.com/foaf/0.1/name")]
        string Name { get; set; }
        
        [PropertyType("http://xmlns.com/foaf/0.1/givenName")]
        string GivenName { get; set; }

        [PropertyType("http://xmlns.com/foaf/0.1/familyName")]
        string FamilyName { get; set; }

        [PropertyType("http://xmlns.com/foaf/0.1/age")]
        int Age { get; set; }

        [PropertyType("http://xmlns.com/foaf/0.1/organization")]
        string Organisation { get; set; }

        [PropertyType("http://xmlns.com/foaf/0.1/knows")]
        ICollection<IFoafPerson> Knows { get; set; }

    }
}

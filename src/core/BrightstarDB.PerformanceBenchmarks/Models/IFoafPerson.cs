using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.PerformanceBenchmarks.Models
{
    [Entity("foaf:Person")]
    interface IFoafPerson
    {
        [Identifier("http://www.brightstardb.com/people/")]
        string Id { get; }

        [PropertyType("foaf:nick")]
        string Nickname { get; set; }

        [PropertyType("foaf:name")]
        string Name { get; set; }

        [PropertyType("foaf:givenName")]
        string GivenName { get; set; }

        [PropertyType("foaf:familyName")]
        string FamilyName { get; set; }

        [PropertyType("foaf:Organization")]
        string Organisation { get; set; }

        [PropertyType("foaf:age")]
        int Age { get; set; }

        [PropertyType("foaf:knows")]
        ICollection<IFoafPerson> Knows { get; set; }

        [InversePropertyType("foaf:knows")]
        ICollection<IFoafPerson> KnownBy { get; set; }
    }
}

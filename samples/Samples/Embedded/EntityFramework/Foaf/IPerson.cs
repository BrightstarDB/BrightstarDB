using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Samples.EntityFramework.Foaf
{
    [Entity("http://xmlns.com/foaf/0.1/Person")]
    public interface IPerson
    {
        [Identifier("http://www.brightstardb.com/people/")]
        string Id { get; }

        [PropertyType("foaf:nick")]
        string Nickname { get; set; }

        [PropertyType("foaf:name")]
        string Name { get; set; }

        [PropertyType("foaf:Organization")]
        string Organisation { get; set; }

        [PropertyType("foaf:knows")]
        ICollection<IPerson> Knows { get; set; }

        [InversePropertyType("foaf:knows")]
        ICollection<IPerson> KnownBy { get; set; }
    }
}

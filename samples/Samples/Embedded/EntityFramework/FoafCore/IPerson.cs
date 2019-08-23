using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Samples.EntityFramework.FoafCore
{
    [Entity("http://xmlns.com/foaf/0.1/Person")]
    public interface IPerson
    {
        [Identifier("http://www.brightstardb.com/people/")]
        string Id { get; }

        [PropertyType("http://xmlns.com/foaf/0.1/nick")]
        string Nickname { get; set; }

        [PropertyType("http://xmlns.com/foaf/0.1/name")]
        string Name { get; set; }

        [PropertyType("http://xmlns.com/foaf/0.1/Organization")]
        string Organisation { get; set; }

        [PropertyType("http://xmlns.com/foaf/0.1/knows")]
        ICollection<IPerson> Knows { get; set; }

        [InversePropertyType("http://xmlns.com/foaf/0.1/knows")]
        ICollection<IPerson> KnownBy { get; set; }
    }
}

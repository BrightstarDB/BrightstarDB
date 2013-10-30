using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Server.IntegrationTests.Context
{
    [Entity("http://xmlns.com/foaf/0.1/Agent")]
    public interface IFoafAgent
    {
        [PropertyType("http://xmlns.com/foaf/0.1/mbox_sha1sum")]
        ICollection<string> MboxSums { get; set; }
    }
}
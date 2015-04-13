using System.Security.Cryptography.X509Certificates;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity]
    interface INoId
    {
        string Name { get; set; }
        IPerson Owner { get; set; }
    }
}

using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.PerformanceTests.Model
{
    [Entity]
    public interface IWebsite
    {
        [Identifier("http://www.examplevocab.com/schema/website/")]
        string Id { get; }
        string Name { get; set; }
        string Url { get; set; }

        [InverseProperty("Website")]
        ICollection<IArticle> Articles { get; }
    }
}

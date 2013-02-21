using System;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.PerformanceTests.Model
{
    [Entity]
    public interface IArticle
    {
        [Identifier("http://www.examplevocab.com/schema/article/")]
        string Id { get; }

        string Title { get; set; }
        string BodyText { get; set; }
        DateTime? PublishDate { get; set; }

        IPerson Publisher { get; set; } 
        IWebsite Website { get; set; } 
    }
}

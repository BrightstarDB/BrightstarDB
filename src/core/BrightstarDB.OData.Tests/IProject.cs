using System;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.OData.Tests
{
    [Entity]
    public interface IProject
    {
        [Identifier("http://example.org/projects/")]
        string Id { get; }

        string Title { get; set; }

        string Summary { get; set; }

        Uri Website { get; set; }

        int ProjectCode { get; set; }

        DateTime? StartDate { get; set; }
    }
}

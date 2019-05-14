using System;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity]
    public interface ITrackable
    {
        DateTime Created { get; set; }
        DateTime LastModified { get; set; }
    }
}
using System;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Portable.Tests.EntityFramework.Model
{
    [Entity]
    public interface ITrackable
    {
        DateTime Created { get; set; }
        DateTime LastModified { get; set; }
    }
}
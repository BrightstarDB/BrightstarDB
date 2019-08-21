using System;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Samples.EntityFramework.ChangeTracking
{
    [Entity]
    interface ITrackable
    {
        DateTime Created { get; set; }
        DateTime LastModified { get; set; }
    }
}

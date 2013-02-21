using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB.ClusterManager
{
    public enum ClusterStatus
    {
        Unavailable = 0,
        ReadOnly = 1,
        Available = 2
    }
}

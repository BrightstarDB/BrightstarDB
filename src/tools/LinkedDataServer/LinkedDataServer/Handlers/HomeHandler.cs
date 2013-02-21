using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NetworkedPlanet.Brightstar.LinkedDataServer.Resources;

namespace NetworkedPlanet.Brightstar.LinkedDataServer.Handlers
{
    public class HomeHandler
    {
        public object Get()
        {
            return new Home {Title = "Welcome Home."};
        }
    }
}
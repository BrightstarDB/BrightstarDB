using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nancy.ViewEngines.Razor;

namespace BrightstarDB.Server.Modules
{
    public class RazorConfig : IRazorConfiguration
    {
        public IEnumerable<string> GetAssemblyNames()
        {
            yield return "BrightstarDB";
        }

        public IEnumerable<string> GetDefaultNamespaces()
        {
            yield return "BrightstarDB";
            yield return "BrightstarDB.Dto";
        }

        public bool AutoIncludeModelNamespace
        {
            get { return true; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using BrightstarDB.EntityFramework;

namespace CodeGenerationDev
{
    [Entity("http://example.org/person", LaunchDebuggerDuringBuild = false)]
    public interface IPerson
    {
        //public string Name { get; set; }
    }
}

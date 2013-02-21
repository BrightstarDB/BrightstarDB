using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB.Azure.AdminCommandLine
{
    interface ICommand
    {
        string Name { get; }
        void Execute(params string[] args);
    }
}

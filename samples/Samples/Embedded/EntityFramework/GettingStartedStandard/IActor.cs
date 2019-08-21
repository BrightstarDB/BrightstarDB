using System;
using System.Collections.Generic;
using BrightstarDB.EntityFramework;
using BrightstarDB.CodeGeneration;

namespace BrightstarDB.Samples.EntityFramework.GettingStartedStandard
{
    [Entity]
    public interface IActor
    {
        string Id { get; }
        string Name { get; set; }
        DateTime DateOfBirth { get; set; }  
        ICollection<IFilm> Films { get; set; }
    }
}

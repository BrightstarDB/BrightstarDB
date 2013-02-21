using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Mobile.Tests.EntityFramework
{
    [Entity]
    public interface INote
    {
        string Title { get; set; }
        string Body { get; set; }
        ICollection<INote> LinkedNotes { get; set; } 
    }
}

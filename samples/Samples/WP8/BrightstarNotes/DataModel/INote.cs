using System;
using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarNotes.DataModel
{
    [Entity]
    public interface INote
    {
        string Id { get; }
        string Title { get; set; }
        string Body { get; set; }
        DateTime Modified { get; set; }
        ICollection<INote> LinkedNotes { get; set; }
        INoteCategory Category { get; set; }
    }
}

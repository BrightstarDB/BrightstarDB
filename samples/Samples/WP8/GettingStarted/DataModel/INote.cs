using System;
using System.Net;
using BrightstarDB.EntityFramework;

namespace GettingStarted.DataModel
{
    [Entity]
    public interface INote
    {
        string NoteId { get; }
        string Label { get; set; }
        string Content { get; set; }
        ICategory Category { get; set; }
    }
}

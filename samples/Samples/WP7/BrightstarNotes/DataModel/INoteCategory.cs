using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarNotes.DataModel
{
    [Entity]
    public interface INoteCategory
    {
        string Id { get; }
        string Title { get; set; }

        [InverseProperty("Category")]
        ICollection<INote> Notes { get; set; } 
    }
}

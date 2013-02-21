using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.EntityFramework;

namespace GettingStarted.DataModel
{
    [Entity]
    public interface ICategory
    {
        /// <summary>
        /// Returns the URI identity for this category
        /// </summary>
        string CategoryId { get; }

        /// <summary>
        /// Get or set the label for the category
        /// </summary>
        string Label { get; set; }
    }
}

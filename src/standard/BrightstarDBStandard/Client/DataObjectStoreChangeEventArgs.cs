using System;
using System.Collections.Generic;
using BrightstarDB.Model;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Class for event arguments provided by the <see cref="IDataObjectStore.SavingChanges"/> event.
    /// </summary>
    public class DataObjectStoreChangeEventArgs : EventArgs
    {
        internal DataObjectStoreChangeEventArgs(List<ITriple> addTriples, List<ITriple> deletePatterns,
            List<ITriple> existencePreconditions, List<ITriple> nonexistencePreconditions)
        {
            AddTriples = addTriples;
            DeletePatterns = deletePatterns;
            ExistencePreconditions = existencePreconditions;
            NonexistencePreconditions = nonexistencePreconditions;
        }

        /// <summary>
        /// The list of triples that will be added to the store
        /// </summary>
        public List<ITriple> AddTriples { get; private set; }
        /// <summary>
        /// The list of triple patterns that will be removed from the store
        /// </summary>
        public List<ITriple> DeletePatterns { get; private set; }
        /// <summary>
        /// The list of triple patterns that will be checked for existence prior to executing the update transaction
        /// </summary>
        public List<ITriple> ExistencePreconditions { get; private set; }
        /// <summary>
        /// The list of triple patterns that will be checked for non-existence prior to executing the update transaction
        /// </summary>
        public List<ITriple> NonexistencePreconditions { get; private set; }
    }
}
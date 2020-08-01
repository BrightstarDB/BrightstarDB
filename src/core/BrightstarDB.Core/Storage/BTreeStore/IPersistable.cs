namespace BrightstarDB.Storage.BTreeStore
{
    /// <summary>
    /// All objects that have unique identity within the store must implement this interface.
    /// </summary>
    internal interface IPersistable : IStorable
    {
        /// <summary>
        /// All objects have a unique id assigned from the store.
        /// </summary>
        ulong ObjectId { get; set; }

        /// <summary>
        /// Indicates if the object has been added to the commitlist
        /// </summary>
        bool ScheduledForCommit { get; set; }

        /// <summary>
        /// All objects need to know which store they belong to.
        /// </summary>
        IStore Store { get; set; }
    }
}

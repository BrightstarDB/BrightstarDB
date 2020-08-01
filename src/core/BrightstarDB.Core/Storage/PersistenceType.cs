namespace BrightstarDB.Storage
{
    /// <summary>
    /// Enumeration of the different persistant storage mechanisms supported by B*
    /// </summary>
    public enum PersistenceType
    {
        /// <summary>
        /// Persist data in an append-only data file.
        /// </summary>
        AppendOnly = 0,
        /// <summary>
        /// Persist data in a rewriteable page format
        /// </summary>
        Rewrite = 1
    }
}
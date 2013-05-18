namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// An enumeration of values that specify whether property changes
    /// made to a collection of persistable objects are kept or replaced
    /// with property values from the data source
    /// </summary>
    public enum RefreshMode
    {
        /// <summary>
        /// Property changes made to objects are not replaced with values
        /// from the data source, and will overwrite the data source values
        /// on the next save.
        /// </summary>
        ClientWins = 0,
        /// <summary>
        /// Property changes made to objects are replaced with values from the data source
        /// </summary>
        StoreWins = 1
    }
}

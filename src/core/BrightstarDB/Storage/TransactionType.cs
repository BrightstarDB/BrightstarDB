namespace BrightstarDB.Storage
{
    /// <summary>
    /// An enumeration over the types of transactions that are processed by a BrightstarDB server
    /// </summary>
    public enum TransactionType
    {
        /* NOTE: DO NOT CHANGE THE INT VALUES -- these are the codes that get stored in the transactions data file */

        /// <summary>
        /// Type for a bulk-import transaction
        /// </summary>
        ImportJob = 0,

        /// <summary>
        /// Type for a triple update transaction
        /// </summary>
        UpdateTransaction = 1,

        /// <summary>
        /// Type for a SPARQL update transaction
        /// </summary>
        SparqlUpdateTransaction = 2
    }
}
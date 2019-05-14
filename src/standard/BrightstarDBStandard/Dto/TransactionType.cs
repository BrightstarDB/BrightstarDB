namespace BrightstarDB.Dto
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
        /// Type for a triple update transaction -- OBSOLETE, replaced with GuardedUpdateTransaction
        /// </summary>
        UpdateTransaction = 1,

        /// <summary>
        /// Type for a SPARQL update transaction
        /// </summary>
        SparqlUpdateTransaction = 2,

        /// <summary>
        /// Type for a triple update transaction with support for non-existance guards
        /// </summary>
        GuardedUpdateTransaction = 3,
    }
}
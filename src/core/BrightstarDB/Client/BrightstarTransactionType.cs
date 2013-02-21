namespace BrightstarDB.Client
{
    /// <summary>
    /// An enumeration over the types of transactions that are processed by a BrightstarDB server
    /// </summary>
    public enum BrightstarTransactionType : int
    {
        /// <summary>
        /// Type for a bulk-import transaction
        /// </summary>
        ImportJob = 0,

        /// <summary>
        /// Type for a triple update transaction
        /// </summary>
        UpdateTransaction = 1,
    }

    /// <summary>
    /// An enumeration over the status codes for completed transactions
    /// </summary>
    public enum BrightstarTransactionStatus : int
    {
        /// <summary>
        /// The transaction completed successfully
        /// </summary>
        CompletedOk = 0,

        /// <summary>
        /// The transaction failed
        /// </summary>
        Failed = 1,
    }
}

namespace BrightstarDB.Storage
{
    /// <summary>
    /// An enumeration over the status codes for completed transactions
    /// </summary>
    public enum TransactionStatus
    {

        /* NOTE: DO NOT CHANGE THE INT VALUES -- these are the codes that get stored in the transactions data file */

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
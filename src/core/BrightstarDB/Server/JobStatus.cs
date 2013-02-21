namespace BrightstarDB.Server
{
    internal enum JobStatus
    {
        Pending,
        Started,
        CompletedOk,
        TransactionError,
        NotRegistered,
        Unknown
    }
}

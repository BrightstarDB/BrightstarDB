namespace BrightstarDB.Dto
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

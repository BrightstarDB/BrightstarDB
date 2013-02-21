namespace BrightstarDB.Azure.Common
{
    public enum JobStatus
    {
        Pending = 0,
        Started = 1,
        Committing = 2,
        CompletedOk = 98,
        CompletedWithErrors = 99
    }
}
using System;

namespace BrightstarDB.Client
{
    public class TransactionInfo : ITransactionInfo
    {
        public string StoreName {get; internal set; }

        public ulong Id { get; internal set; }

        public BrightstarTransactionType TransactionType { get; internal set; }

        public Guid JobId { get; internal set; }

        public DateTime StartTime { get; internal set; }

        public BrightstarTransactionStatus Status { get; internal set; }
    }
}

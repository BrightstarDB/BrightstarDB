using System;
using BrightstarDB.Client;

namespace BrightstarDB.Dto
{
    /// <summary>
    /// Data transfer object for transaction information
    /// </summary>
    internal class TransactionInfoObject : ITransactionInfo
    {
        public string StoreName { get; set; }
        public ulong Id { get; set; }
        public TransactionType TransactionType { get; set; }
        public TransactionStatus Status { get; set; }
        public Guid JobId { get; set; }
        public DateTime StartTime { get; set; }
    }
}

using System;
using BrightstarDB.Dto;

namespace BrightstarDB.Storage
{
    internal interface ITransactionInfo
    {
        int VersionNumber { get; set; }
        Guid JobId { get; set; }
        TransactionStatus TransactionStatus { get; set; }
        TransactionType TransactionType { get; set; }
        ulong DataStartPosition { get; set; }
        ulong DataLength { get; set; }
        DateTime TransactionStartTime { get; set; }
    }
}
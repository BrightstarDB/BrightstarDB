using System;

namespace BrightstarDB.Server.Modules
{
    [Flags]
    public enum StorePermissions
    {
        None = 0x0,
        
        Read = 0x01, // Required for SPARQL query access
        Export = 0x02, // Required to post an Export job
        ViewHistory=0x04, // Required to view commit and transaction history
        SparqlUpdate = 0x10, // Required for SPARQL UPDATE access
        TransactionUpdate = 0x20, // Required to post a transaction
        Admin = 0x4000, // Required to re-execute transactions
        WithGrant = 0x8000, // Required to assign permissions to someone else

        All = Read|Export|ViewHistory|SparqlUpdate|TransactionUpdate|Admin|WithGrant
    }
}
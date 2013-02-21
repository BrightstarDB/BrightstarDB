namespace BrightstarDB.Azure.Common
{
    public enum JobType
    {
        Transaction = 0,
        Export = 1,
        Import = 2,
        Consolidate = 3,
        SparqlUpdate = 4,
        DeleteStore =5
    }
}
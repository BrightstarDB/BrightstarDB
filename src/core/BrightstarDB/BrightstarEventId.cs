namespace BrightstarDB
{
    internal enum BrightstarEventId
    {
        Undefined = 0,

        // Datafile errors 1 - 50
        ObjectWriteError = 1, // Indicates that the store manager couldn't persist the modified objects to disk
        ObjectReadError = 2, // Inidicates that the store manager couldn't read an object from disk
        CommitPointReadError = 5, // Inidcates an error in reading a commit point
        StreamCloseError = 10, // Indicates an error closing the input stream for a store
        BlockProviderError = 20, // Indicates an error with block-based file operations

        // Store errors 51-99
        StoreCommitException = 51, // Indicates an error committing and update to the store
        StoreFlushException = 52, // Indicates an error flushing changes to the store
        StoreBackgroundWriteError = 53, // Indicates an error in the background page writing thread

        // Server errors 100 - 199
        ServerCoreException = 100, // Indicates an error in processing a request 
        InvalidOperationException = 101, // Indicates an attempt to perform an operation that is not supported by the store
        CacheError = 101, // Indicates an error reading/writing a cache

        // Server-side SPARQL engine errors 200 - 299
        SparqlExecutionError = 200, // Indicates an error in processing a SPARQL query

        // Client-side data objects errors 300 - 350
        ClientDataBindError = 300, // Indicates an error in binding the results of a SPARQL query to an object

        // Client-side Transport errors
        TransportError = 351,

        // Server-side processing errors 400 - 499
        JobProcessingError = 400, // Indicates a general error in processing a server job
        TransactionClientError = 410, // Indicates that an update transaction failed due to client issues (e.g. bad data)
        TransactionServerError = 411, // Indicates taht an update transaction failed due to server issues (internal errors)
        ExportDataError = 412, // Indicates an error while exporting data from a store
        ImportDataError = 413,
        ParserWarning = 414, // A non-fatal parser issue
        CachingError = 420,

        // ServerRunner errors 500 - 599
        ServiceStartupFailed = 500, // Indicates the OnStart method threw an exception
        AddressAccessDenied = 501, // Indicates that the user running the service does not have permission to register the service endpoints
        ServiceHostStartupFailed = 502, // Indicates the StartService() method in the console app threw an exception
        NoValidLicense = 503,  // Indicates that a valid activated license could not be found

        // Warnings

        UndefinedWarning = 10000,
        // Store warnings 10001 - 10500
        StorePerformanceWarning = 10001,
    }
}
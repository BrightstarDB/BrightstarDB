namespace BrightstarDB.Azure.Common
{
    /// <summary>
    /// String constants for azure role configuration and operation
    /// </summary>
    public static class AzureConstants
    {
        public const string StoreContainerPrefix = "bs-";

        // Aure role names
        public const string StoreWorkerRoleName = "BrightstarDB.Azure.StoreWorker";
        public const string GatewayRoleName = "BrightstarDB.Azure.Gateway";

        // service endpoint names
        public const string BlockServiceEndpoint = "BlockUpdateService";

        // Blob store properties
        public const string BlobDataLengthPropertyName = "DataLength";
        public const long StoreBlobSize = 1073741824000; // 1000 GB

        // Block cache properties
        public const string BlockStoreLocalStorageName = "BlockStoreCache";
        public const string BlockStoreConnectionStringName = "BlockStoreConnectionString";
        public const string BlockStoreMemoryCacheSizeName = "BlockStoreMemoryCacheMB";

        // Role Diagnostics Properties
        public const string ScheduledTransferPeriodPropertyName = "ScheduledTransferPeriod";
        public const string PerformanceCountersPropertyName = "PerformanceCounters";
        public const string SampleRatePropertyName = "PerformanceCounters.SampleRate";
        public const string DiagnosticsConnectionStringName =
            "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";

        // Job queue properties
        public const string ManagementDatabaseConnectionStringName = "ManagementDatabaseConnectionString";

        // Admin access properties
        public const string SuperUserAccountPropertyName = "SuperUserAccount";
        public const string SuperUserKeyPropertyName = "SuperUserKey";

        // Log filter for the custom AzureEventLogs category
        public const string AzureEventLogFilter = "Application!*[System[Provider[@Name='AzureEventLogs']]]";
        public const string AllApplicationEvents = "Application!*";
        public const string AllSystemEvents = "System!*";

        // Separator between preconditions, inserts and deletes in the update transaction data
        public const string TransactionSeparator = "\n.\n";
    }
}

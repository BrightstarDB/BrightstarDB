using System;
using System.Collections.Generic;
using System.Text;

namespace BrightstarDB
{
    public static class Strings
    {

        public static readonly string InvalidDataObjectIdentity = "Identity is not a valid URI nor CURIE with regards to the current namespace mappings.";
        public static readonly string InvalidEntityType = "The value provided must be an instance of the type {0} or of a type derived from it.";
        public static readonly string BrightstarServiceClient_QueryMustNotBeNull = "Query expression must not be NULL.";
        public static readonly string BrightstarServiceClient_QueryMustNotBeEmptyString = "Query expression must not be an empty string.";
        public static readonly string BrightstarServiceClient_StoreNameMustNotBeNull = "Store name must not be NULL.";
        public static readonly string BrightstarServiceClient_StoreNameMustNotBeEmptyString = "Store name must not be an empty string.";
        public static readonly string BrightstarServiceClient_JobIdMustNotBeNull = "Job Id must not be NULL.";
        public static readonly string BrightstarServiceClient_JobIdMustNotBeEmptyString = "Job Id must not be an empty string.";
        public static readonly string BrightstarServiceClient_ImportFileNameMustNotBeNull = "Import file name must not be NULL.";
        public static readonly string BrightstarServiceClient_ImportFileNameMustNotBeEmptyString = "Import file name must not be an empty string.";
        public static readonly string BrightstarServiceClient_GetCommitPoints_TakeToLarge = "Requested number of commit points exceeds maximum value of 100.";
        public static readonly string BrightstarServiceClient_SkipMustNotBeNegative = "Skip value must be a non-negative integer.";
        public static readonly string BrightstarServiceClient_CommitPointMustNotBeNull = "Commit point must not be NULL.";
        public static readonly string BrightstarServiceClient_InvalidCommitPointInfoObject = "Invalid commit point info object";
        public static readonly string BrightstarServiceClient_InvalidTransactionInfoObject = "Invalid transaction info object";
        public static readonly string BrightstarServiceClient_ExportFileNameMustNotBeEmptyString = "Export file name must not be an empty string.";
        public static readonly string BrightstarServiceClient_ExportFileNameMustNotBeNull = "Export file name must not be NULL.";
        public static readonly string BrightstarServiceClient_InvalidStoreName = "Invalid store name. Store name may only include letters, digits or the following punctuation characters: -_.+,()";
        public static readonly string BrightstarServiceClient_NoConnectionStringConfiguration = "Could not find the connection string configuration information. Please check that the application configuration file contains the required appSettings elements.";
        public static readonly string BrightstarServiceClient_QueryDefaultGraphUriMustNotBeNull = "Default graph URI must not be NULL.";
        public static readonly string INode_Attempt_to_write_to_a_fixed_page = "Attempt to write to a fixed page.";
        public static readonly string BrightstarServiceClient_GetStatistics_TakeTooLarge = "Requested number of statistics records exceeds the maximum value of 100.";
        public static readonly string BrightstarServiceClient_StoreNameConflict = "Store name conflicts with the name of an existing store.";
        public static readonly string StringParameterMustBeNonEmpty = "Parameter must be a non-empty, non-null string.";
        public static readonly string NotAnHttpRequest = "Request does not use HTTP(S).";
        public static readonly string BrightstarServiceClient_TakeMustBeGreaterThanZero = "Requestd page size must be greater than zero.";
        public static readonly string BrightstarServiceClient_UnexpectedResponseContent = "The server provided unexpected content in response to the service request.";
        public static readonly string BrightstarServiceClient_GetJobInfo_TakeToLarge = "The requested number of job records exceeds the maximum of 100.";
        public static readonly string BrightstarServiceClient_GetTransactions_TakeTooLarge = "The requested number of transaction records exceeds the maximum of 100.";
        public static readonly string BrightstarServiceClient_InvalidDateRange = "Invalid date range. Ensure that the latest date is later than or the same as the earliest date.";
        public static readonly string BrightstarServiceClient_UpdateExpressionMustNotBeEmptyString = "The SPARQL Update expression must not be null or an empty string.";
        public static readonly string BrightstarConnectionString_MustMotBeNull = "Connection string must not be NULL.";
        public static readonly string BrightstarConnectionString_MustNotBeEmpty = "Connection string must not be an empty string.";
        public static readonly string BrightstarConnectionString_ObsoleteType = "The connection type '{0}' is unsupported from BrightstarDB 1.5 onwards.";
        public static readonly string EntityFramework_InvalidEntityType = "The argument provided was not of or assignable to the expected type '{0}'";
        public static readonly string EntityFramework_InvalidPropertyMessage = "The property '{0}' of type '{1}' cannot be used as an identity property. An identity property must be a read-only System.String property. If this property is intended to be the identity property for the entity, please change it to a read-only System.String property. If it is not intended to be the identity property for the entity, please add the [ResourceAddress] attribute to the property which is intended to be the identity property for the entity.";
        public static readonly string BrightstarServiceClient_StoreDoesNotExist = "The store '{0}' does not exist or cannot be accessed.";
        public static readonly string DotNetRdf_ErrorFromUnderlyingServer = "An exception was raised by the DotNetRDF storage server. See Inner Exception for details.";
        public static readonly string DotNetRdf_NotAStorageProvider = "The configured item must be a DotNetRDF IStorageProvider instance.";
        public static readonly string DotNetRdf_StoreCreationFailed = "Store creation failed. No further information is available. Please check your storage server configuration and permissions.";
        public static readonly string DotNetRdf_StoreDoesNotSupportDelete = "The storage provider for this store does not provide the methods required to clear the store data.";
        public static readonly string DotNetRdf_StoreMustImplementIQueryableStorage = "The storage provider for this store does not support SPARQL query.";
        public static readonly string DotNetRdf_UnsupportedByServer = "The operation is not supported by the DotNetRDF storage server.";
        public static readonly string BrightstarServiceClient_StoreIsReadOnly = "Cannot save changes to a read-only store.";
        public static readonly string ExportJob_UnsupportedExportFormat = "The media type {0} is not currently supported by Export jobs.";
        public static readonly string PreconditionFailedBasicMessage = "Transaction preconditions failed.";
        public static readonly string PreconditionFailedFullMessage = "Transaction preconditions failed. {0} existence preconditions failed. {1} non-existence preconditions failed.";
        public static readonly string EntityFramework_EntityKeyChanged = "The entity's key properties have been modified without updating the identity value.";
        public static readonly string EntityFramework_KeyRequired = "The key properties for the entity must be set before adding then entity to a context.";
        public static readonly string EntityFramework_UniqueConstraintViolation = "One or more new resources have an identifier that conflicts with an existing resource of the same type.";
        public static readonly string Persistence_InvalidFileMode = "Invalid file mode.";
    }
}

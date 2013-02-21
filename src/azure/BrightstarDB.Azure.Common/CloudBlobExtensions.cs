using System.Diagnostics;
using Microsoft.WindowsAzure.StorageClient;

namespace BrightstarDB.Azure.Common
{
    public static class CloudBlobExtensions
    {
        /// <summary>
        /// Returns true of the specified container exists, false otherwise
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        public static bool Exists(this CloudBlobContainer container)
        {
            try
            {
                container.FetchAttributes();
                return true;
            }
            catch (StorageClientException e)
            {
                if (e.ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    return false;
                }
                throw;
            }
        }

        /// <summary>
        /// Returns true if the blob exists, false otherwise
        /// </summary>
        /// <param name="blob"></param>
        /// <returns></returns>
        public static bool Exists(this CloudPageBlob blob)
        {
            try
            {
                blob.FetchAttributes();
                return true;
            }
            catch (StorageClientException e)
            {
                if (e.ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    Trace.TraceWarning("CloudPageBlob {0} was not found due to StorageClientException {1}", blob.Uri, e);
                    return false;
                }
                Trace.TraceError("Unexecpted exception checking existence of CloudPageBlob {0} : {1}", blob.Uri, e);
                throw;
            }
        }
    }
}

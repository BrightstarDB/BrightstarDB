using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using BrightstarDB.Client;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace BrightstarDB.Azure.StoreWorker
{
    public static class BlobImportSourceExtensions
    {
        /// <summary>
        /// Returns a stream for writing to the source
        /// </summary>
        /// <returns></returns>
        public static Stream OpenWrite(this BlobImportSource source)
        {
            if (string.IsNullOrEmpty(source.ConnectionString))
            {
                var client = new WebClient();
                var rawStream = client.OpenWrite(source.BlobUri);
                return source.IsGZiped ? new GZipStream(rawStream, CompressionMode.Compress) : rawStream;
            }
            else
            {
                CloudStorageAccount storageAccount;
                if (!CloudStorageAccount.TryParse(source.ConnectionString, out storageAccount))
                {
                    throw new Exception("Invalid blob storage connection string");
                }
                var blobClient = storageAccount.CreateCloudBlobClient();
                var blob = blobClient.GetBlobReference(source.BlobUri);
                var rawStream = (Stream)blob.OpenWrite();
                return source.IsGZiped ? new GZipStream(rawStream, CompressionMode.Compress) : rawStream;
            }
        }

        /// <summary>
        /// Returns a stream for reading from the source
        /// </summary>
        /// <returns></returns>
        public static Stream OpenRead(this BlobImportSource source)
        {
            if (String.IsNullOrEmpty(source.ConnectionString))
            {
                Uri importUri;
                if (Uri.TryCreate(source.BlobUri, UriKind.Absolute, out importUri))
                {
                    var client = new WebClient();
                    var rawStream = client.OpenRead(importUri);
                    return source.IsGZiped ? new GZipStream(rawStream, CompressionMode.Decompress) : rawStream;
                }
                throw new Exception("Invalid BlobUri specified for import.");
            }
            CloudStorageAccount storageAccount;
            if (CloudStorageAccount.TryParse(source.ConnectionString, out storageAccount))
            {
                var blobClient = storageAccount.CreateCloudBlobClient();
                var cloudBlob = blobClient.GetBlobReference(source.BlobUri);
                var rawStream = cloudBlob.OpenRead() as Stream;
                return source.IsGZiped ? new GZipStream(rawStream, CompressionMode.Decompress) : rawStream;
            }
            throw new Exception("Invalid ConnectionString specified for import.");
        }
    }
}

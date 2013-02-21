using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using NetworkedPlanet.Brightstar.Azure.Common;
using NetworkedPlanet.Brightstar.Azure.StoreWorker;

namespace NetworkedPlanet.Brightstar.Azure.StorageTests
{
    [TestClass]
    public class AzureBlockStoreTests
    {
        private CloudStorageAccount _storageAccount;
        public AzureBlockStoreTests()
        {
            SetUp();
        }
        public void SetUp()
        {
            var configuration = new AzureBlockStoreConfiguration
            {
                ConnectionString = "UseDevelopmentStorage=true",
                MemoryCacheInMB = 1024,
                Disconnected = true
            };
            AzureBlockStore.Initialize(configuration);
            _storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
        }

        private void CreateTestBlob(string name)
        {
            var client = _storageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference("test");
            container.CreateIfNotExist();
            var blobRef = container.GetPageBlobReference(name);
            blobRef.Create(AzureConstants.StoreBlobSize);
            blobRef.Metadata[AzureConstants.BlobDataLengthPropertyName] = "0";
            blobRef.SetMetadata();
        }

        [TestMethod]
        public void TestIncrementalWrite()
        {
            var testBlobName = Guid.NewGuid().ToString();
            CreateTestBlob(testBlobName);
            var testPattern = new byte[255];
            for (int i = 0; i < 255; i++) testPattern[i] = (byte)i;
            var testBuffer = new byte[255000];
            var readBuffer = new byte[255000];
            for (int i = 0; i < 1000; i++)
            {
                testPattern.CopyTo(testBuffer, 255*i);
            }
            for (int i = 0; i < 100; i++)
            {
                using (
                    var stream = new Storage.BlockProviderStream(AzureBlockStore.Instance, "test\\" + testBlobName,
                                                                 FileMode.Append))
                {
                    stream.Write(testBuffer, 0, 255000);
                }
                using(var stream = new Storage.BlockProviderStream(AzureBlockStore.Instance, "test\\" + testBlobName, FileMode.Open))
                {
                    Assert.AreEqual(255000 * (i+1), stream.Length);
                    stream.Seek(-255000, SeekOrigin.End);
                    int readCount = stream.Read(readBuffer, 0, 255000);
                    Assert.AreEqual(255000, readCount);
                    for (int x = 0; x < 1000; x++)
                    {
                        for (int y = 0; y < 255; y++)
                        {
                            Assert.AreEqual(y, readBuffer[(255 * x) + y]);
                        }
                    }
                }
            }
            for(int i = 0; i < 100; i++)
            {
                using(var stream = new Storage.BlockProviderStream(AzureBlockStore.Instance, "test\\"+testBlobName, FileMode.Open))
                {
                    stream.Seek(255000*i, SeekOrigin.Begin);
                    int readCount = stream.Read(readBuffer, 0, 255000);
                    Assert.AreEqual(255000, readCount);
                }
                for(int x =0;x<1000;x++)
                {
                    for(int y = 0; y < 255; y++ )
                    {
                        Assert.AreEqual(y, readBuffer[(255*x)+y]);
                    }
                }
            }
        }
    }
}

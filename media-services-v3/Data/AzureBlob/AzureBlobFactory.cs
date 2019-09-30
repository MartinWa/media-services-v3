using System;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace media_services_v3.Data.AzureBlob
{
    public class AzureBlobFactory
    {
        private readonly Lazy<CloudBlobClient> _client;

        public AzureBlobFactory(string storageConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            _client = new Lazy<CloudBlobClient>(() => storageAccount.CreateCloudBlobClient());
        }

        public CloudBlobClient GetCloudBlobClient()
        {
            return _client.Value;
        }
    }
}
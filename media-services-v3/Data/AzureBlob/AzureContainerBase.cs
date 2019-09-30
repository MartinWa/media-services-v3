using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace media_services_v3.Data.AzureBlob
{
    public abstract class AzureContainerBase
    {
        private readonly CloudBlobClient _client;
        private readonly string _containername;
        private readonly ILogger _logger;

        protected AzureContainerBase(CloudBlobClient client, string containerName, ILogger logger)
        {
            _client = client;
            _containername = containerName;
            _logger = logger;
        }

        private CloudBlobContainer _container;

        protected CloudBlobContainer Container
        {
            get
            {
                if (_container != null)
                {
                    return _container;
                }
                _container = _client.GetContainerReference(_containername);
                //if (_container.Exists()) // Save calls, assume that it exists
                //{
                //    return _container;
                //}
                //_container.Create();
                //_container.SetPermissions(new BlobContainerPermissions
                //{
                //    PublicAccess = BlobContainerPublicAccessType.Off
                //});
                return _container;
            }
        }

        protected string CreateSafeFilename(string filename)
        {
            if (filename.Length < 1)
            {
                _logger.LogError("Filename is to short");
                filename = "none";
            }
            if (filename.Length > 1024) // Blob names must be from 1 to 1024 characters long
            {
                _logger.LogError($"Filename >{filename}< is to long");
                filename = filename.Substring(0, 1000);
            }
            return filename;
        }
    }
}
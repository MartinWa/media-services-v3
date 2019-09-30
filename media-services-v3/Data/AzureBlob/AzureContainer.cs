using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace media_services_v3.Data.AzureBlob
{
    public class AzureContainer : AzureContainerBase, IZeroContainer
    {
        public AzureContainer(CloudBlobClient client, string containerName, ILogger logger) : base(client, containerName, logger) { }

        public IZeroBlob GetBlob(string filename)
        {
            return new AzureBlob(Container, CreateSafeFilename(filename));
        }
    }
}
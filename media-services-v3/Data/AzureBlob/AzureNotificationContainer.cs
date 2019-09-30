using media_services_v3.Common;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace media_services_v3.Data.AzureBlob
{
    public class AzureNotificationContainer : AzureContainerBase, IZeroNotificationContainer
    {
        public AzureNotificationContainer(CloudBlobClient client, ILogger logger) : base(client, Constants.AzureBlobNotificationContainer, logger) { }

        public IZeroBlob GetNotificationBlob(string filename)
        {
            return new AzureBlob(Container, $"{CreateSafeFilename(filename)}");
        }
    }
}
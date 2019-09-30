using media_services_v3.Common;
using media_services_v3.Common.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace media_services_v3.Data.AzureBlob
{
    public class AzureSharedContainer : AzureContainerBase, IZeroSharedContainer
    {
        public AzureSharedContainer(CloudBlobClient client, ILogger logger) : base(client, Constants.AzureBlobSharedContainerName, logger) { }

        public IZeroBlob GetBlob(BlobFileType type, string filename)
        {
            return new AzureBlob(Container, $"{type.ToString().ToLower()}/{CreateSafeFilename(filename)}");
        }
    }
}
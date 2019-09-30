using media_services_v3.Common;
using media_services_v3.Common.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace media_services_v3.Data.AzureBlob
{
    public class AzurePortalContainer : AzureContainerBase, IZeroPortalContainer
    {
        private readonly int _portalId;
        public AzurePortalContainer(CloudBlobClient client, int portalId, ILogger logger) : base(client, string.Format(Constants.AzureBlobPortalContainer), logger) 
        {
            _portalId = portalId;
        }

        public IZeroBlob GetPortalBlob(BlobFileType type, string filename)
        {
            return new AzureBlob(Container, $"{_portalId}/{type.ToString().ToLower()}/{CreateSafeFilename(filename)}");
        }
    }
}
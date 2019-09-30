using media_services_v3.Common;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace media_services_v3.Data.AzureBlob
{
    public class AzureContentContainer : AzureContainerBase, IZeroContentContainer
    {
        private readonly int _contentId;
        public AzureContentContainer(CloudBlobClient client, int contentId, ILogger logger) : base(client, Constants.AzureBlobContentContainer, logger)
        {
            _contentId = contentId;
        }

        public IZeroBlob GetContentBlob(string filename)
        {
            return new AzureBlob(Container, $"{_contentId}/{CreateSafeFilename(filename)}");
        }


        public async Task<IEnumerable<IZeroBlob>> GetAllContentBlobs()
        {
            var directory = Container.GetDirectoryReference(_contentId.ToString());

            var blobs = new List<AzureBlob>();
            BlobContinuationToken continuationtoken = null;
            do
            {
                var segment = await directory.ListBlobsSegmentedAsync(continuationtoken);
                continuationtoken = segment.ContinuationToken;

                blobs.AddRange(segment.Results.OfType<CloudBlockBlob>().Select(bb => new AzureBlob(Container, bb.Name)));
            } while (continuationtoken != null);

            return blobs;
        }
    }
}
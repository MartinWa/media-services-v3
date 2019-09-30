using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace media_services_v3.Data.AzureBlob
{
    public class AzureStorage : IAzureStorage
    {
        private readonly ILogger _logger;
        private readonly Lazy<CloudBlobClient> _client;

        public AzureStorage(ILogger logger, AzureBlobFactory factory)
        {
            _logger = logger;
            _client = new Lazy<CloudBlobClient>(factory.GetCloudBlobClient);
        }


        public IZeroPortalContainer GetPortalContainer(int portalId)
        {
            return new AzurePortalContainer(_client.Value, portalId, _logger);
        }

        public IZeroSharedContainer GetSharedContainer()
        {
            return new AzureSharedContainer(_client.Value, _logger);
        }

        public IZeroContainer GetContainer(string containerName)
        {
            var regex = new Regex("^(?-i)(?:[a-z0-9]|(?<=[0-9a-z])-(?=[0-9a-z])){3,63}$", RegexOptions.Compiled);
            if (!regex.IsMatch(containerName))
            {
                throw new ArgumentException("Container names must conform to these rules: " +
                                            "Must start with a letter or number, and can contain only letters, numbers, and the dash (-) character. " +
                                            "Every dash (-) character must be immediately preceded and followed by a letter or number; consecutive dashes are not permitted in container names. " +
                                            "All letters in a container name must be lowercase. " +
                                            "Must be from 3 to 63 characters long.");
            }
            return new AzureContainer(_client.Value, containerName, _logger);
        }

        public string BaseUri()
        {
            return _client.Value.BaseUri.AbsoluteUri;
        }

        public IZeroContentContainer GetContentContainer(int contentId)
        {
            return new AzureContentContainer(_client.Value, contentId, _logger);
        }

        public IZeroNotificationContainer GetNotificationContainer()
        {
            return new AzureNotificationContainer(_client.Value, _logger);
        }
    }
}
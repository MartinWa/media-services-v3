using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using media_services_v3.Common;

namespace media_services_v3.Data.AzureBlob
{
    public class AzureBlob : IZeroBlob
    {
        private readonly CloudBlobContainer _container;
        private readonly string _filename;
        private bool _isAttributesFetched;
        private CloudBlockBlob _blob;
        private bool _hasExistBeenChecked;

        private CloudBlockBlob Blob
        {
            get
            {
                if (_blob != null)
                {
                    return _blob;
                }
                _blob = _container.GetBlockBlobReference(_filename);
                return _blob;
            }
        }

        public AzureBlob(CloudBlobContainer container, string filename)
        {
            _container = container;
            _filename = filename;
            _isAttributesFetched = false;
            _hasExistBeenChecked = false;
        }

        private async Task FetchAttributes()
        {
            if (_isAttributesFetched)
            {
                return;
            }
            await Blob.FetchAttributesAsync();
            _isAttributesFetched = true;
        }

        private async Task SetPropertiesAsync()
        {
            _isAttributesFetched = false;
            await Blob.SetPropertiesAsync();
        }

        public async Task<bool> ExistsAsync()
        {
            if (_hasExistBeenChecked)
            {
                return true;
            }
            try
            {
                _hasExistBeenChecked = await Blob.ExistsAsync();
            }
            catch (Exception)
            {
                return false;
            }
            return _hasExistBeenChecked;
        }

        public async Task<string> GetEtagAsync()
        {
            var exists = await ExistsAsync();
            if (!exists)
            {
                return "";
            }
            await FetchAttributes();
            return Blob.Properties.ETag;
        }

        private async Task<string> GetContentTypeAsync()
        {
            var exists = await ExistsAsync();
            if (!exists)
            {
                return "";
            }
            await FetchAttributes();
            return Blob.Properties.ContentType;
        }

        private async Task SetContentTypeAsync(string type)
        {
            var exists = await ExistsAsync();
            if (!exists)
            {
                return;
            }
            Blob.Properties.ContentType = type;
            await SetPropertiesAsync();
        }

        public async Task<long> GetSizeAsync()
        {
            var exists = await ExistsAsync();
            if (!exists)
            {
                return -1;
            }
            await FetchAttributes();
            return Blob.Properties.Length;
        }

        public string GetName()
        {
            var indexOfSlash = _filename.LastIndexOf('/');
            return _filename.Substring(indexOfSlash + 1);
        }

        public async Task<string> GetPathAsync()
        {
            var exists = await ExistsAsync();
            return exists ? Blob.Name : "";
        }

        public string GetReadSharedAccessUrl(string ipAddress)
        {
            return ReadSharedAccessUrl(ipAddress, SharedAccessBlobPermissions.Read);
        }

        public string GetWriteSharedAccessUrl()
        {
            return ReadSharedAccessUrl(null, SharedAccessBlobPermissions.Write);
        }

        private string ReadSharedAccessUrl(string ipAddress, SharedAccessBlobPermissions permissions)
        {
            var ipAddressOrRange = ipAddress == null || ipAddress == "*" ? null : new IPAddressOrRange(ipAddress);
            var sharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(60);
            var sas = Blob.GetSharedAccessSignature(new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = sharedAccessExpiryTime,
                Permissions = permissions
            }, null, null, SharedAccessProtocol.HttpsOnly, ipAddressOrRange);
            return Blob.Uri.AbsoluteUri + sas;
        }

        public async Task<string> DownloadTextAsync()
        {
            var exists = await ExistsAsync();
            if (!exists)
            {
                return null;
            }
            return await Blob.DownloadTextAsync();
        }

        public async Task SetContentDispositionAsync(string contentDisposition)
        {
            await FetchAttributes();
            Blob.Properties.ContentDisposition = contentDisposition;
            await SetPropertiesAsync();
        }

        public async Task<string> GetTextAsync()
        {
            return await Blob.DownloadTextAsync();
        }

        public async Task UploadTextAsync(string text)
        {
            await Blob.UploadTextAsync(text);
            await SetContentTypeAsync(MimeTypeMap.GetContentTypeFromFilename(_filename));
        }

        public async Task UploadFromStreamAsync(Stream stream)
        {
            stream.Position = 0;
            await Blob.UploadFromStreamAsync(stream);
            await SetContentTypeAsync(MimeTypeMap.GetContentTypeFromFilename(_filename));
        }

        public async Task<bool> CopyBlobAsync(IZeroBlob original)
        {
            var azureBlobOriginal = original as AzureBlob;
            var originalExists = await original.ExistsAsync();
            if (azureBlobOriginal == null || !originalExists)
            {
                return false;
            }
            await Blob.StartCopyAsync(azureBlobOriginal.Blob);
            while (Blob.CopyState.Status == CopyStatus.Pending)
            {
                await Task.Delay(100);
            }
            if (Blob.CopyState.Status != CopyStatus.Success)
            {
                return false;
            }
            var type = await azureBlobOriginal.GetContentTypeAsync();
            await SetContentTypeAsync(type);
            return true;
        }

    }
}
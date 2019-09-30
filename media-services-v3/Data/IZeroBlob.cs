using System.IO;
using System.Threading.Tasks;

namespace media_services_v3.Data
{
    public interface IZeroBlob
    {
        Task<bool> ExistsAsync();
        Task<long> GetSizeAsync();
        string GetName();
        Task<string> GetPathAsync();
        string GetReadSharedAccessUrl(string ipAddress);
        string GetWriteSharedAccessUrl();
        Task SetContentDispositionAsync(string contentDisposition);
        Task UploadTextAsync(string text);
        Task UploadFromStreamAsync(Stream stream);
        Task<bool> CopyBlobAsync(IZeroBlob original);
    }
}